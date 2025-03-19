using AlDentev2.Models;

namespace AlDentev2.Repositories
{
    /// <summary>
    /// Interfejs odpowiedzialny za zarządzanie koszykiem zakupowym
    /// </summary>
    public interface ICartRepository
    {
        Task<IEnumerable<CartItem>> GetCartItemsAsync(string? sessionId, int? userId);
        Task<CartItem?> GetCartItemAsync(int cartItemId);
        Task<CartItem?> GetCartItemAsync(int productId, int sizeId, string? sessionId, int? userId);
        Task AddItemToCartAsync(CartItem cartItem);
        Task UpdateCartItemAsync(CartItem cartItem);
        Task RemoveCartItemAsync(int cartItemId);
        Task ClearCartAsync(string? sessionId, int? userId);
        Task TransferCartAsync(string sessionId, int userId);
    }
}
