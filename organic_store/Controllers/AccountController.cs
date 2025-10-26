using organic_store.Models;
using organic_store.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;

namespace organic_store.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;

        public AccountController()
        {
            _accountService = new AccountService();
        }

        public ActionResult Login() => View();

        // POST: /Account/Login
        [HttpPost]
        public async Task<ActionResult> Login(string TenDangNhap, string MatKhau)
        {
            var user = await _accountService.LoginAsync(TenDangNhap, MatKhau);
            if (user == null)
            {
                ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu";
                return View();
            }

            Session["CurrentUser"] = user;

            if (user is KhachHang kh)
            {
                Session["MaKH"] = kh.MaKH; // Lưu MaKH vào Session
                return RedirectToAction("Index", "Home");
            }

            else
                return RedirectToAction("Dashboard", "HomeAdmin");
        }

        // GET: /Account/Register
        public ActionResult Register() => View();

        // POST: /Account/Register
        [HttpPost]
        public async Task<ActionResult> Register(KhachHang model)
        {
            // Kiểm tra xác thực model
            if (!ModelState.IsValid)
            {
                return View(model); // Lỗi xác thực sẽ được hiển thị qua ValidationMessageFor
            }

            // Kiểm tra trùng lặp
            var (exists, errorMessage) = await _accountService.CheckDuplicateAsync(model.TenDangNhap, model.Email, model.SoDienThoai);
            if (exists)
            {
                ViewBag.Error = errorMessage; // Chỉ sử dụng ViewBag.Error cho lỗi nghiệp vụ
                return View(model);
            }

            // Đăng ký tài khoản (Service sẽ tự động tạo MaKH)
            bool result = await _accountService.RegisterKhachHangAsync(model);
            if (result)
            {
                ViewBag.Success = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
                return View();
            }
            else
            {
                ViewBag.Error = "Đăng ký thất bại. Vui lòng thử lại sau.";
                return View(model);
            }
        }


        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        // Kiểm tra đăng nhập và chuyển hướng phù hợp
        public ActionResult CheckLogin()
        {
            if (Session["MaKH"] != null)
            {
                return RedirectToAction("Profile", "KhachHang");
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }
    }
}