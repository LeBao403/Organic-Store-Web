// organic_store.Controllers/HomeController.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using organic_store.Models;
using organic_store.Services;

namespace organic_store.Controllers
{
    public class HomeController : Controller
    {
        private readonly HomeService _homeService = new HomeService();

        // FIX: Nhận tham số MaCH VÀ MaDM để lọc sản phẩm
        public async Task<ActionResult> Index(string maCH = "ALL", string maDM = "ALL", int page = 1)
        {
            int pageSize = 9;

            // 1. Lấy danh sách cần thiết cho View và Layout
            var stores = await _homeService.GetAllStoresAsync();
            var categories = await _homeService.GetAllCategoriesAsync(); // Lấy danh mục

            // 2. Thiết lập ViewBag cho Layout và View
            ViewBag.Stores = stores;
            ViewBag.Categories = categories; // Truyền danh mục lên Layout

            ViewBag.SelectedStore = maCH;    // Mã CH đang chọn
            ViewBag.CurrentMaCH = maCH;      // Dùng cho phân trang
            ViewBag.SelectedMaDM = maDM;     // Mã DM đang chọn (cần cho cả Navbar và Home View)

            // 3. Lấy sản phẩm đã lọc
            var allProducts = await _homeService.GetAllProductsAsync(maCH, maDM);

            // 4. Phân trang
            var paged = allProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Page = page;
            ViewBag.TotalCount = allProducts.Count;
            ViewBag.PageSize = pageSize;

            return View(paged);
        }

        // Tìm kiếm (AJAX Partial)
        // FIX: Nhận tham số MaCH VÀ MaDM để lọc
        public async Task<ActionResult> Search(string q, string maCH = "ALL", string maDM = "ALL")
        {
            string keyword = q?.Trim() ?? "";
            var result = await _homeService.SearchProductsAsync(keyword, maCH, maDM);
            return PartialView("_ProductPartial", result);
        }

        public ActionResult AboutUs()
        {
            return View();
        }
    }
}