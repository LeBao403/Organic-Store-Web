using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace organic_store.Services
{
    public class HoaDonService
    {
        private readonly IDriver _driver;

        public HoaDonService()
        {
            _driver = MvcApplication.Neo4jDriver;
        }

        // Đếm số hóa đơn của 1 khách hàng
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
                Console.WriteLine("❌ Lỗi khi đếm hóa đơn: " + ex.Message);
                return 0;
            }
        }
    }
}