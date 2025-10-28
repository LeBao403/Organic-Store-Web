using organic_store.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace organic_store.Controllers
{
    public class HomeController : Controller
    {
        private readonly HomeService _homeService = new HomeService();

        public async Task<ActionResult> Index(string maCH)
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
            var stores = await _homeService.GetAllStoresAsync();
            ViewBag.Stores = stores;
            ViewBag.SelectedStore = selectedStore;
            ViewBag.CurrentMaCH = selectedStore;

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
    }
}