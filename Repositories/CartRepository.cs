using AlDentev2.Data;
using AlDentev2.Models;
using Microsoft.EntityFrameworkCore;

namespace AlDentev2.Repositories
{

    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;
        public CartRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task AddItemToCartAsync(CartItem cartItem)
        {
            var existingItem = await GetCartItemAsync(cartItem.ProductId, cartItem.SizeId, cartItem.SessionId, cartItem.UserId); //spr czy produkt już istnieje w koszyku
            if (existingItem != null)
            {
                existingItem.Quantity += cartItem.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                await UpdateCartItemAsync(existingItem);
            }
            else
            {
                cartItem.CreatedAt = DateTime.UtcNow;
                cartItem.UpdatedAt = DateTime.UtcNow;
                await _context.CartItems.AddAsync(cartItem);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(string? sessionId, int? userId)
        {
            IQueryable<CartItem> query = _context.CartItems;
            if (userId.HasValue)
            {
                query = query.Where(ci => ci.UserId == userId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                query = query.Where(ci => ci.SessionId == sessionId);
            }
            else
            {
                return;
            }
            var cartItems = await query.ToListAsync();
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
        }

        //do pobrania pojedynczego elementu koszyka do aktualizacji ilości/ usuwania pozycji z koszyka
        public async Task<CartItem?> GetCartItemAsync(int cartItemId)
        {
            return await _context.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Size)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);
        }

        //sprawdza, czy produkt w konkretnym rozmiarze jest już w koszyku
        public async Task<CartItem?> GetCartItemAsync(int productId, int sizeId, string? sessionId, int? userId)
        {
            IQueryable<CartItem> query = _context.CartItems;
            if (userId.HasValue)
            {
                query = query.Where(ci => ci.UserId == userId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                query = query.Where(ci => ci.SessionId == sessionId);
            }
            else
            {
                return null;
            }
            return await query.FirstOrDefaultAsync(ci => ci.ProductId == productId && ci.SizeId == sizeId);
        }

        //zwraca wszystkie elementy koszyka dla określonego użytkownika do wyświetlenia zawartości koszyka na stronie/ obliczania sumy zamówienia/ Procesu składania zamówienia, gdy potrzebujemy przenieść wszystkie elementy z koszyka do zamówienia
        public async Task<IEnumerable<CartItem>> GetCartItemsAsync(string? sessionId, int? userId)
        {
            IQueryable<CartItem> query = _context.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Size);
            if (userId.HasValue)
            {
                query = query.Where(ci => ci.UserId == userId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                query = query.Where(ci => ci.SessionId == sessionId);
            }
            else
            {
                return Enumerable.Empty<CartItem>();
            }
            return await query.ToListAsync();
        }

        public async Task RemoveCartItemAsync(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
        }

        //do przenoszenia zawartości koszyka z sesji do konta po zalogowaniu
        public async Task TransferCartAsync(string sessionId, int userId)
        {
            var sessionCartItems = await _context.CartItems
                .Where(ci => ci.SessionId == sessionId)
                .ToListAsync();
            var userCartItems = await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .ToListAsync();
            foreach(var item in sessionCartItems)
            {
                var existingUserItem = userCartItems.FirstOrDefault(ci => ci.ProductId == item.ProductId && ci.SizeId == item.SizeId);
                if (existingUserItem != null)
                {
                    existingUserItem.Quantity += item.Quantity;
                    existingUserItem.UpdatedAt = DateTime.UtcNow;
                    _context.CartItems.Update(existingUserItem);
                    _context.CartItems.Remove(item);
                }
                else
                {
                    item.UserId = userId;
                    item.SessionId = null;
                    item.UpdatedAt = DateTime.UtcNow;
                    _context.CartItems.Update(item);
                }
            }
            await _context.SaveChangesAsync();
            
        }

        public async Task UpdateCartItemAsync(CartItem cartItem)
        {
            cartItem.UpdatedAt = DateTime.UtcNow;
            _context.CartItems.Update(cartItem);
            await _context.SaveChangesAsync();
        }
    }
}
