using organic_store.Models;
using organic_store.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace organic_store.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly KhachHangService _khachHangService = new KhachHangService();
        private readonly HoaDonService _hoaDonService = new HoaDonService();

        // Trang cá nhân - Tải dữ liệu cơ bản cho Header và Tab mặc định
        public async Task<ActionResult> Profile()
        {
            var currentUser = Session["CurrentUser"] as KhachHang;
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 1. Lấy lại thông tin Khách hàng mới nhất từ DB
            var kh = await _khachHangService.GetKhachHangByIdAsync(currentUser.MaKH);

            if (kh == null)
            {
                ViewBag.Error = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            // Đảm bảo HoTen không bị null
            if (string.IsNullOrEmpty(kh.HoTen))
            {
                kh.HoTen = currentUser.HoTen;
            }

            // 2. Đếm số hóa đơn cho Header
            int soDonHang = await _hoaDonService.DemHoaDonTheoKhachHangAsync(currentUser.MaKH);

            ViewBag.SoDonHang = soDonHang;

            // Đã loại bỏ ViewBag.HoaDonList

            return View(kh);
        }

        /// <summary>
        /// Action này được gọi bằng AJAX để lấy Partial View danh sách đơn hàng.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetOrderListPartial()
        {
            var currentUser = Session["CurrentUser"] as KhachHang;
            if (currentUser == null)
            {
                return Content("<div class='alert alert-danger'>Phiên làm việc hết hạn. Vui lòng đăng nhập lại.</div>");
            }

            // LẤY DANH SÁCH HÓA ĐƠN
            var hoaDonList = await _hoaDonService.GetHoaDonByKhachHangAsync(currentUser.MaKH);

            // TRẢ VỀ PARTIAL VIEW
            return PartialView("_OrderListPartial", hoaDonList);
        }

        // Action Cập nhật vẫn giữ nguyên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateProfile(KhachHang model)
        {
            // Logic xử lý cập nhật thông tin khách hàng
            // ... (Giữ nguyên logic của bạn) ...
            return RedirectToAction("Profile");
        }
    }
}