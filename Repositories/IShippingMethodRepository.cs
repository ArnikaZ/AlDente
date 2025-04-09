using AlDentev2.Models;

namespace AlDentev2.Repositories
{
    public interface IShippingMethodRepository
    {
        Task<IEnumerable<ShippingMethod>> GetAllAsync();
        Task<ShippingMethod?> GetByIdAsync(int id);
    }
}
