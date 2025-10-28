using Neo4j.Driver;
using organic_store.Models;
using System;
using System.Collections.Generic;
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

        private async Task ExecuteWriteAsync(Func<IAsyncSession, Task> action)
        {
            IAsyncSession session = null;
            try
            {
                session = _driver.AsyncSession(config => config.WithDatabase("organic-store"));
                await action(session);
            }
            finally
            {
                if (session != null)
                    await session.CloseAsync();
            }
        }

        public async Task CreateHoaDonAsync(string maKH, double thanhTien, string diaChiGiaoHang, DateTime ngayTao, string ghiChu, string trangThaiThanhToan, string trangThai, string maCH, List<Dictionary<string, object>> items)
        {
            string maHD = "HD" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            await ExecuteWriteAsync(async session =>
            {
                var queryCreateHoaDon = @"
                    CREATE (hd:HoaDon {
                        MaHD: $maHD,
                        ThanhTien: $thanhTien,
                        DiaChiGiaoHang: $diaChiGiaoHang,
                        NgayTao: datetime($ngayTao),
                        GhiChu: $ghiChu,
                        TrangThaiThanhToan: $trangThaiThanhToan,
                        TrangThai: $trangThai
                    })
                ";
                await session.RunAsync(queryCreateHoaDon, new
                {
                    maHD,
                    thanhTien,
                    diaChiGiaoHang,
                    ngayTao = ngayTao.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ghiChu,
                    trangThaiThanhToan,
                    trangThai
                });

                var queryTaoHoaDon = @"
                    MATCH (kh:KhachHang {MaKH: $maKH}), (hd:HoaDon {MaHD: $maHD})
                    CREATE (kh)-[:TAO_HOADON]->(hd)
                ";
                await session.RunAsync(queryTaoHoaDon, new { maKH, maHD });

                var queryChuaSanPham = @"
                    UNWIND $items AS item
                    MATCH (hd:HoaDon {MaHD: $maHD})
                    MATCH (sp:SanPham {MaSP: item.MaSP})
                    CREATE (hd)-[:CHUA_SANPHAM {SoLuong: item.SoLuong}]->(sp)
                ";
                await session.RunAsync(queryChuaSanPham, new { maHD, items });

                var queryLapTu = @"
                    MATCH (hd:HoaDon {MaHD: $maHD}), (ch:CuaHang {MaCH: $maCH})
                    CREATE (hd)-[:LAP_TU]->(ch)
                ";
                await session.RunAsync(queryLapTu, new { maHD, maCH });

                var queryCapNhatSoTon = @"
                    UNWIND $items AS item
                    MATCH (ch:CuaHang {MaCH: $maCH})-[r:CUNG_CAP]->(sp:SanPham {MaSP: item.MaSP})
                    SET r.SoTon = r.SoTon - item.SoLuong
                ";
                await session.RunAsync(queryCapNhatSoTon, new { maCH, items });
            });
        }

        public async Task<int> DemHoaDonTheoKhachHangAsync(string maKH)
        {
            IAsyncSession session = null;
            try
            {
                session = _driver.AsyncSession(o => o.WithDatabase("organic-store"));
                var query = @"
                    MATCH (k:KhachHang {MaKH: $maKH})-[:TAO_HOADON]->(hd:HoaDon)
                    RETURN count(hd) AS SoLuongHoaDon
                    ";

                var result = await session.RunAsync(query, new Dictionary<string, object> { { "maKH", maKH } });
                var record = await result.SingleAsync();
                return record["SoLuongHoaDon"].As<int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi đếm hóa đơn: " + ex.Message);
                return 0;
            }
            finally
            {
                if (session != null)
                    await session.CloseAsync();
            }
        }

        public async Task<List<HoaDon>> GetHoaDonByKhachHangAsync(string maKH)
        {
            var session = _driver.AsyncSession(config => config.WithDatabase("organic-store"));
            try
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
                        TrangThai = hd.Properties.ContainsKey("TrangThai") ? hd.Properties["TrangThai"].As<string>() : ""
                    };
                });
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}