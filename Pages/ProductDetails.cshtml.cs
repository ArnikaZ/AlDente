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
        public async Task<IActionResult> OnPostAddToCartAsync()
        {
            if (ProductId <= 0 || SelectedSizeId <= 0 || Quantity <= 0)
            {
                return BadRequest("Nieprawid³owe dane produktu.");
            }
            var product = await _productRepository.GetProductByIdAsync(ProductId);
            if (product == null)
            {
                return NotFound("Produkt nie zosta³ znaleziony.");
            }
            var availableSizes = await _productRepository.GetAvailableSizesForProductAsync(ProductId);
            if (!availableSizes.Any(s => s.Id == SelectedSizeId))
            {
                return BadRequest("Wybrany rozmiar nie jest dostêpny.");
            }
            var stockQuantity = product.ProductSizes?.FirstOrDefault(ps => ps.SizeId == SelectedSizeId)?.StockQuantity ?? 0;
            if (stockQuantity < Quantity)
            {
                return BadRequest($"Nie ma wystarczaj¹cej iloœci produktu w magazynie. Dostêpne: {stockQuantity}");
            }
            string sessionId = HttpContext.Session.Id;
            var cartItem = new CartItem
            {
                ProductId = ProductId,
                SizeId = SelectedSizeId,
                Quantity = Quantity,
                SessionId = sessionId,
                UserId = null, // Jeœli u¿ytkownik jest zalogowany, ustaw jego ID
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _cartRepository.AddItemToCartAsync(cartItem);
            StatusMessage = "Produkt zosta³ dodany do koszyka";
            return RedirectToPage("/ShoppingCart");
        }
    }
}
