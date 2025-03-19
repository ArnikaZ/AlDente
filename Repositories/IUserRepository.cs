using AlDentev2.Models;

namespace AlDentev2.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<IEnumerable<Address>> GetUserAddressesAsync(int userId);
        Task<Address?> GetAddressByIdAsync(int addressId);
        Task<Address> CreateAddressAsync(Address address);
        Task UpdateAddressAsync(Address address);
        Task DeleteAddressAsync(int addressId);
        Task SetDefaultAddressAsync(int userId, int addressId);
    }
}
