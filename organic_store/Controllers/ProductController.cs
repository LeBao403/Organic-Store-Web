using System.Threading.Tasks;
using System.Web.Mvc;
using organic_store.Models;
using organic_store.Services;

namespace organic_store.Controllers
{
    public class ProductController : Controller
    {
        private readonly HomeService _homeService = new HomeService();

        // Hiển thị chi tiết sản phẩm theo MaSP
        public async Task<ActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return HttpNotFound();

            var selectedStore = Session["SelectedStore"] as string ?? "ALL";
            var allProducts = await _homeService.GetAllProductsAsync(selectedStore);
            var product = allProducts.Find(p => p.MaSP == id);

            if (product == null)
                return HttpNotFound();

            ViewBag.SelectedStore = selectedStore;
            return View(product);
        }
    }
}