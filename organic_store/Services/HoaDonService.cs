using Neo4j.Driver;
using organic_store.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace organic_store.Services
{
    public class HoaDonService
    {
        private readonly IDriver _driver;

        public HoaDonService()
        {
            _driver = MvcApplication.Neo4jDriver;
        }

        // Đếm số hóa đơn của 1 khách hàng (Giữ nguyên)
        public async Task<int> DemHoaDonTheoKhachHangAsync(string maKH)
        {
            try
            {
                var query = @"
                    MATCH (k:KhachHang {MaKH: $maKH})-[:TAO_HOADON]->(hd:HoaDon)
                    RETURN count(hd) AS SoLuongHoaDon
                    ";

                using (var session = _driver.AsyncSession(o => o.WithDatabase("organic-store")))
                {
                    var result = await session.RunAsync(query, new Dictionary<string, object> { { "maKH", maKH } });
                    var record = await result.SingleAsync();
                    return record["SoLuongHoaDon"].As<int>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi đếm hóa đơn: " + ex.Message);
                return 0;
            }
        }

        // PHƯƠNG THỨC MỚI: Lấy danh sách hóa đơn theo MaKH (Đã fix lỗi)
        public async Task<List<HoaDon>> GetHoaDonByKhachHangAsync(string maKH)
        {
            using (var session = _driver.AsyncSession(config => config.WithDatabase("organic-store")))
            {
                var query = @"
                    MATCH (k:KhachHang {MaKH: $maKH})-[:TAO_HOADON]->(hd:HoaDon)
                    RETURN hd
                    ORDER BY hd.NgayTao DESC
                ";

                var result = await session.RunAsync(query, new { maKH });

                return await result.ToListAsync(record =>
                {
                    var hd = record["hd"].As<INode>();

                    DateTime GetDateTime(INode node, string key)
                    {
                        if (node.Properties.ContainsKey(key))
                        {
                            try
                            {
                                return node.Properties[key].As<DateTime>();
                            }
                            catch
                            {
                                return DateTime.MinValue;
                            }
                        }
                        return DateTime.MinValue;
                    }

                    return new HoaDon
                    {
                        MaHD = hd.Properties.ContainsKey("MaHD") ? hd.Properties["MaHD"].As<string>() : "",
                        ThanhTien = hd.Properties.ContainsKey("ThanhTien") ? hd.Properties["ThanhTien"].As<double>() : 0,
                        DiaChiGiaoHang = hd.Properties.ContainsKey("DiaChiGiaoHang") ? hd.Properties["DiaChiGiaoHang"].As<string>() : "",
                        NgayTao = GetDateTime(hd, "NgayTao"),
                        GhiChu = hd.Properties.ContainsKey("GhiChu") ? hd.Properties["GhiChu"].As<string>() : "",
                        TrangThaiThanhToan = hd.Properties.ContainsKey("TrangThaiThanhToan") ? hd.Properties["TrangThaiThanhToan"].As<string>() : "",
                    };
                });
            }
        }
    }
}