using Neo4j.Driver;
using organic_store.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace organic_store.Services
{
    public class CartService
    {
        private readonly IDriver _driver;

        public CartService()
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

        // Helper: Thực thi truy vấn ghi
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

        // Lấy danh sách giỏ hàng của khách hàng
        public async Task<List<CartItemDTO>> GetCartByCustomerAsync(string maKH, string maCH) // ⭐ ĐÃ SỬA: Kiểu trả về là CartItemDTO
        {
            return await ExecuteReadAsync(async session =>
            {
                var query = @"
            MATCH (kh:KhachHang {MaKH: $maKH})-[:TAO_GIOHANG]->(gh:GioHang)-[r:CHUA_SANPHAM]->(sp:SanPham)
            WHERE $maCH = 'ALL' OR r.MaCH = $maCH // ⭐ ĐÃ SỬA: Thêm điều kiện lọc ALL
            RETURN sp.MaSP AS MaSP, sp.TenSP AS TenSP, sp.GiaBan AS GiaBan, sp.HinhAnhURL AS HinhAnhURL, r.SoLuong AS SoLuong, r.ThanhTien AS ThanhTien // ⭐ ĐÃ SỬA: Lấy từ mối quan hệ 'r'
        ";
                var result = await session.RunAsync(query, new { maKH, maCH });

                var cartItems = new List<CartItemDTO>(); 
                await result.ForEachAsync(record =>
                {
                    cartItems.Add(new CartItemDTO 
                    {
                        MaSP = record["MaSP"].As<string>() ?? "",
                        TenSP = record["TenSP"].As<string>() ?? "",
                        GiaBan = record["GiaBan"].As<double?>() ?? 0.0,
                        HinhAnhURL = record["HinhAnhURL"].As<string>() ?? "",
                        SoLuong = record["SoLuong"].As<int?>() ?? 0,
                        ThanhTien = record["ThanhTien"].As<double?>() ?? 0.0
                    });
                });

                return cartItems;
            });
        }

        // Thêm sản phẩm vào giỏ hàng
        public async Task AddToCartAsync(string maKH, string maSP, int soLuong, string maCH)
        {
            await ExecuteWriteAsync(async session =>
            {
                var checkCartQuery = @"
                    MATCH (kh:KhachHang {MaKH: $maKH})-[:TAO_GIOHANG]->(gh:GioHang)-[r:CHUA_SANPHAM {MaCH: $maCH}]->(sp:SanPham {MaSP: $maSP})
                    RETURN r
                    LIMIT 1
                ";
                var checkResult = await session.RunAsync(checkCartQuery, new { maKH, maSP, maCH });

                
                var existingItem = (await checkResult.ToListAsync()).FirstOrDefault();

                if (existingItem != null)
                {
                    var updateQuery = @"
                        MATCH (kh:KhachHang {MaKH: $maKH})-[:TAO_GIOHANG]->(gh:GioHang)-[r:CHUA_SANPHAM {MaCH: $maCH}]->(sp:SanPham {MaSP: $maSP})
                        SET r.SoLuong = r.SoLuong + $soLuong, 
                            r.ThanhTien = (r.SoLuong + $soLuong) * sp.GiaBan
                    ";
                    await session.RunAsync(updateQuery, new { maKH, maSP, maCH, soLuong });
                }
                else
                {
                    var createQuery = @"
                        MATCH (kh:KhachHang {MaKH: $maKH})
                        MATCH (sp:SanPham {MaSP: $maSP})
                        MATCH (kh)-[:TAO_GIOHANG]->(gh:GioHang) 
                        CREATE (gh)-[:CHUA_SANPHAM {MaCH: $maCH, SoLuong: $soLuong, ThanhTien: $thanhTien}]->(sp)
                    ";
                    var price = await GetPriceByProductAsync(maSP);
                    await session.RunAsync(createQuery, new { maKH, maSP, maCH, soLuong, thanhTien = soLuong * price });
                }
            });
        }

        // Cập nhật số lượng trong giỏ hàng
        public async Task UpdateQuantityAsync(string maKH, string maSP, int soLuong, string maCH)
        {
            await ExecuteWriteAsync(async session =>
            {
                var updateQuery = @"
            MATCH (kh:KhachHang {MaKH: $maKH})-[:TAO_GIOHANG]->(gh:GioHang)-[r:CHUA_SANPHAM {MaCH: $maCH}]->(sp:SanPham {MaSP: $maSP})
            SET r.SoLuong = $soLuong, // ⭐ ĐÃ SỬA: Cập nhật trên mối quan hệ 'r'
                r.ThanhTien = $soLuong * sp.GiaBan // ⭐ ĐÃ SỬA: Tính lại ThanhTien trên mối quan hệ 'r'
        ";
                await session.RunAsync(updateQuery, new { maKH, maSP, maCH, soLuong });
            });
        }

        // Xóa sản phẩm khỏi giỏ hàng
        public async Task RemoveFromCartAsync(string maKH, string maSP, string maCH)
        {
            await ExecuteWriteAsync(async session =>
            {
                var deleteQuery = @"
                    MATCH (kh:KhachHang {MaKH: $maKH})-[:TAO_GIOHANG]->(gh:GioHang)-[r:CHUA_SANPHAM {MaCH: $maCH}]->(sp:SanPham {MaSP: $maSP})
                    DELETE r
                ";
                await session.RunAsync(deleteQuery, new { maKH, maSP, maCH });
            });
        }

        // Lấy giá bán của sản phẩm
        private async Task<double> GetPriceByProductAsync(string maSP)
        {
            return await ExecuteReadAsync(async session =>
            {
                var query = @"
                    MATCH (sp:SanPham {MaSP: $maSP})
                    RETURN sp.GiaBan AS GiaBan
                    LIMIT 1
                ";
                var result = await session.RunAsync(query, new { maSP });

                var record = (await result.ToListAsync()).FirstOrDefault();

                return record != null ? record["GiaBan"].As<double>() : 0;
            });
        }

        // Lấy danh sách tất cả cửa hàng
        public async Task<List<CuaHang>> GetAllStoresAsync()
        {
            return await ExecuteReadAsync(async session =>
            {
                var query = @"
                    MATCH (ch:CuaHang)
                    RETURN ch.MaCH AS MaCH, ch.TenCH AS TenCH
                ";
                var result = await session.RunAsync(query);
                var stores = new List<CuaHang>();
                await result.ForEachAsync(record =>
                {
                    stores.Add(new CuaHang
                    {
                        MaCH = record["MaCH"].As<string>(),
                        TenCH = record["TenCH"].As<string>()
                    });
                });
                return stores;
            });
        }

        // Lấy tên cửa hàng
        public async Task<string> GetStoreNameAsync(string maCH)
        {
            return await ExecuteReadAsync(async session =>
            {
                var query = @"
                    MATCH (ch:CuaHang {MaCH: $maCH})
                    RETURN ch.TenCH AS TenCH
                    LIMIT 1
                ";
                var result = await session.RunAsync(query, new { maCH });
                IRecord record = null;
                while (await result.FetchAsync())
                {
                    record = result.Current;
                    break;
                }
                return record != null ? record["TenCH"].As<string>() : "Không xác định";
            });
        }

        // Model nội bộ cho giỏ hàng tạm thời
        public class TempCartItem
        {
            public string MaSP { get; set; }
            public string TenSP { get; set; }
            public double GiaBan { get; set; }
            public string HinhAnhURL { get; set; }
            public int Quantity { get; set; }
            public long SoTon { get; set; } 
            public string MaCH { get; set; }
        }

        public class CartItemDTO 
        {
            public string MaSP { get; set; }
            public string TenSP { get; set; }
            public double GiaBan { get; set; }
            public string HinhAnhURL { get; set; }
            public int SoLuong { get; set; }
            public double ThanhTien { get; set; }
        }
    }
}