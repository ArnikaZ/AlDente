using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlDentev2.Pages
{
    public class ShoppingCartModel : PageModel
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IShippingMethodRepository _shippingMethodRepository;

        public ShoppingCartModel(ICartRepository cartRepository, IProductRepository productRepository, IShippingMethodRepository shippingMethodRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _shippingMethodRepository = shippingMethodRepository;
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
        public async Task <IActionResult> OnGetAsync()
        {
            string sessionId = HttpContext.Session.Id;
            CartItems = await _cartRepository.GetCartItemsAsync(sessionId, null); /// id = null?
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
