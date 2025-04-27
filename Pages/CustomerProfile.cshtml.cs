using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlDentev2.Pages
{
   
        public class CustomerProfileModel : PageModel
        {
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;

        public CustomerProfileModel(UserManager<User> userManager, IUserRepository userRepository, IOrderRepository orderRepository)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
        }

        public User? User { get; set; }
        public Address? DefaultAddress { get; set; }
        public IEnumerable<Order> Orders { get; set; } = new List<Order>();

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Use HttpContext.User (ClaimsPrincipal) instead of User model
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            User = user;
            DefaultAddress = await _userRepository.GetAddressByIdAsync(user.DefaultAddressId ?? 0);
            Orders = await _orderRepository.GetUserOrdersAsync(user.Id);

            return Page();
        }
    }
}
