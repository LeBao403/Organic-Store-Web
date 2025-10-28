using Neo4j.Driver;
using organic_store.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace organic_store.Services
{
    public class HomeService : IDisposable
    {
        private readonly IDriver _driver;

        public HomeService()
        {
            _driver = MvcApplication.Neo4jDriver;
        }

        // Helper: Thực thi truy vấn đọc
        private async Task<T> ExecuteReadAsync<T>(Func<IAsyncSession, Task<T>> action)
        {
            IAsyncSession session = null;
            try
            {
                session = _driver.AsyncSession(config => config.WithDatabase("organic-store"));
                return await action(session);
            }
            finally
            {
                if (session != null)
                    await session.CloseAsync();
            }
        }

        private Products MapRecordToProduct(IRecord record, string maCH)
        {
            var p = record["p"].As<INode>();
            var d = record["d"].As<INode>();
            var ch = record.Keys.Contains("ch") ? record["ch"].As<INode>() : null;

            // ⭐ ĐẢM BẢO GIÁ TRỊ MẶC ĐỊNH LUÔN LÀ 0 TẠI ĐÂY
            long soTon = 0;

            // Chỉ lấy SoTon từ mối quan hệ 'r' nếu maCH không phải "ALL" VÀ 'r' tồn tại
            if (maCH != "ALL" && record.Keys.Contains("r"))
            {
                var r = record["r"].As<IRelationship>();
                // Lấy SoTon từ mối quan hệ CUNG_CAP, nếu không có thì vẫn là 0
                soTon = r.Properties.ContainsKey("SoTon")
                    ? r.Properties["SoTon"].As<long>()
                    : 0;
            }
            // Nếu maCH là ALL hoặc 'r' không tồn tại, soTon = 0

            var tenCH = ch?.Properties.ContainsKey("TenCH") == true ? ch.Properties["TenCH"].As<string>() : "Tất cả";

            return new Products
            {
                MaSP = p.Properties.ContainsKey("MaSP") ? p.Properties["MaSP"].As<string>() : "",
                TenSP = p.Properties.ContainsKey("TenSP") ? p.Properties["TenSP"].As<string>() : "",
                DonVi = p.Properties.ContainsKey("DonVi") ? p.Properties["DonVi"].As<string>() : "",
                GiaBan = p.Properties.ContainsKey("GiaBan") ? Convert.ToDouble(p.Properties["GiaBan"]) : 0,
                MoTa = p.Properties.ContainsKey("MoTa") ? p.Properties["MoTa"].As<string>() : "",
                HinhAnhURL = p.Properties.ContainsKey("HinhAnhURL") ? p.Properties["HinhAnhURL"].As<string>() : "",
                MaDM = d.Properties.ContainsKey("MaDM") ? d.Properties["MaDM"].As<string>() : "",
                SoTon = soTon, // ⭐ Sẽ là 0 nếu không lấy được
                TenCH = tenCH
            };
        }

        public async Task<List<CuaHang>> GetAllStoresAsync()
        {
            var query = @"
                MATCH (ch:CuaHang)
                RETURN ch
                ORDER BY ch.TenCH";

            return await ExecuteReadAsync(async session =>
            {
                var result = await session.RunAsync(query);
                return await result.ToListAsync(record =>
                {
                    var chNode = record["ch"].As<INode>();
                    return new CuaHang
                    {
                        MaCH = chNode.Properties["MaCH"].As<string>(),
                        TenCH = chNode.Properties["TenCH"].As<string>(),
                        DiaChiCuThe = chNode.Properties["DiaChiCuThe"].As<string>(),
                        SoDienThoai = chNode.Properties["SoDienThoai"].As<string>()
                    };
                });
            });
        }

        public async Task<List<Products>> GetAllProductsAsync(string maCH = null)
        {
            maCH = string.IsNullOrEmpty(maCH) ? "ALL" : maCH;

            var matchClause = maCH == "ALL"
                ? "(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)"
                // Bắt buộc phải có r:CUNG_CAP để lấy r.SoTon khi lọc theo cửa hàng
                : "(ch:CuaHang {MaCH: $maCH})-[r:CUNG_CAP]->(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)";

            var returnClause = maCH == "ALL"
                ? "RETURN p, d"
                : "RETURN p, d, r, ch";

            var cypherQuery = $@"
                MATCH {matchClause}
                {returnClause}
            ";

            return await ExecuteReadAsync(async session =>
            {
                var result = await session.RunAsync(cypherQuery, new { maCH });
                var records = await result.ToListAsync();
                return records.Select(record => MapRecordToProduct(record, maCH)).ToList();
            });
        }

        // Tìm kiếm sản phẩm theo keyword và MaCH
        public async Task<List<Products>> SearchProductsAsync(string keyword, string maCH = null)
        {
            maCH = string.IsNullOrEmpty(maCH) ? "ALL" : maCH;

            var matchClause = maCH == "ALL"
                ? "(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)"
                : "(ch:CuaHang {MaCH: $maCH})-[r:CUNG_CAP]->(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)";

            var returnClause = maCH == "ALL"
                ? "RETURN p, d"
                : "RETURN p, d, r, ch";

            var query = $@"
                MATCH {matchClause}
                WHERE toLower(p.TenSP) CONTAINS toLower($kw) OR toLower(p.MaSP) CONTAINS toLower($kw)
                {returnClause}
            ";

            return await ExecuteReadAsync(async session =>
            {
                var result = await session.RunAsync(query, new { kw = keyword ?? "", maCH });
                var records = await result.ToListAsync();
                // ⭐ Truyền maCH vào MapRecordToProduct
                return records.Select(record => MapRecordToProduct(record, maCH)).ToList();
            });
        }

        public async Task<long> GetStockByProductAsync(string maSP, string maCH)
        {
            if (maCH == "ALL")
            {
                return 0; // Luôn trả về 0 nếu đang ở chế độ "Tất cả cửa hàng"
            }

            return await ExecuteReadAsync(async session =>
            {
                var query = @"
                    MATCH (ch:CuaHang {MaCH: $maCH})-[r:CUNG_CAP]->(sp:SanPham {MaSP: $maSP})
                    RETURN r.SoTon AS SoTon
                    LIMIT 1
                ";
                var result = await session.RunAsync(query, new { maSP, maCH });

                var record = (await result.ToListAsync()).FirstOrDefault();

                if (record != null && record.Values.ContainsKey("SoTon"))
                {
                    // Trả về số tồn kho thực tế
                    return record["SoTon"].As<long>();
                }

                // ⭐ Quan trọng: Nếu không tìm thấy tồn kho, trả về 0
                return 0;
            });
        }

        public void Dispose() => _driver?.Dispose();
    }
}