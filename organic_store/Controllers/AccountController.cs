using organic_store.Models;
using organic_store.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;


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
                Session["MaKH"] = kh.MaKH; 

                var cartService = new CartService();
                var maCH = Session["SelectedStore"] as string ?? "ALL";

                var guestCart = Session["TempCart"] as List<TempCartItem>;

                if (guestCart != null && guestCart.Any())
                {
                    var cartToMerge = guestCart.Where(i => i.MaCH == maCH || maCH == "ALL").ToList();

                    if (cartToMerge.Any())
                    {
                        

                        guestCart.RemoveAll(i => i.MaCH == maCH || maCH == "ALL");
                        Session["TempCart"] = guestCart;
                    }
                }

                return RedirectToAction("Index", "Home");
            }

            else
                return RedirectToAction("Dashboard", "HomeAdmin");
        }

        public ActionResult Register() => View();

        [HttpPost]
        public async Task<ActionResult> Register(KhachHang model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); 
            }

            var (exists, errorMessage) = await _accountService.CheckDuplicateAsync(model.TenDangNhap, model.Email, model.SoDienThoai);
            if (exists)
            {
                ViewBag.Error = errorMessage; 
                return View(model);
            }

            var (exists, errorMessage) = await _accountService.CheckDuplicateAsync(model.TenDangNhap, model.Email, model.SoDienThoai);
            if (exists)
            {
                ViewBag.Error = errorMessage;
                return View(model);
            }

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