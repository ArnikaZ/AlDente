using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlDentev2.Pages
{
    public class ShirtsModel : PageModel
    {
        private readonly IProductRepository _productRepository;
        public ShirtsModel(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public IEnumerable<Product> Shirts { get; set; } = new List<Product>();
        public Dictionary<int, IEnumerable<Size>> AvailableSizes { get; set; } = new Dictionary<int, IEnumerable<Size>>();

        public Dictionary<string, int> StockQuantities { get; set; } = new Dictionary<string, int>();
        public async Task OnGetAsync()
        {
            Shirts = await _productRepository.GetProductsByCategoryAsync("Koszulki");
            foreach (var shirt in Shirts)
            {
                var sizes = await _productRepository.GetAvailableSizesForProductAsync(shirt.Id);
                AvailableSizes[shirt.Id] = sizes;
                if (shirt.ProductSizes != null)
                {
                    foreach (var productSize in shirt.ProductSizes)
                    {
                        var key = $"{shirt.Id}-{productSize.SizeId}";
                        StockQuantities[key] = productSize.StockQuantity;
                    }
                }
            }
        }
        public bool IsSizeAvailable(int productId, int sizeId)
        {
            var key = $"{productId}-{sizeId}";
            return StockQuantities.ContainsKey(key) && StockQuantities[key] > 0;
        }

        // Metoda pomocnicza do uzyskania iloœci dostêpnych sztuk danego produktu w danym rozmiarze
        public int GetStockQuantity(int productId, int sizeId)
        {
            var key = $"{productId}-{sizeId}";
            return StockQuantities.ContainsKey(key) ? StockQuantities[key] : 0;
        }
    }
}
