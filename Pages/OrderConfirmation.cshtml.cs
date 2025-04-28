using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlDentev2.Pages
{
    public class OrderConfirmationModel : PageModel
    {
        private readonly IOrderRepository _orderRepository;
        private readonly UserManager<User> _userManager;

        public OrderConfirmationModel(IOrderRepository orderRepository, UserManager<User> userManager)
        {
            _orderRepository = orderRepository;
            _userManager = userManager;
        }

        public Order Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            Order = await _orderRepository.GetOrderByIdAsync(id);
            if (Order == null || Order.UserId != user.Id)
            {
                return Page();
            }

            return Page();
        }
    }
}
