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

        public async Task<ActionResult> Index(int page = 1)
        {
            int pageSize = 9;
            var allProducts = await _homeService.GetAllProductsAsync();

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
        public async Task<ActionResult> Search(string q)
        {
            string keyword = q?.Trim() ?? "";
            var result = await _homeService.SearchProductsAsync(keyword);
            return PartialView("_ProductPartial", result);
        }

    }
}