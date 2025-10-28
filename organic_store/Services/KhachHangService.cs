using Neo4j.Driver;
using organic_store.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace organic_store.Services
{
    public class KhachHangService
    {
        private readonly IDriver _driver;

        public KhachHangService()
        {
            _driver = MvcApplication.Neo4jDriver;
        }

        // Lấy thông tin khách hàng theo mã KH
        public async Task<KhachHang> GetKhachHangByIdAsync(string maKH)
        {
            IAsyncSession session = null;
            try
            {
                var query = @"
                    MATCH (k:KhachHang {MaKH: $maKH})
                    RETURN k
                ";

                session = _driver.AsyncSession(config => config.WithDatabase("organic-store"));
                var result = await session.RunAsync(query, new { maKH });
                var record = (await result.ToListAsync()).FirstOrDefault();

                if (record == null) return null;

                var k = record["k"].As<INode>();

                return new KhachHang
                {
                    MaKH = k.Properties.ContainsKey("MaKH") ? k.Properties["MaKH"].As<string>() : "",
                    HoTen = k.Properties.ContainsKey("HoTen") ? k.Properties["HoTen"].As<string>() : "",
                    TenDangNhap = k.Properties.ContainsKey("TenDangNhap") ? k.Properties["TenDangNhap"].As<string>() : "",
                    Email = k.Properties.ContainsKey("Email") ? k.Properties["Email"].As<string>() : "",
                    SoDienThoai = k.Properties.ContainsKey("SoDienThoai") ? k.Properties["SoDienThoai"].As<string>() : "",
                    DiaChiCuThe = k.Properties.ContainsKey("DiaChiCuThe") ? k.Properties["DiaChiCuThe"].As<string>() : "",
                    NgaySinh = k.Properties.ContainsKey("NgaySinh") ? DateTime.Parse(k.Properties["NgaySinh"].As<string>()) : DateTime.MinValue,
                    LoaiKH = k.Properties.ContainsKey("LoaiKH") ? k.Properties["LoaiKH"].As<string>() : "thường"
                };
            }
            finally
            {
                if (session != null)
                    await session.CloseAsync();
            }
        }
    }
}