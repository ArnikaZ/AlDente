using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlDentev2.Pages
{
    public class ProductDetailsModel : PageModel
    {
        private readonly IProductRepository _productRepository;
        private readonly ICartRepository _cartRepository;
        public ProductDetailsModel(IProductRepository productRepository, ICartRepository cartRepository)
        {
            _productRepository = productRepository;
            _cartRepository = cartRepository;
        }
        public Product? Product { get; set; }
        public IEnumerable<Size> AvailableSizes { get; set; } = new List<Size>();
        public Dictionary<int, int> StockQuantities { get; set; } = new Dictionary<int, int>();

        [BindProperty]
        public int ProductId { get; set; }

        [BindProperty]
        public int SelectedSizeId { get; set; }

        [BindProperty]
        public int Quantity { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ProductId = id;
            Product = await _productRepository.GetProductByIdAsync(id);
            if (Product == null)
            {
                return NotFound();
            }
            ProductId = id;
            AvailableSizes = await _productRepository.GetAvailableSizesForProductAsync(id);
            if (Product.ProductSizes != null)
            {
                foreach (var productSize in Product.ProductSizes)
                {
                    StockQuantities[productSize.SizeId] = productSize.StockQuantity;
                }
            }

            return Page();
        }
       

    }
}
