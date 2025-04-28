using AlDentev2.Models;

namespace AlDentev2.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(string categoryName);
        Task<Product?> GetProductByIdAsync(int id);
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<IEnumerable<Size>> GetAvailableSizesForProductAsync(int productId);
        Task<IEnumerable<Product>> SearchProductsAsync(string query);
    }
}
