using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlDentev2.Pages
{
    public class BagsModel : PageModel
    {
        private readonly IProductRepository _productRepository;
        public BagsModel(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public IEnumerable<Product> Bags { get; set; } = new List<Product>();
        public async Task OnGetAsync()
        {
            Bags = await _productRepository.GetProductsByCategoryAsync("Torby");
        }
    }
}
