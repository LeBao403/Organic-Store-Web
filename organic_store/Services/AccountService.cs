using Neo4j.Driver;
using organic_store.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace organic_store.Services
{
    public class AccountService : IDisposable
    {
        private readonly IDriver _driver;

        public AccountService()
        {
            _driver = MvcApplication.Neo4jDriver;
        }

        #region CREATE NODE

        public async Task<bool> RegisterKhachHangAsync(KhachHang kh)
        {
            kh.MaKH = "KH" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            var props = new Dictionary<string, object>
            {
                ["MaKH"] = kh.MaKH,
                ["TenDangNhap"] = kh.TenDangNhap,
                ["MatKhau"] = kh.MatKhau,
                ["HoTen"] = kh.HoTen,
                ["Email"] = kh.Email,
                ["SoDienThoai"] = kh.SoDienThoai,
                ["DiaChiCuThe"] = kh.DiaChiCuThe,
                ["NgaySinh"] = kh.NgaySinh.ToString("yyyy-MM-dd"),
                ["LoaiKH"] = kh.LoaiKH
            };
            return await CreateNodeAsync("KhachHang", props);
        }

        public async Task<bool> RegisterNguoiQuanTriAsync(NguoiQuanTri qt)
        {
            var props = new Dictionary<string, object>
            {
                ["MaQT"] = qt.MaQT,
                ["TenDangNhap"] = qt.TenDangNhap,
                ["MatKhau"] = qt.MatKhau,
                ["HoTen"] = qt.HoTen,
                ["Email"] = qt.Email,
                ["SoDienThoai"] = qt.SoDienThoai,
            };
            return await CreateNodeAsync("NguoiQuanTri", props);
        }

        private async Task<bool> CreateNodeAsync(string label, Dictionary<string, object> props)
        {
            var query = $"CREATE (n:{label} $props)";
            try
            {
                await ExecuteWriteAsync(async session =>
                    await session.RunAsync(query, new { props }));
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region LOGIN / CHECK EXISTS

        public async Task<TaiKhoan> LoginAsync(string tenDangNhap, string matKhau)
        {
            // Mật khẩu được so sánh trực tiếp trong Cypher
            var query = @"
                MATCH (u)
                WHERE (u:KhachHang OR u:NguoiQuanTri)
                  AND u.TenDangNhap = $tenDangNhap
                  AND u.MatKhau = $matKhau
                RETURN u";

            var node = await ExecuteReadAsync(async session =>
            {
                var result = await session.RunAsync(query, new { tenDangNhap, matKhau });
                var list = await result.ToListAsync();
                return list.Count > 0 ? list[0]["u"].As<INode>() : null;
            });

            return node == null ? null : MapNodeToTaiKhoan(node);
        }

        public async Task<bool> ExistsUsernameOrEmailAsync(string tenDangNhap, string email)
        {
            var query = @"
                MATCH (u)
                WHERE (u:KhachHang OR u:NguoiQuanTri)
                  AND (u.TenDangNhap = $tenDangNhap OR u.Email = $email)
                RETURN u LIMIT 1";

            var node = await ExecuteReadAsync(async session =>
            {
                var result = await session.RunAsync(query, new { tenDangNhap, email });
                var list = await result.ToListAsync();
                return list.Count > 0 ? list[0]["u"].As<INode>() : null;
            });

            return node != null;
        }

        public async Task<(bool Exists, string ErrorMessage)> CheckDuplicateAsync(string tenDangNhap, string email, string soDienThoai)
        {
            var query = @"
                MATCH (u)
                WHERE (u:KhachHang OR u:NguoiQuanTri)
                  AND (u.TenDangNhap = $tenDangNhap 
                    OR u.Email = $email 
                    OR (u:KhachHang AND u.SoDienThoai = $soDienThoai)
                  )
                RETURN u.TenDangNhap, u.Email, u.SoDienThoai LIMIT 1";

            var record = await ExecuteReadAsync(async session =>
            {
                var result = await session.RunAsync(query, new { tenDangNhap, email, soDienThoai });
                var list = await result.ToListAsync();
                return list.Count > 0 ? list[0] : null;
            });

            if (record != null)
            {
                // Sử dụng hàm TryGetValue an toàn hơn để tránh lỗi nếu key không tồn tại (như SoDienThoai trên NguoiQuanTri)
                // Tuy nhiên, do query đã được fix, ta có thể dùng cách cũ nhưng cần lưu ý rằng kết quả trả về là properties của node u
                string recordTenDangNhap = record["u.TenDangNhap"].As<string>();
                string recordEmail = record["u.Email"].As<string>();
                // Thuộc tính có thể là null nếu đó là NguoiQuanTri nhưng query đã lọc
                string recordSoDienThoai = record["u.SoDienThoai"].As<string>();

                if (recordTenDangNhap == tenDangNhap)
                    return (true, "Tên đăng nhập đã tồn tại.");
                if (recordEmail == email)
                    return (true, "Email đã tồn tại.");
                if (recordSoDienThoai == soDienThoai && recordSoDienThoai != null)
                    return (true, "Số điện thoại đã tồn tại.");
            }

            return (false, null);
        }

        #endregion

        #region GET ALL
        // ... (Không thay đổi)
        public async Task<List<KhachHang>> GetAllKhachHangAsync()
        {
            return await GetAllByLabelAsync("KhachHang", record => new KhachHang
            {
                MaKH = record["MaKH"].As<string>(),
                HoTen = record["HoTen"].As<string>(),
                TenDangNhap = record["TenDangNhap"].As<string>(),
                MatKhau = record["MatKhau"].As<string>(),
                Email = record["Email"].As<string>(),
                SoDienThoai = record["SoDienThoai"].As<string>(),
                DiaChiCuThe = record["DiaChiCuThe"].As<string>(),
                NgaySinh = record["NgaySinh"].As<DateTime>(),
                LoaiKH = record["LoaiKH"].As<string>()
            });
        }

        public async Task<List<NguoiQuanTri>> GetAllNguoiQuanTriAsync()
        {
            return await GetAllByLabelAsync("NguoiQuanTri", record => new NguoiQuanTri
            {
                MaQT = record["MaQT"].As<string>(),
                HoTen = record["HoTen"].As<string>(),
                TenDangNhap = record["TenDangNhap"].As<string>(),
                MatKhau = record["MatKhau"].As<string>(),
                Email = record["Email"].As<string>(),
                SoDienThoai = record["SoDienThoai"].As<string>(),
            });
        }

        private async Task<List<T>> GetAllByLabelAsync<T>(string label, Func<IRecord, T> mapFunc)
        {
            var query = $"MATCH (n:{label}) RETURN n.* ORDER BY n.HoTen";
            return await ExecuteReadAsync(async session =>
            {
                var result = await session.RunAsync(query);
                return await result.ToListAsync(mapFunc);
            });
        }

        #endregion

        #region HELPERS
        // ... (Không thay đổi)
        private TaiKhoan MapNodeToTaiKhoan(INode node)
        {
            string GetString(string key) => node.Properties.ContainsKey(key) ? node[key].As<string>() : null;
            DateTime GetDateTime(string key) =>
                node.Properties.ContainsKey(key) && DateTime.TryParse(node[key].As<string>(), out var dt) ? dt : DateTime.MinValue;

            if (node.Labels.Contains("KhachHang"))
            {
                return new KhachHang
                {
                    MaKH = GetString("MaKH"),
                    TenDangNhap = GetString("TenDangNhap"),
                    MatKhau = GetString("MatKhau"),
                    HoTen = GetString("HoTen"),
                    Email = GetString("Email"),
                    SoDienThoai = GetString("SoDienThoai"),
                    DiaChiCuThe = GetString("DiaChiCuThe"),
                    NgaySinh = GetDateTime("NgaySinh"),
                    LoaiKH = GetString("LoaiKH")
                };
            }
            else // NguoiQuanTri
            {
                return new NguoiQuanTri
                {
                    MaQT = GetString("MaQT"),
                    TenDangNhap = GetString("TenDangNhap"),
                    MatKhau = GetString("MatKhau"),
                    HoTen = GetString("HoTen"),
                    Email = GetString("Email"),
                    SoDienThoai = GetString("SoDienThoai")
                };
            }
        }



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


        public void Close() => _driver?.Dispose();
        public void Dispose() => _driver?.Dispose();

        #endregion
    }
}