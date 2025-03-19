using AlDentev2.Data;
using AlDentev2.Models;
using Microsoft.EntityFrameworkCore;

namespace AlDentev2.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<Address> CreateAddressAsync(Address address)
        {
            if(!await _context.Addresses.AnyAsync(a => a.UserId == address.UserId))
            {
                address.IsDefault = true;
            }
            await _context.Addresses.AddAsync(address);
            await _context.SaveChangesAsync();
            if (address.IsDefault)
            {
                await SetDefaultAddressAsync(address.UserId, address.Id);
            }
            return address;
        }

        public async Task<User> CreateUserAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task DeleteAddressAsync(int addressId)
        {
            var address = await _context.Addresses.FindAsync(addressId);
            if (address != null)
            {
                if (address.IsDefault)
                {
                    var user = await _context.Users.FindAsync(address.UserId);
                    if (user != null)
                    {
                        user.DefaultAddress = null;
                        _context.Users.Update(user);
                    }
                }
                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Address?> GetAddressByIdAsync(int addressId)
        {
            return await _context.Addresses.FindAsync(addressId);
        }

        public async Task<IEnumerable<Address>> GetUserAddressesAsync(int userId)
        {
            return await _context.Addresses
                .Where(a => userId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.DefaultAddress)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.DefaultAddress)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task SetDefaultAddressAsync(int userId, int addressId)
        {
            var userAddresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .ToListAsync();
            foreach(var address in userAddresses)
            {
                address.IsDefault = (address.Id == addressId);
                _context.Addresses.Update(address);
            }
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.DefaultAddressId = addressId;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAddressAsync(Address address)
        {
            _context.Addresses.Update(address);
            await _context.SaveChangesAsync();
            if (address.IsDefault)
            {
                await SetDefaultAddressAsync(address.UserId, address.Id);
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
