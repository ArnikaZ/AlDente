using AlDentev2.Data;
using AlDentev2.Models;
using Microsoft.EntityFrameworkCore;

namespace AlDentev2.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes).ThenInclude(ps => ps.Size)
                .ToListAsync();
        }
        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string categoryName)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes).ThenInclude(ps => ps.Size)
                .Where(p => p.Category.Name == categoryName)
                .ToListAsync();
        }

        public async Task <Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .Include(p=>p.Category)
                .Include(p=>p.ProductSizes).ThenInclude(ps=>ps.Size)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<IEnumerable<Size>> GetAvailableSizesForProductAsync(int productId)
        {
            return await _context.ProductSizes
                .Where(p => p.ProductId == productId && p.StockQuantity > 0)
                .Select(p => p.Size)
                .ToListAsync();
        }
    }
}
