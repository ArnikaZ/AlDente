using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlDentev2.Pages
{
    public class HatsModel : PageModel
    {
        private readonly IProductRepository _productRepository;
        public HatsModel(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public IEnumerable<Product> Hats { get; set; } = new List<Product>();
        public Dictionary<int, IEnumerable<Size>> AvailableSizes { get; set; } = new Dictionary<int, IEnumerable<Size>>();

        public Dictionary<string, int> StockQuantities { get; set; } = new Dictionary<string, int>();
        public async Task OnGetAsync()
        {
            Hats = await _productRepository.GetProductsByCategoryAsync("Czapki");
            foreach(var hat in Hats)
            {
                var sizes = await _productRepository.GetAvailableSizesForProductAsync(hat.Id);
                AvailableSizes[hat.Id] = sizes;
                if (hat.ProductSizes != null)
                {
                    foreach(var productSize in hat.ProductSizes)
                    {
                        var key = $"{hat.Id}-{productSize.SizeId}";
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

        public int GetStockQuantity(int productId, int sizeId)
        {
            var key = $"{productId}-{sizeId}";
            return StockQuantities.ContainsKey(key) ? StockQuantities[key] : 0;
        }
    }
}
