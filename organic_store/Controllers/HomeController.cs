// organic_store.Controllers/HomeController.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using organic_store.Services;

namespace organic_store.Controllers
{
    public class HomeController : Controller
    {
        private readonly HomeService _homeService = new HomeService();

        // CẬP NHẬT: Nhận thêm tham số MaCH (mã cửa hàng)
        public async Task<ActionResult> Index(string maCH = "ALL", int page = 1)
        {
            int pageSize = 9;

            // Lấy danh sách cửa hàng để hiển thị Dropdown
            var stores = await _homeService.GetAllStoresAsync();
            ViewBag.Stores = stores;
            ViewBag.SelectedStore = maCH; // Gán giá trị đang được chọn

            // Lấy sản phẩm theo maCH
            var allProducts = await _homeService.GetAllProductsAsync(maCH);

            var paged = allProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Page = page;
            ViewBag.TotalCount = allProducts.Count;
            ViewBag.PageSize = pageSize;

            ViewBag.CurrentMaCH = maCH;

            return View(paged);
        }

        // Tìm kiếm (AJAX Partial)
        // CẬP NHẬT: Nhận thêm tham số MaCH
        public async Task<ActionResult> Search(string q, string maCH = "ALL")
        {
            string keyword = q?.Trim() ?? "";
            var result = await _homeService.SearchProductsAsync(keyword, maCH);
            return PartialView("_ProductPartial", result);
        }

    }
}