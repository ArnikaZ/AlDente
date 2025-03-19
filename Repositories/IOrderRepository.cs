using AlDentev2.Models;

namespace AlDentev2.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
        Task<Order> CreateOrderAsync(Order order);
        Task UpdateOrderAsync(Order order);
        Task<IEnumerable<ShippingMethod>> GetShippingMethodsAsync();
        Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync();
    }
}
