using organic_store.Services;
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

        public async Task<ActionResult> Index(string maCH = "ALL", string maDM = "ALL", int page = 1)
        {
            if (!string.IsNullOrEmpty(maCH))
            {
                Session["SelectedStore"] = maCH;
            }
            else if (Session["SelectedStore"] == null)
            {
                Session["SelectedStore"] = "ALL";
            }


            var selectedStore = Session["SelectedStore"] as string;

            // 1. Lấy danh sách cần thiết cho View và Layout
            var stores = await _homeService.GetAllStoresAsync();
            var categories = await _homeService.GetAllCategoriesAsync();

            // 2. Thiết lập ViewBag cho Layout và View
            ViewBag.Stores = stores;
            ViewBag.Categories = categories; 
            ViewBag.SelectedStore = selectedStore; 
            ViewBag.CurrentMaCH = selectedStore; 
            ViewBag.SelectedMaDM = maDM;

            var products = await _homeService.GetAllProductsAsync(selectedStore);
            ViewBag.Page = 1;
            ViewBag.PageSize = 10;
            ViewBag.TotalCount = products.Count();
            return View(products);
        }

        
        public async Task<ActionResult> Search(string q, string maCH)
        {
            if (string.IsNullOrEmpty(maCH))
            {
                maCH = Session["SelectedStore"] as string ?? "ALL";
            }
            else
            {
                Session["SelectedStore"] = maCH;
            }

            var products = await _homeService.SearchProductsAsync(q, maCH);
            ViewBag.CurrentMaCH = maCH;
            return PartialView("_ProductPartial", products);
        }

        [HttpPost]
        public ActionResult SetSelectedStore(string maCH)
        {
            Session["SelectedStore"] = maCH ?? "ALL";
            return Json(new { success = true });
        }

        // Nhận tham số MaCH VÀ MaDM để lọc
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