using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlDentev2.Pages
{
    public class OrderConfirmationModel : PageModel
    {
        private readonly IOrderRepository _orderRepository;

        public OrderConfirmationModel(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public Order? Order { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Order = await _orderRepository.GetOrderByIdAsync(id);

            if (Order == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (userId != Order.UserId)
            {
                return Forbid();
            }

            return Page();
        }

        private int? GetCurrentUserId()
        {
            // Na razie prosta implementacja, póŸniej mo¿na u¿yæ ASP.NET Core Identity
            var authCookie = Request.Cookies["AuthCookie"];
            if (string.IsNullOrEmpty(authCookie))
            {
                return null;
            }

            try
            {
                var cookieValue = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(authCookie));
                var claims = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(cookieValue);
                if (claims != null && claims.TryGetValue("sub", out var userIdString))
                {
                    return int.Parse(userIdString);
                }
            }
            catch
            {
                // Ignoruj b³êdy, u¿ytkownik niezalogowany
            }

            return null;
        }
    }
}
