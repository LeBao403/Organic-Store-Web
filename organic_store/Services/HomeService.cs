// organic_store.Services/HomeService.cs

using Neo4j.Driver;
using organic_store.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc; // Cần thiết cho MvcApplication

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
            var ch = record.Keys.Contains("ch") ? record["ch"].As<INode>() : null;

            var soTon = record.Keys.Contains("r")
                        && record["r"].As<IRelationship>().Properties.ContainsKey("SoTon")
                        ? record["r"].As<IRelationship>().Properties["SoTon"].As<long>()
                        : 0;

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

                SoTon = soTon,
                TenCH = tenCH
            };
        }

        // Bổ sung: Lấy tất cả Danh mục (cho Navbar)
        public async Task<List<DanhMuc>> GetAllCategoriesAsync()
        {
            return await ExecuteReadAsync(async session =>
            {
                var query = "MATCH (d:DanhMuc) RETURN d ORDER BY d.MaDM";
                var result = await session.RunAsync(query);
                return await result.ToListAsync(record =>
                {
                    var dNode = record["d"].As<INode>();
                    return new DanhMuc
                    {
                        MaDM = dNode.Properties["MaDM"].As<string>(),
                        TenDM = dNode.Properties["TenDM"].As<string>()
                    };
                });
            });
        }

        // Lấy danh sách Cửa hàng (Giữ nguyên)
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

        // FIX: Lấy sản phẩm theo MaCH VÀ MaDM
        public async Task<List<Products>> GetAllProductsAsync(string maCH = "ALL", string maDM = "ALL")
        {
            string matchClause;
            string returnClause;

            if (maCH == "ALL" || string.IsNullOrEmpty(maCH))
            {
                matchClause = "(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)";
                returnClause = "RETURN p, d";
            }
            else
            {
                matchClause = "(ch:CuaHang {MaCH: $maCH})-[r:CUNG_CAP]->(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)";
                returnClause = "RETURN p, d, r, ch";
            }

            string whereClause = "";
            if (!string.IsNullOrEmpty(maDM) && maDM != "ALL")
            {
                whereClause = "WHERE d.MaDM = $maDM";
            }

            var cypherQuery = $@"
                MATCH {matchClause}
                {whereClause}
                {returnClause}
            ";

            return await ExecuteReadAsync(async tx =>
            {
                var result = await tx.RunAsync(cypherQuery, new { maCH, maDM });
                var records = await result.ToListAsync();
                return records.Select(MapRecordToProduct).ToList();
            });
        }

        // FIX: Tìm kiếm sản phẩm theo Keyword, MaCH VÀ MaDM
        public async Task<List<Products>> SearchProductsAsync(string keyword, string maCH = "ALL", string maDM = "ALL")
        {
            string matchClause;
            string returnClause;

            if (maCH == "ALL" || string.IsNullOrEmpty(maCH))
            {
                matchClause = "(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)";
                returnClause = "RETURN p, d";
            }
            else
            {
                matchClause = "(ch:CuaHang {MaCH: $maCH})-[r:CUNG_CAP]->(p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)";
                returnClause = "RETURN p, d, r, ch";
            }

            string whereCondition = $"toLower(p.TenSP) CONTAINS toLower($kw)";

            if (!string.IsNullOrEmpty(maDM) && maDM != "ALL")
            {
                whereCondition += $" AND d.MaDM = $maDM";
            }

            var query = $@"
                MATCH {matchClause}
                WHERE {whereCondition}
                {returnClause}
            ";

            return await ExecuteReadAsync(async session =>
            {
                var result = await session.RunAsync(query, new { kw = keyword ?? "", maCH, maDM });
                var records = await result.ToListAsync();
                return records.Select(MapRecordToProduct).ToList();
            });
        }

        public void Dispose() => _driver?.Dispose();
    }
}