using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlDentev2.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        public CartController(ICartRepository cartRepository, IProductRepository productRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
        }
        public async Task<IActionResult> Index()
        {
            string sessionId = HttpContext.Session.Id;
            int? userId = null;
            if (User.Identity.IsAuthenticated)
            {
                userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }
            var cartItems = _cartRepository.GetCartItemsAsync(sessionId, userId);
            return View(cartItems);
        }
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int sizeId, int quantity)
        {
            string sessionId = HttpContext.Session.Id;
            int? userId = null;
            if (User.Identity.IsAuthenticated)
            {
                userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }
            var cartItem = new CartItem
            {
                ProductId = productId,
                SizeId = sizeId,
                Quantity = quantity,
                SessionId = sessionId,
                UserId = userId
            };
            await _cartRepository.AddItemToCartAsync(cartItem);
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var cartItem = await _cartRepository.GetCartItemAsync(cartItemId);
            if (cartItem == null)
            {
                return NotFound();
            }
            cartItem.Quantity = quantity;
            await _cartRepository.UpdateCartItemAsync(cartItem);
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            await _cartRepository.RemoveCartItemAsync(cartItemId);
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            string sessionId = HttpContext.Session.Id;
            int? userId = null;
            if (User.Identity.IsAuthenticated)
            {
                userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }
            await _cartRepository.ClearCartAsync(sessionId, userId);
            return RedirectToAction("Index");
        }
        
    }
}
