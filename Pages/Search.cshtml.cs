using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlDentev2.Pages
{
    public class SearchModel : PageModel
    {
        private readonly IProductRepository _productRepository;

        public SearchModel(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public IEnumerable<Product> SearchResults { get; set; } = new List<Product>();
        public string Query { get; set; } = string.Empty;

        public async Task OnGetAsync(string query)
        {
            Query = query?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(Query))
            {
                SearchResults = await _productRepository.SearchProductsAsync(Query);
            }
            else
            {
                SearchResults = new List<Product>();
            }
        }
    }
}
