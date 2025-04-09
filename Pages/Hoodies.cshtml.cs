using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Client;

namespace AlDentev2.Pages
{
    public class HoodiesModel : PageModel
    {
        private readonly IProductRepository _productRepository;

        public HoodiesModel(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public IEnumerable<Product> Hoodies { get; set; } = new List<Product>();
        public Dictionary<int, IEnumerable<Size>> AvailableSizes { get; set; } = new Dictionary<int, IEnumerable<Size>>();

        public Dictionary<string, int> StockQuantities { get; set; } = new Dictionary<string, int>();
        public async Task OnGetAsync()
        {
            Hoodies = await _productRepository.GetProductsByCategoryAsync("Bluzy");
            foreach (var hoodie in Hoodies)
            {
                var sizes = await _productRepository.GetAvailableSizesForProductAsync(hoodie.Id);
                AvailableSizes[hoodie.Id] = sizes;
                if (hoodie.ProductSizes != null)
                {
                    foreach (var productSize in hoodie.ProductSizes)
                    {
                        var key = $"{hoodie.Id}-{productSize.SizeId}";
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
