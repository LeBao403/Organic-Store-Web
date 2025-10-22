using Neo4j.Driver;
using organic_store.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace organic_store.Services
{
    public class HomeService
    {
        private readonly IDriver _driver;

        public HomeService()
        {
            _driver = MvcApplication.Neo4jDriver;
        }

        public async Task<List<Products>> GetAllProductsAsync()
        {
            IAsyncSession session = null;
            try
            {
                session = _driver.AsyncSession(config => config.WithDatabase("organic-store"));
                var products = await session.ExecuteReadAsync(async tx =>
                {
                    var cypherQuery = @"
                        MATCH (p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)
                        RETURN p, d
                    ";
                    var result = await tx.RunAsync(cypherQuery);
                    var records = await result.ToListAsync();

                    return records.Select(record => new Products
                    {
                        MaSP = record["p"].As<INode>().Properties.ContainsKey("MaSP") ? record["p"].As<INode>().Properties["MaSP"].As<string>() : "",
                        TenSP = record["p"].As<INode>().Properties.ContainsKey("TenSP") ? record["p"].As<INode>().Properties["TenSP"].As<string>() : "",
                        DonVi = record["p"].As<INode>().Properties.ContainsKey("DonVi") ? record["p"].As<INode>().Properties["DonVi"].As<string>() : "",
                        GiaBan = record["p"].As<INode>().Properties.ContainsKey("GiaBan") ? record["p"].As<INode>().Properties["GiaBan"].As<double>() : 0,
                        MoTa = record["p"].As<INode>().Properties.ContainsKey("MoTa") ? record["p"].As<INode>().Properties["MoTa"].As<string>() : "",
                        HinhAnhURL = record["p"].As<INode>().Properties.ContainsKey("HinhAnhURL") ? record["p"].As<INode>().Properties["HinhAnhURL"].As<string>() : "",
                        MaDM = record["d"].As<INode>().Properties.ContainsKey("MaDM") ? record["d"].As<INode>().Properties["MaDM"].As<string>() : ""
                    }).ToList();
                });

                return products;
            }
            finally
            {
                if (session != null)
                    await session.CloseAsync();
            }
        }

        // Tìm kiếm sản phẩm
        public async Task<List<Products>> SearchProductsAsync(string keyword)
        {
            var list = new List<Products>();
            var query = @"
                MATCH (p:SanPham)-[:THUOC_DANHMUC]->(d:DanhMuc)
                WHERE toLower(p.TenSP) CONTAINS toLower($kw)
                RETURN p, d
            ";

            using (var session = _driver.AsyncSession(config => config.WithDatabase("organic-store")))
            {
                var result = await session.RunAsync(query, new { kw = keyword ?? "" });
                var records = await result.ToListAsync();

                foreach (var record in records)
                {
                    var p = record["p"].As<INode>();
                    var d = record["d"].As<INode>();

                    list.Add(new Products
                    {
                        MaSP = p.Properties.ContainsKey("MaSP") ? p.Properties["MaSP"].As<string>() : "",
                        TenSP = p.Properties.ContainsKey("TenSP") ? p.Properties["TenSP"].As<string>() : "",
                        DonVi = p.Properties.ContainsKey("DonVi") ? p.Properties["DonVi"].As<string>() : "",
                        GiaBan = p.Properties.ContainsKey("GiaBan") ? Convert.ToDouble(p.Properties["GiaBan"]) : 0,
                        MoTa = p.Properties.ContainsKey("MoTa") ? p.Properties["MoTa"].As<string>() : "",
                        HinhAnhURL = p.Properties.ContainsKey("HinhAnhURL") ? p.Properties["HinhAnhURL"].As<string>() : "",
                        MaDM = d.Properties.ContainsKey("MaDM") ? d.Properties["MaDM"].As<string>() : ""
                    });
                }
            }

            return list;
        }
    }
}
