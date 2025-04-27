using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlDentev2.Pages
{
    public class ShoppingCartModel : PageModel
    {
        private readonly ICartRepository _cartRepository;
        private readonly IShippingMethodRepository _shippingMethodRepository;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ShoppingCartModel> _logger;

        public ShoppingCartModel(ICartRepository cartRepository, IShippingMethodRepository shippingMethodRepository, UserManager<User> userManager, ILogger<ShoppingCartModel> logger)
        {
            _cartRepository = cartRepository;
            _shippingMethodRepository = shippingMethodRepository;
            _userManager = userManager;
            _logger = logger;
        }
        public IEnumerable<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Discount { get; set; }
        public decimal Total => SubTotal + ShippingCost - Discount;
        public IEnumerable<ShippingMethod> ShippingMethods { get; set; } = new List<ShippingMethod>();
        public ShippingMethod? SelectedShippingMethod { get; set; }
        [TempData]
        public string? StatusMessage { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            string sessionId = HttpContext.Session.Id;
            int? userId = null;
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                userId = user.Id;
            }
            _logger.LogInformation("ShoppingCart OnGetAsync: SessionId={SessionId}, UserId={UserId}, IsAuthenticated={IsAuthenticated}", sessionId, userId, User.Identity.IsAuthenticated);
            CartItems = await _cartRepository.GetCartItemsAsync(sessionId, userId);
            _logger.LogInformation("ShoppingCart OnGetAsync: CartItems count={Count}", CartItems.Count());
            foreach (var item in CartItems)
            {
                _logger.LogInformation("CartItem: Id={Id}, ProductId={ProductId}, SizeId={SizeId}, Quantity={Quantity}, UserId={UserId}", item.Id, item.ProductId, item.SizeId, item.Quantity, item.UserId);
            }
            SubTotal = CartItems.Sum(item => item.Product?.Price * item.Quantity ?? 0);
            ShippingMethods = await _shippingMethodRepository.GetAllAsync();
            SelectedShippingMethod = ShippingMethods.FirstOrDefault();
            if (SelectedShippingMethod != null)
            {
                ShippingCost = SelectedShippingMethod.Cost;
            }
            return Page();
        }
        public async Task<IActionResult> OnPostUpdateCartItemAsync(int cartItemId, int quantity)
        {
            if (quantity < 1)
            {
                return await OnPostRemoveCartItemAsync(cartItemId);
            }
            var cartItem = await _cartRepository.GetCartItemAsync(cartItemId);
            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                await _cartRepository.UpdateCartItemAsync(cartItem);
                StatusMessage = "Koszyk zosta³ zaktualizowany";
            }
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostRemoveCartItemAsync(int cartItemId)
        {
            await _cartRepository.RemoveCartItemAsync(cartItemId);
            StatusMessage = "Produkt zosta³ usuniêty z koszyka";
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostApplyDiscountAsync(string discountCode)
        {
            ///logika weryfikacji kodów rabatowych
            Discount = 20.00m;
            StatusMessage = "Kod rabatowy zosta³ zastosowany";
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostUpdateShippingMethodAsync(int shippingMethodId)
        {
            var shippingMethod = await _shippingMethodRepository.GetByIdAsync(shippingMethodId);
            if (shippingMethod != null)
            {
                SelectedShippingMethod = shippingMethod;
                ShippingCost = shippingMethod.Cost;
            }
            return RedirectToPage();
        }
        
    }
}
