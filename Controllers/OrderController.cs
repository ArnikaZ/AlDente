using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlDentev2.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IUserRepository _userRepository;
        public OrderController(IOrderRepository orderRepository, ICartRepository cartRepository, IUserRepository userRepository)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _userRepository = userRepository;
        }

        public async Task<IActionResult> Checkout()
        {
            int userId = GetCurrentUserId();

            var cartItems = await _cartRepository.GetCartItemsAsync(null, userId);
            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }
            var user = await _userRepository.GetUserByIdAsync(userId);
            var address = await _userRepository.GetUserAddressesAsync(userId);
            var shippingMethod = await _orderRepository.GetShippingMethodsAsync();
            var paymentMethod = await _orderRepository.GetPaymentMethodsAsync();

            ViewBag.CartItems = cartItems;
            ViewBag.Addresses = address;
            ViewBag.ShippingMethods = shippingMethod;
            ViewBag.PaymentMethods = paymentMethod;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(int shippingAddressId, int shippingMethodId, int paymentMethodId)
        {
            int userId = GetCurrentUserId();
            var cartItems = await _cartRepository.GetCartItemsAsync(null, userId);
            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }
            decimal total = cartItems.Sum(item => item.Product.Price * item.Quantity);
            var shippingMethod = await _orderRepository.GetShippingMethodsAsync();
            var shipping = shippingMethod.First(sm => sm.Id == shippingMethodId);
            total += shipping.Cost;
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = total,
                Status = OrderStatus.Pending,
                ShippingAddressId = shippingAddressId,
                ShippingMethodId = shippingMethodId,
                PaymentMethodId = paymentMethodId,
                OrderItems = cartItems.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    SizeId = item.SizeId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                }).ToList()
            };

            await _orderRepository.CreateOrderAsync(order);
            await _cartRepository.ClearCartAsync(null, userId);
            return RedirectToAction("Confirmation", new { orderId = order.Id });
        }

        public async Task<IActionResult>Confirmation(int orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        public async Task<IActionResult> History()
        {
            int userId = GetCurrentUserId();
            var orders = await _orderRepository.GetUserOrdersAsync(userId);
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderRepository.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            int userId = GetCurrentUserId();
            if (order.UserId != userId)
            {
                return Forbid();
            }
            return View(order);
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }
    }
}
