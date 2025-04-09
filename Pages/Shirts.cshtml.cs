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
        public async Task OnGetAsync()
        {
            Shirts = await _productRepository.GetProductsByCategoryAsync("Koszulki");
        }
    }
}
