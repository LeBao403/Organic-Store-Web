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

        // Helper: Thực thi truy vấn đọc (Read Transaction)
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

        // Helper: Ánh xạ Record từ Cypher sang Model Products
        private Products MapRecordToProduct(IRecord record)
        {
            var p = record["p"].As<INode>();
            var d = record["d"].As<INode>();
            // Node CuaHang có thể không tồn tại nếu truy vấn toàn bộ sản phẩm
            var ch = record.Keys.Contains("ch") ? record["ch"].As<INode>() : null;

            // Quan hệ CUNG_CAP (chứa SoTon) có thể không tồn tại
            var soTon = record.Keys.Contains("r")
                        && record["r"].As<IRelationship>().Properties.ContainsKey("SoTon")
                        ? record["r"].As<IRelationship>().Properties["SoTon"].As<long>()
                        : 0;

            // Tên cửa hàng để hiển thị
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

                // Thuộc tính mới
                SoTon = soTon,
                TenCH = tenCH
            };
        }


        // PHƯƠNG THỨC MỚI: Lấy danh sách Cửa hàng
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

        // CẬP NHẬT: Lấy sản phẩm theo MaCH
        public async Task<List<Products>> GetAllProductsAsync(string maCH = null)
        {
            // Xây dựng câu lệnh Cypher dựa trên MaCH
            var matchClause = maCH == "ALL" || string.IsNullOrEmpty(maCH)
                ? "(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)"
                : "(ch:CuaHang {MaCH: $maCH})-[r:CUNG_CAP]->(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)";

            var returnClause = maCH == "ALL" || string.IsNullOrEmpty(maCH)
                ? "RETURN p, d"
                : "RETURN p, d, r, ch";

            var cypherQuery = $@"
                MATCH {matchClause}
                {returnClause}
            ";

            return await ExecuteReadAsync(async tx =>
            {
                var result = await tx.RunAsync(cypherQuery, new { maCH });
                var records = await result.ToListAsync();
                return records.Select(MapRecordToProduct).ToList();
            });
        }

        // CẬP NHẬT: Tìm kiếm sản phẩm theo keyword và MaCH
        public async Task<List<Products>> SearchProductsAsync(string keyword, string maCH = null)
        {
            var matchClause = maCH == "ALL" || string.IsNullOrEmpty(maCH)
                ? "(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)"
                : "(ch:CuaHang {MaCH: $maCH})-[r:CUNG_CAP]->(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)";

            var returnClause = maCH == "ALL" || string.IsNullOrEmpty(maCH)
                ? "RETURN p, d"
                : "RETURN p, d, r, ch";

            var query = $@"
                MATCH {matchClause}
                WHERE toLower(p.TenSP) CONTAINS toLower($kw)
                {returnClause}
            ";

            return await ExecuteReadAsync(async session =>
            {
                var result = await session.RunAsync(query, new { kw = keyword ?? "", maCH });
                var records = await result.ToListAsync();
                return records.Select(MapRecordToProduct).ToList();
            });
        }

        public void Dispose() => _driver?.Dispose();
    }
}