using AlDentev2.Data;
using AlDentev2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AlDentev2.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public UserRepository(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Address> CreateAddressAsync(Address address)
        {
            if (!await _context.Addresses.AnyAsync(a => a.UserId == address.UserId))
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
            // Use UserManager for user creation
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            return user;
        }

        public async Task DeleteAddressAsync(int addressId)
        {
            var address = await _context.Addresses.FindAsync(addressId);
            if (address != null)
            {
                if (address.IsDefault)
                {
                    var user = await _userManager.FindByIdAsync(address.UserId.ToString());
                    if (user != null)
                    {
                        user.DefaultAddress = null;
                        await _userManager.UpdateAsync(user);
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
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _userManager.FindByIdAsync(userId.ToString());
        }

        public async Task SetDefaultAddressAsync(int userId, int addressId)
        {
            var userAddresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .ToListAsync();
            foreach (var address in userAddresses)
            {
                address.IsDefault = (address.Id == addressId);
                _context.Addresses.Update(address);
            }
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                user.DefaultAddressId = addressId;
                await _userManager.UpdateAsync(user);
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
            await _userManager.UpdateAsync(user);
        }
    }
}
