using AlDentev2.Data;
using AlDentev2.Models;
using Microsoft.EntityFrameworkCore;

namespace AlDentev2.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;
        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;   
        }
        public async Task<Order> CreateOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order;
        }

        //pobieranie informacji o zamówieniu na podstawie id
        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
               .Include(o => o.User)
               .Include(o => o.ShippingAddress)
               .Include(o => o.ShippingMethod)
               .Include(o => o.PaymentMethod)
               .Include(o => o.OrderItems)
                   .ThenInclude(oi => oi.Product)
               .Include(o => o.OrderItems)
                   .ThenInclude(oi => oi.Size)
               .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync()
        {
            return await _context.PaymentMethods.ToListAsync();
        }

        public async Task<IEnumerable<ShippingMethod>> GetShippingMethodsAsync()
        {
            return await _context.ShippingMethods.ToListAsync();
        }

        //wyświetlenie zamówień danego użytkownika
        public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.ShippingMethod)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o=>o.OrderItems).ThenInclude(oi=>oi.Size)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task UpdateOrderAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }
    }
}
