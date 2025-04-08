using AlDentev2.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AlDentev2.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        public ProductController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllProductsAsync();
            return View(products);
        }
        public async Task<IActionResult> Category(string name)
        {
            var products = await _productRepository.GetProductsByCategoryAsync(name);
            ViewBag.CategoryName = name;
            return View(products);
        }
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if(product == null)
            {
                return NotFound();
            }
            var availableSizes = await _productRepository.GetAvailableSizesForProductAsync(id);
            return View(product);
        }
    }
    
}
