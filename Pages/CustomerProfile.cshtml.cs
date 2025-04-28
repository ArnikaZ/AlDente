using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AlDentev2.Pages
{
   
        public class CustomerProfileModel : PageModel
        {
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly SignInManager<User> _signInManager;

        public CustomerProfileModel(
            UserManager<User> userManager,
            IUserRepository userRepository,
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _signInManager = signInManager;
        }

        public User? User { get; set; }
        public Address? DefaultAddress { get; set; }
        public IEnumerable<Order> Orders { get; set; } = new List<Order>();

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public ChangePasswordInputModel ChangePasswordInput { get; set; } = new ChangePasswordInputModel();

        public class ChangePasswordInputModel
        {
            [Required(ErrorMessage = "Bie¿¹ce has³o jest wymagane")]
            [DataType(DataType.Password)]
            [Display(Name = "Bie¿¹ce has³o")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Nowe has³o jest wymagane")]
            [StringLength(100, ErrorMessage = "Has³o musi zawieraæ co najmniej {2} znaków", MinimumLength = 8)]
            [RegularExpression(@"^(?=.*\d)(?=.*[A-Z])(?=.*[@#$%^&+=!]).{8,}$", ErrorMessage = "Has³o musi zawieraæ co najmniej jedn¹ cyfrê, jedn¹ wielk¹ literê i jeden znak specjalny (@#$%^&+=!)")]
            [DataType(DataType.Password)]
            [Display(Name = "Nowe has³o")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Potwierdzenie has³a jest wymagane")]
            [DataType(DataType.Password)]
            [Display(Name = "PotwierdŸ nowe has³o")]
            [Compare("NewPassword", ErrorMessage = "Has³a nie s¹ jednakowe")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
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

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            if (!ModelState.IsValid)
            {
                User = user;
                DefaultAddress = await _userRepository.GetAddressByIdAsync(user.DefaultAddressId ?? 0);
                Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
                return Page();
            }

            // Verify current password
            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, ChangePasswordInput.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                ModelState.AddModelError("ChangePasswordInput.CurrentPassword", "Bie¿¹ce has³o jest nieprawid³owe.");
                User = user;
                DefaultAddress = await _userRepository.GetAddressByIdAsync(user.DefaultAddressId ?? 0);
                Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
                return Page();
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, ChangePasswordInput.CurrentPassword, ChangePasswordInput.NewPassword);
            if (result.Succeeded)
            {
                StatusMessage = "Has³o zosta³o zmienione pomyœlnie.";
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            User = user;
            DefaultAddress = await _userRepository.GetAddressByIdAsync(user.DefaultAddressId ?? 0);
            Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAccountAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            try
            {
                // Check if the user has any active orders
                var orders = await _orderRepository.GetUserOrdersAsync(user.Id);
                if (orders.Any(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Delivered))
                {
                    StatusMessage = "Nie mo¿na usun¹æ konta, poniewa¿ istniej¹ aktywne zamówienia.";
                    User = user;
                    DefaultAddress = await _userRepository.GetAddressByIdAsync(user.DefaultAddressId ?? 0);
                    Orders = orders;
                    return Page();
                }

                // Delete associated cart items
                await _cartRepository.ClearCartAsync(null, user.Id);

                // Delete user addresses
                var addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                foreach (var address in addresses)
                {
                    await _userRepository.DeleteAddressAsync(address.Id);
                }

                // Delete the user account
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    StatusMessage = "Wyst¹pi³ b³¹d podczas usuwania konta. Spróbuj ponownie.";
                    User = user;
                    DefaultAddress = await _userRepository.GetAddressByIdAsync(user.DefaultAddressId ?? 0);
                    Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
                    return Page();
                }

                // Sign out the user
                await _signInManager.SignOutAsync();

                // Redirect to the homepage with a success message
                TempData["StatusMessage"] = "Twoje konto zosta³o usuniête pomyœlnie.";
                return RedirectToPage("/Index");
            }
            catch (DbUpdateException ex)
            {
                // Handle potential database constraint violations
                StatusMessage = "Nie mo¿na usun¹æ konta z powodu powi¹zanych danych. Skontaktuj siê z obs³ug¹.";
                User = user;
                DefaultAddress = await _userRepository.GetAddressByIdAsync(user.DefaultAddressId ?? 0);
                Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
                return Page();
            }
        }
    }
}
