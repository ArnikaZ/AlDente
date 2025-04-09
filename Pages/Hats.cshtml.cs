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
        public async Task OnGetAsync()
        {
            Hats = await _productRepository.GetProductsByCategoryAsync("Czapki");
        }
    }
}
