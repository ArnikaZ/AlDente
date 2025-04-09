using AlDentev2.Data;
using AlDentev2.Models;
using Microsoft.EntityFrameworkCore;

namespace AlDentev2.Repositories
{
    public class ShippingMethodRepository: IShippingMethodRepository
    {
        private readonly ApplicationDbContext _context;
        public ShippingMethodRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<ShippingMethod>> GetAllAsync()
        {
            return await _context.ShippingMethods.ToListAsync();
        }
        public async Task<ShippingMethod?> GetByIdAsync(int id)
        {
            return await _context.ShippingMethods.FindAsync(id);
        }
    }
}
