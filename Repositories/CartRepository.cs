using AlDentev2.Data;
using AlDentev2.Models;
using Microsoft.EntityFrameworkCore;

namespace AlDentev2.Repositories
{

    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;
        private ILogger<CartRepository> _logger;
        public CartRepository(ApplicationDbContext context, ILogger<CartRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task AddItemToCartAsync(CartItem cartItem)
        {
            _logger.LogInformation("AddItemToCart: ProductId={ProductId}, SizeId={SizeId}, Quantity={Quantity}, SessionId={SessionId}, UserId={UserId}",
                cartItem.ProductId, cartItem.SizeId, cartItem.Quantity, cartItem.SessionId, cartItem.UserId);

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == cartItem.ProductId);

            if (product == null)
            {
                _logger.LogError("Produkt nie istnieje: ProductId={ProductId}", cartItem.ProductId);
                throw new ArgumentException("Produkt nie istnieje.");
            }

            if (product.Category.Name != "Torby" && cartItem.SizeId == 0)
            {
                _logger.LogError("Rozmiar wymagany dla produktu: ProductId={ProductId}", cartItem.ProductId);
                throw new ArgumentException("Rozmiar jest wymagany dla tego produktu.");
            }

            var existingItem = await GetCartItemAsync(cartItem.ProductId, cartItem.SizeId, cartItem.SessionId, cartItem.UserId);
            if (existingItem != null)
            {
                existingItem.Quantity += cartItem.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                _context.CartItems.Update(existingItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Zaktualizowano istniejący element koszyka: CartItemId={CartItemId}", existingItem.Id);
            }
            else
            {
                cartItem.CreatedAt = DateTime.UtcNow;
                cartItem.UpdatedAt = DateTime.UtcNow;
                await _context.CartItems.AddAsync(cartItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Dodano nowy element koszyka: CartItemId={CartItemId}", cartItem.Id);
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
            _logger.LogInformation("GetCartItemAsync: ProductId={ProductId}, SizeId={SizeId}, SessionId={SessionId}, UserId={UserId}", productId, sizeId, sessionId, userId);

            IQueryable<CartItem> query = _context.CartItems;

            if (userId.HasValue)
            {
                query = query.Where(ci => ci.UserId == userId && ci.ProductId == productId && ci.SizeId == sizeId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                query = query.Where(ci => ci.SessionId == sessionId && ci.ProductId == productId && ci.SizeId == sizeId);
            }
            else
            {
                _logger.LogWarning("Brak SessionId i UserId w GetCartItemAsync");
                return null;
            }

            var result = await query.FirstOrDefaultAsync();
            _logger.LogInformation("GetCartItemAsync: Found={Found}", result != null);
            return result;
        }

        //zwraca wszystkie elementy koszyka dla określonego użytkownika do wyświetlenia zawartości koszyka na stronie/ obliczania sumy zamówienia/ Procesu składania zamówienia, gdy potrzebujemy przenieść wszystkie elementy z koszyka do zamówienia
        public async Task<IEnumerable<CartItem>> GetCartItemsAsync(string? sessionId, int? userId)
        {
            _logger.LogInformation("GetCartItemsAsync: SessionId={SessionId}, UserId={UserId}", sessionId, userId);

            IQueryable<CartItem> query = _context.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Size); // To ładuje Size, ale używa INNER JOIN

            // Zastąp Include dla Size na ręczne zapytanie z LEFT JOIN
            query = _context.CartItems
                .Include(ci => ci.Product)
                .GroupJoin(_context.Sizes,
                    ci => ci.SizeId,
                    s => s.Id,
                    (ci, sizes) => new { CartItem = ci, Sizes = sizes })
                .SelectMany(
                    x => x.Sizes.DefaultIfEmpty(),
                    (x, s) => new CartItem
                    {
                        Id = x.CartItem.Id,
                        ProductId = x.CartItem.ProductId,
                        Product = x.CartItem.Product,
                        SizeId = x.CartItem.SizeId,
                        Size = s, // Może być null dla SizeId = 0
                        Quantity = x.CartItem.Quantity,
                        SessionId = x.CartItem.SessionId,
                        UserId = x.CartItem.UserId,
                        CreatedAt = x.CartItem.CreatedAt,
                        UpdatedAt = x.CartItem.UpdatedAt
                    });

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
                _logger.LogWarning("Brak SessionId i UserId w GetCartItemsAsync");
                return Enumerable.Empty<CartItem>();
            }

            var result = await query.ToListAsync();
            _logger.LogInformation("GetCartItemsAsync: Zwrócono {Count} elementów", result.Count);
            return result;
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
