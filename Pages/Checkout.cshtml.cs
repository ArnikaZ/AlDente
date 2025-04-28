using AlDentev2.Data;
using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AlDentev2.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly ICartRepository _cartRepository;
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IShippingMethodRepository _shippingMethodRepository;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly Services.IEmailSender _emailSender;
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(
            ICartRepository cartRepository,
            IUserRepository userRepository,
            IOrderRepository orderRepository,
            IShippingMethodRepository shippingMethodRepository,
            UserManager<User> userManager,
            ApplicationDbContext context,
            Services.IEmailSender emailSender,
            ILogger<CheckoutModel> logger)
        {
            _cartRepository = cartRepository;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
            _shippingMethodRepository = shippingMethodRepository;
            _userManager = userManager;
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        public User CurrentUser { get; set; }
        public IEnumerable<CartItem> CartItems { get; set; } = new List<CartItem>();
        public IEnumerable<Address> Addresses { get; set; } = new List<Address>();
        public IEnumerable<ShippingMethod> ShippingMethods { get; set; } = new List<ShippingMethod>();
        public IEnumerable<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total => SubTotal + ShippingCost;

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required(ErrorMessage = "Wybierz adres wysy³ki")]
            public int ShippingAddressId { get; set; }

            [Required(ErrorMessage = "Wybierz metodê wysy³ki")]
            public int ShippingMethodId { get; set; }

            [Required(ErrorMessage = "Wybierz metodê p³atnoœci")]
            public int PaymentMethodId { get; set; }
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Próba dostêpu do strony Checkout przez niezalogowanego u¿ytkownika.");
                return RedirectToPage("/LoginPage");
            }

            CurrentUser = user;
            CartItems = await _cartRepository.GetCartItemsAsync(null, user.Id);
            if (!CartItems.Any())
            {
                StatusMessage = "Koszyk jest pusty. Dodaj produkty, aby kontynuowaæ.";
                return RedirectToPage("/ShoppingCart");
            }

            Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
            ShippingMethods = await _shippingMethodRepository.GetAllAsync();
            PaymentMethods = await _orderRepository.GetPaymentMethodsAsync();

            SubTotal = CartItems.Sum(item => item.Product?.Price * item.Quantity ?? 0);
            var defaultShippingMethod = ShippingMethods.FirstOrDefault();
            if (defaultShippingMethod != null)
            {
                ShippingCost = defaultShippingMethod.Cost;
                Input.ShippingMethodId = defaultShippingMethod.Id;
            }

            var defaultAddress = Addresses.FirstOrDefault(a => a.IsDefault);
            if (defaultAddress != null)
            {
                Input.ShippingAddressId = defaultAddress.Id;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            if (!ModelState.IsValid)
            {
                CurrentUser = user;
                CartItems = await _cartRepository.GetCartItemsAsync(null, user.Id);
                Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                ShippingMethods = await _shippingMethodRepository.GetAllAsync();
                PaymentMethods = await _orderRepository.GetPaymentMethodsAsync();
                SubTotal = CartItems.Sum(item => item.Product?.Price * item.Quantity ?? 0);
                return Page();
            }

            var cartItems = await _cartRepository.GetCartItemsAsync(null, user.Id);
            if (!cartItems.Any())
            {
                StatusMessage = "Koszyk jest pusty. Dodaj produkty, aby kontynuowaæ.";
                return RedirectToPage("/ShoppingCart");
            }

            var shippingAddress = await _userRepository.GetAddressByIdAsync(Input.ShippingAddressId);
            if (shippingAddress == null || shippingAddress.UserId != user.Id)
            {
                ModelState.AddModelError("Input.ShippingAddressId", "Nieprawid³owy adres wysy³ki.");
                return await OnGetAsync();
            }

            var shippingMethod = await _shippingMethodRepository.GetByIdAsync(Input.ShippingMethodId);
            if (shippingMethod == null)
            {
                ModelState.AddModelError("Input.ShippingMethodId", "Nieprawid³owa metoda wysy³ki.");
                return await OnGetAsync();
            }

            var paymentMethod = await _context.PaymentMethods.FindAsync(Input.PaymentMethodId);
            if (paymentMethod == null || !paymentMethod.IsActive)
            {
                ModelState.AddModelError("Input.PaymentMethodId", "Nieprawid³owa lub nieaktywna metoda p³atnoœci.");
                return await OnGetAsync();
            }

            var productSizes = new Dictionary<int, ProductSize>();
            foreach (var item in cartItems)
            {
                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId && ps.SizeId == item.SizeId);
                if (productSize == null || productSize.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError("", $"Produkt {item.Product?.Name} (rozmiar {item.Size?.Name}) jest niedostêpny w ¿¹danej iloœci.");
                    return await OnGetAsync();
                }
                productSizes.Add(item.ProductId + item.SizeId * 1000, productSize);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    UserId = user.Id,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = cartItems.Sum(item => item.Product?.Price * item.Quantity ?? 0) + shippingMethod.Cost,
                    Status = OrderStatus.Pending,
                    ShippingAddressId = Input.ShippingAddressId,
                    ShippingMethodId = Input.ShippingMethodId,
                    PaymentMethodId = Input.PaymentMethodId,
                    OrderItems = cartItems.Select(item => new OrderItem
                    {
                        ProductId = item.ProductId,
                        SizeId = item.SizeId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product?.Price ?? 0
                    }).ToList()
                };

                await _orderRepository.CreateOrderAsync(order);

                foreach (var item in cartItems)
                {
                    var productSize = productSizes[item.ProductId + item.SizeId * 1000];
                    productSize.StockQuantity -= item.Quantity;
                    _context.ProductSizes.Update(productSize);
                }
                await _context.SaveChangesAsync();

                var emailContent = $@"<h2>Potwierdzenie zamówienia #{order.Id}</h2>
                    <p>Dziêkujemy za z³o¿enie zamówienia w dniu {order.OrderDate:dd.MM.yyyy HH:mm}.</p>
                    <p>Suma: {order.TotalAmount:N2} z³</p>
                    <p>Szczegó³y zamówienia mo¿esz zobaczyæ w swoim profilu.</p>";
                await _emailSender.SendEmailAsync(user.Email, $"Potwierdzenie zamówienia #{order.Id}", emailContent);

                await _cartRepository.ClearCartAsync(null, user.Id);

                await transaction.CommitAsync();

                _logger.LogInformation("Zamówienie utworzone: OrderId={OrderId}, UserId={UserId}", order.Id, user.Id);
                return RedirectToPage("/OrderConfirmation", new { id = order.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "B³¹d podczas tworzenia zamówienia dla u¿ytkownika {UserId}. ShippingAddressId: {ShippingAddressId}, ShippingMethodId: {ShippingMethodId}, PaymentMethodId: {PaymentMethodId}",
                    user.Id, Input.ShippingAddressId, Input.ShippingMethodId, Input.PaymentMethodId);
                StatusMessage = "Wyst¹pi³ b³¹d podczas sk³adania zamówienia. Spróbuj ponownie.";
                return await OnGetAsync();
            }
        }
    }
}
