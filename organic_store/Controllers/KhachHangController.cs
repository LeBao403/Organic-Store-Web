using organic_store.Models;
using organic_store.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace organic_store.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly KhachHangService _khachHangService = new KhachHangService();
        private readonly HoaDonService _hoaDonService = new HoaDonService();

        // Trang cá nhân
        public async Task<ActionResult> Profile()
        {
            var currentUser = Session["CurrentUser"] as KhachHang;
            if (currentUser == null)
            {
                Debug.WriteLine("❌ LỖI: Session 'CurrentUser' là null. Đang chuyển hướng về Login.");
                return RedirectToAction("Login", "Account");
            }

            // --- BẮT ĐẦU DEBUG ---
            // Ghi log giá trị MaKH lấy từ Session
            Debug.WriteLine($"[DEBUG] Đang kiểm tra Profile cho MaKH từ Session: '{currentUser.MaKH}'");
            // --- KẾT THÚC DEBUG ---

            // Lấy lại thông tin mới nhất từ DB
            var kh = await _khachHangService.GetKhachHangByIdAsync(currentUser.MaKH);

            // Đếm số hóa đơn
            int soDonHang = await _hoaDonService.DemHoaDonTheoKhachHangAsync(currentUser.MaKH);

            // Ghi log số đơn hàng đếm được
            Debug.WriteLine($"[DEBUG] Số đơn hàng đếm được cho MaKH '{currentUser.MaKH}': {soDonHang}");

            ViewBag.SoDonHang = soDonHang;

            if (kh == null)
            {
                ViewBag.Error = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            return View(kh);
        }
    }
}