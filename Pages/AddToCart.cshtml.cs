using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace AlDentev2.Pages
{
    public class AddToCartModel : PageModel
    {
        private readonly ILogger<AddToCartModel> _logger;
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly UserManager<User> _userManager;

        public AddToCartModel(ILogger<AddToCartModel> logger, ICartRepository cartRepository, IProductRepository productRepository, UserManager<User> userManager)
        {
            _logger = logger;
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _userManager = userManager;
        }

        [BindProperty]
        public int ProductId { get; set; }

        [BindProperty]
        public int? SizeId { get; set; }

        [BindProperty]
        public int Quantity { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("AddToCart OnPostAsync: ProductId={ProductId}, SizeId={SizeId}, Quantity={Quantity}", ProductId, SizeId, Quantity);
            _logger.LogInformation("User.Identity.IsAuthenticated={IsAuthenticated}", User.Identity.IsAuthenticated);
            _logger.LogInformation("User.Identity.Name={Name}", User.Identity.Name ?? "null");
            _logger.LogInformation("Cookies: {Cookies}", string.Join(", ", Request.Cookies.Keys));

            // SprawdŸ, czy produkt ma dostêpne rozmiary
            var availableSizes = await _productRepository.GetAvailableSizesForProductAsync(ProductId);
            if (availableSizes.Any() && (!SizeId.HasValue || SizeId == 0))
            {
                _logger.LogWarning("Rozmiar nie zosta³ wybrany (SizeId jest null lub 0), a produkt ma dostêpne rozmiary: ProductId={ProductId}", ProductId);
                TempData["ErrorMessage"] = "Proszê wybraæ rozmiar produktu.";
                return RedirectToPage("/ProductDetails", new { id = ProductId });
            }

            // Ustaw domyœlny SizeId = 7 ("Brak") dla produktów bez rozmiarów
            if (!availableSizes.Any())
            {
                SizeId = 7; // Domyœlny rozmiar "Brak" dla toreb
                _logger.LogInformation("Ustawiono domyœlny SizeId=7 (Brak) dla produktu bez rozmiarów: ProductId={ProductId}", ProductId);
            }

            int? userId = null;
            if (User.Identity.IsAuthenticated)
            {
                _logger.LogInformation("ClaimsPrincipal: IsAuthenticated={IsAuthenticated}", User.Identity.IsAuthenticated);
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation("Claim: Type={Type}, Value={Value}", claim.Type, claim.Value);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    userId = user.Id;
                    _logger.LogInformation("U¿ytkownik zalogowany: UserId={UserId}", userId);
                }
                else
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
                    {
                        userId = parsedUserId;
                        _logger.LogInformation("U¿ytkownik zalogowany (z Claims): UserId={UserId}", userId);
                    }
                    else
                    {
                        _logger.LogWarning("Nie mo¿na pobraæ UserId z ClaimsPrincipal");
                    }
                }
            }
            else
            {
                _logger.LogInformation("U¿ytkownik niezalogowany");
            }

            // Wymuszenie inicjalizacji sesji
            if (!HttpContext.Session.IsAvailable)
            {
                _logger.LogWarning("Sesja nie jest dostêpna!");
                HttpContext.Session.SetString("Initialized", "true"); // Inicjalizuj sesjê
                _logger.LogInformation("Sesja zainicjalizowana. Nowy SessionId={SessionId}", HttpContext.Session.Id);
            }
            else
            {
                _logger.LogInformation("Sesja jest dostêpna. SessionId={SessionId}", HttpContext.Session.Id);
            }

            // Wymuszenie zapisu ciasteczka sesji przez zapis wartoœci
            HttpContext.Session.SetString("CartSession", HttpContext.Session.Id);
            _logger.LogInformation("Zapisano CartSession w sesji. SessionId={SessionId}", HttpContext.Session.Id);

            var sessionId = HttpContext.Session.Id;
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogError("SessionId jest puste!");
                return RedirectToPage("/Error");
            }

            _logger.LogInformation("Przed dodaniem do koszyka: ProductId={ProductId}, SizeId={SizeId}, Quantity={Quantity}, SessionId={SessionId}, UserId={UserId}", ProductId, SizeId, Quantity, sessionId, userId);

            var cartItem = new CartItem
            {
                ProductId = ProductId,
                SizeId = (int)SizeId,
                Quantity = Quantity,
                SessionId = userId == null ? sessionId : null,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _cartRepository.AddItemToCartAsync(cartItem);
            _logger.LogInformation("Produkt dodany do koszyka: ProductId={ProductId}, SizeId={SizeId}, SessionId={SessionId}, UserId={UserId}", ProductId, SizeId, cartItem.SessionId, cartItem.UserId);

            return RedirectToPage("/ShoppingCart");
        }
    }
}
