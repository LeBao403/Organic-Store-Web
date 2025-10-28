using organic_store.Models;
using organic_store.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace organic_store.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly HoaDonService _hoaDonService;
        private readonly HomeService _homeService; 

        public CartController()
        {
            _cartService = new CartService();
            _hoaDonService = new HoaDonService();
            _homeService = new HomeService();
        }

        // Hiển thị giỏ hàng
        public async Task<ActionResult> Index()
        {
            var user = Session["CurrentUser"] as KhachHang;

            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
            }

            var selectedStore = Session["SelectedStore"] as string ?? "ALL";

            // Lấy thông tin cửa hàng
            var stores = await _homeService.GetAllStoresAsync(); // Dùng _homeService vì nó đã được sửa
            ViewBag.Stores = stores;
            ViewBag.SelectedStore = selectedStore;

            // Lấy tên cửa hàng
            string storeName = stores.FirstOrDefault(ch => ch.MaCH == selectedStore)?.TenCH ?? "Tất cả";
            ViewBag.StoreName = storeName;

            // Lấy dữ liệu giỏ hàng từ Service
            var serviceCart = await _cartService.GetCartByCustomerAsync(user.MaKH, selectedStore);

            // Bắt đầu ánh xạ (mapping) và cập nhật tồn kho
            var modelCart = new List<organic_store.Models.CartItem>();

            foreach (var item in serviceCart)
            {
                // ⭐ LẤY VÀ CẬP NHẬT TỒN KHO THỰC TẾ
                long currentStock = 0;
                if (selectedStore != "ALL")
                {
                    // Chỉ lấy tồn kho nếu đang ở cửa hàng cụ thể
                    currentStock = await _homeService.GetStockByProductAsync(item.MaSP, selectedStore);
                }

                // Đảm bảo SoTon không âm
                if (currentStock < 0)
                {
                    currentStock = 0;
                }

                modelCart.Add(new organic_store.Models.CartItem
                {
                    Product = new Products
                    {
                        MaSP = item.MaSP,
                        TenSP = item.TenSP,
                        GiaBan = item.GiaBan,
                        HinhAnhURL = item.HinhAnhURL,
                        // ⭐ SỬA LỖI: Gán SoTon bằng tồn kho thực tế đã lấy
                        SoTon = currentStock,
                        TenCH = storeName
                    },
                    Quantity = item.SoLuong
                });
            }

            // Lọc lại các sản phẩm có số lượng > 0 (Nếu bạn muốn giữ lại logic này)
            modelCart = modelCart.Where(c => c.Quantity > 0).ToList();

            return View(modelCart);
        }

        // Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        public async Task<ActionResult> AddToCart(string MaSP, string TenSP, double GiaBan, string HinhAnhURL, int Quantity = 1)
        {
            var user = Session["CurrentUser"] as KhachHang;
            var selectedStore = Session["SelectedStore"] as string ?? "ALL";

            if (user == null)
            {
                string returnUrl = Request.UrlReferrer != null
                    ? Request.UrlReferrer.AbsolutePath + Request.UrlReferrer.Query
                    : Url.Action("Index", "Home");

                return Json(new
                {
                    success = false,
                    redirect = Url.Action("Login", "Account", new { returnUrl = returnUrl })
                });
            }

            if (string.IsNullOrEmpty(selectedStore) || selectedStore == "ALL")
            {
                return Json(new { success = false, message = "Vui lòng chọn cửa hàng trước khi thêm sản phẩm!" });
            }

            var stock = await _homeService.GetStockByProductAsync(MaSP, selectedStore);
            if (stock < 0)
            {
                stock = 0;
            }
            if (stock <= 0)
            {
                return Json(new { success = false, message = "Sản phẩm đã hết hàng tại cửa hàng này!" });
            }
            if (Quantity > stock)
            {
                return Json(new { success = false, message = $"Số lượng không được vượt quá {stock}!" });
            }

            await _cartService.AddToCartAsync(user.MaKH, MaSP, Quantity, selectedStore);

            var cartCount = (await _cartService.GetCartByCustomerAsync(user.MaKH, selectedStore)).Sum(x => x.SoLuong);

            return Json(new { success = true, count = cartCount, message = "✅ Đã thêm sản phẩm vào giỏ hàng." });
        }

        [HttpPost]
        public async Task<ActionResult> UpdateQuantity(string maSP, int quantity, string maCH)
        {
            var user = Session["CurrentUser"] as KhachHang;

            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng đăng nhập để cập nhật giỏ hàng.",
                    redirect = Url.Action("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") })
                });
            }

            var stock = await _homeService.GetStockByProductAsync(maSP, maCH);
            if (stock < 0)
            {
                stock = 0;
            }
            if (stock <= 0)
            {
                return Json(new { success = false, message = " Sản phẩm đã hết hàng tại cửa hàng này!" });
            }

            if (quantity > stock)
            {
                return Json(new { success = false, message = $"Số lượng không được vượt quá {stock}!" });
            }
            if (quantity <= 0)
            {
                // Cho phép quantity = 0 để xóa sản phẩm khỏi giỏ hàng
                await _cartService.RemoveFromCartAsync(user.MaKH, maSP, maCH);
                return Json(new { success = true, isRemoved = true });
            }

            await _cartService.UpdateQuantityAsync(user.MaKH, maSP, quantity, maCH);
            return Json(new { success = true });
        }

        //Xóa sản phẩm
        [HttpPost]
        public async Task<ActionResult> RemoveFromCart(string maSP, string maCH)
        {
            var user = Session["CurrentUser"] as KhachHang;

            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng đăng nhập để xóa sản phẩm.",
                    redirect = Url.Action("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") })
                });
            }

            await _cartService.RemoveFromCartAsync(user.MaKH, maSP, maCH);
            return Json(new { success = true });
        }

        //  Lấy số lượng sản phẩm trong giỏ
        [HttpGet]
        public async Task<ActionResult> GetCartCount()
        {
            var user = Session["CurrentUser"] as KhachHang;
            var selectedStore = Session["SelectedStore"] as string ?? "ALL";

            if (user == null)
            {
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }

            var cart = await _cartService.GetCartByCustomerAsync(user.MaKH, selectedStore);
            var count = cart.Sum(x => x.SoLuong);

            return Json(new { count }, JsonRequestBehavior.AllowGet);
        }



        // Checkout: Hiển thị form đặt hàng
        [HttpGet]
        public async Task<ActionResult> Checkout()
        {
            var user = Session["CurrentUser"] as KhachHang;
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
            }

            var selectedStore = Session["SelectedStore"] as string ?? "ALL";
            var cart = await _cartService.GetCartByCustomerAsync(user.MaKH, selectedStore);

            if (cart == null || cart.Count == 0)
            {
                return RedirectToAction("Index");
            }

            ViewBag.Cart = cart;
            ViewBag.ThanhTien = cart.Sum(item => item.ThanhTien);
            ViewBag.SelectedStore = selectedStore;

            return View();
        }

        // Checkout: Xử lý form và tạo hóa đơn
        [HttpPost]
        public async Task<ActionResult> Checkout(string DiaChiGiaoHang, string GhiChu)
        {
            var user = Session["CurrentUser"] as KhachHang;
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var selectedStore = Session["SelectedStore"] as string ?? "ALL";
            var cart = await _cartService.GetCartByCustomerAsync(user.MaKH, selectedStore);

            if (cart == null || cart.Count == 0)
            {
                return RedirectToAction("Index");
            }

            var thanhTien = cart.Sum(item => item.ThanhTien);
            var items = cart.Select(item => new Dictionary<string, object>
            {
                { "MaSP", item.MaSP },
                { "SoLuong", item.SoLuong }
            }).ToList();

            var trangThai = "Chưa xử lý";
            var trangThaiThanhToan = "Chưa thanh toán";

            DiaChiGiaoHang = string.IsNullOrEmpty(DiaChiGiaoHang) ? "Chưa cung cấp địa chỉ" : DiaChiGiaoHang;
            GhiChu = string.IsNullOrEmpty(GhiChu) ? "Không có ghi chú" : GhiChu;

            if (_hoaDonService == null)
            {
                throw new Exception("HoaDonService không được khởi tạo.");
            }

            await _hoaDonService.CreateHoaDonAsync(user.MaKH, thanhTien, DiaChiGiaoHang, DateTime.UtcNow, GhiChu, trangThaiThanhToan, trangThai, selectedStore, items);

            foreach (var item in cart)
            {
                await _cartService.RemoveFromCartAsync(user.MaKH, item.MaSP, selectedStore);
            }

            TempData["SuccessMessage"] = "Đã đặt hàng thành công!";
            return RedirectToAction("Index", "Home");
        }
    }
}