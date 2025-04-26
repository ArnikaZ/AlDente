using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AlDentev2.Pages
{
    public class UserAccountModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserAccountModel> _logger;

        public UserAccountModel(UserManager<User> userManager, IOrderRepository orderRepository, IUserRepository userRepository, ILogger<UserAccountModel> logger)
        {
            _userManager = userManager;
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        [BindProperty(Name = "UserInput")]
        public InputModel Input { get; set; } = new InputModel();

        [BindProperty(Name = "AddressInput")]
        public AddressInputModel AddressInput { get; set; } = new AddressInputModel();

        public User CurrentUser { get; set; } = null!;

        public IEnumerable<Order> Orders { get; set; } = new List<Order>();

        public IEnumerable<Address> Addresses { get; set; } = new List<Address>();

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [StringLength(50, ErrorMessage = "Imiê nie mo¿e przekraczaæ 50 znaków")]
            [Display(Name = "Imiê")]
            public string? FirstName { get; set; }

            [StringLength(50, ErrorMessage = "Nazwisko nie mo¿e przekraczaæ 50 znaków")]
            [Display(Name = "Nazwisko")]
            public string? LastName { get; set; }

            [Phone(ErrorMessage = "Nieprawid³owy numer telefonu")]
            [Display(Name = "Numer telefonu")]
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "Email jest wymagany")]
            [EmailAddress(ErrorMessage = "Nieprawid³owy format adresu email")]
            [Display(Name = "Adres e-mail")]
            public string Email { get; set; } = string.Empty;
        }

        public class AddressInputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Adres jest wymagany")]
            [StringLength(50, ErrorMessage = "Adres nie mo¿e przekraczaæ 50 znaków")]
            [Display(Name = "Adres (ulica, numer domu)")]
            public string AddressLine1 { get; set; } = string.Empty;

            [StringLength(50, ErrorMessage = "Dodatkowe informacje nie mog¹ przekraczaæ 50 znaków")]
            [Display(Name = "Dodatkowe informacje (np. numer mieszkania)")]
            public string? AddressLine2 { get; set; }

            [Required(ErrorMessage = "Miasto jest wymagane")]
            [StringLength(50, ErrorMessage = "Miasto nie mo¿e przekraczaæ 50 znaków")]
            [Display(Name = "Miasto")]
            public string City { get; set; } = string.Empty;

            [Required(ErrorMessage = "Kod pocztowy jest wymagany")]
            [StringLength(10, ErrorMessage = "Kod pocztowy nie mo¿e przekraczaæ 10 znaków")]
            [Display(Name = "Kod pocztowy")]
            public string PostalCode { get; set; } = string.Empty;

            [Required(ErrorMessage = "Kraj jest wymagany")]
            [StringLength(50, ErrorMessage = "Kraj nie mo¿e przekraczaæ 50 znaków")]
            [Display(Name = "Kraj")]
            public string Country { get; set; } = string.Empty;

            [Display(Name = "Ustaw jako domyœlny")]
            public bool IsDefault { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            CurrentUser = user;
            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email ?? string.Empty
            };

            Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
            Addresses = await _userRepository.GetUserAddressesAsync(user.Id);

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
                Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
                Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                return Page();
            }

            // Sprawdzenie, czy email siê zmieni³ i czy jest unikalny
            if (user.Email != Input.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("UserInput.Email", "Ten adres email jest ju¿ u¿ywany.");
                    CurrentUser = user;
                    Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
                    Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                    return Page();
                }

                user.Email = Input.Email;
                user.UserName = Input.Email;
                user.EmailConfirmed = false;
            }

            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.PhoneNumber = Input.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (user.Email != Input.Email)
                {
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = System.Text.Encodings.Web.UrlEncoder.Default.Encode(code);
                    var callbackUrl = Url.Page(
                    "/LoginPage",
                    pageHandler: "ConfirmEmail",
                    values: new { userId = user.Id, code = code },
                    protocol: Request.Scheme);

                    // TODO: Wys³anie emaila z potwierdzeniem (u¿yj IEmailSender)
                    StatusMessage = "Dane zosta³y zaktualizowane. SprawdŸ swoj¹ skrzynkê pocztow¹, aby potwierdziæ nowy adres e-mail.";
                }
                else
                {
                    StatusMessage = "Dane zosta³y zaktualizowane.";
                }
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                CurrentUser = user;
                Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
                Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                return Page();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddAddressAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Próba dodania adresu przez niezalogowanego u¿ytkownika.");
                return RedirectToPage("/LoginPage");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Nieprawid³owy stan modelu dla AddressInput: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                CurrentUser = user;
                Input = new InputModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email ?? string.Empty
                };
                Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
                Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                return Page();
            }

            try
            {
                var address = new Address
                {
                    UserId = user.Id,
                    AddressLine1 = AddressInput.AddressLine1,
                    AddressLine2 = AddressInput.AddressLine2,
                    City = AddressInput.City,
                    PostalCode = AddressInput.PostalCode,
                    Country = AddressInput.Country,
                    IsDefault = AddressInput.IsDefault
                };

                await _userRepository.CreateAddressAsync(address);
                StatusMessage = "Adres zosta³ dodany.";
                _logger.LogInformation("Adres zosta³ dodany dla u¿ytkownika {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas dodawania adresu dla u¿ytkownika {UserId}", user.Id);
                StatusMessage = "Wyst¹pi³ b³¹d podczas dodawania adresu. Spróbuj ponownie.";
                CurrentUser = user;
                Input = new InputModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email ?? string.Empty
                };
                Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
                Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                return Page();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAddressAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            if (!ModelState.IsValid)
            {
                CurrentUser = user;
                Input = new InputModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email ?? string.Empty
                };
                Orders = await _orderRepository.GetUserOrdersAsync(user.Id);
                Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                return Page();
            }

            var address = await _userRepository.GetAddressByIdAsync(AddressInput.Id);
            if (address == null || address.UserId != user.Id)
            {
                StatusMessage = "Adres nie istnieje lub nie nale¿y do Ciebie.";
                return RedirectToPage();
            }

            address.AddressLine1 = AddressInput.AddressLine1;
            address.AddressLine2 = AddressInput.AddressLine2;
            address.City = AddressInput.City;
            address.PostalCode = AddressInput.PostalCode;
            address.Country = AddressInput.Country;
            address.IsDefault = AddressInput.IsDefault;

            await _userRepository.UpdateAddressAsync(address);
            StatusMessage = "Adres zosta³ zaktualizowany.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAddressAsync(int addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            var address = await _userRepository.GetAddressByIdAsync(addressId);
            if (address == null || address.UserId != user.Id)
            {
                StatusMessage = "Adres nie istnieje lub nie nale¿y do Ciebie.";
                return RedirectToPage();
            }

            await _userRepository.DeleteAddressAsync(addressId);
            StatusMessage = "Adres zosta³ usuniêty.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSetDefaultAddressAsync(int addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            var address = await _userRepository.GetAddressByIdAsync(addressId);
            if (address == null || address.UserId != user.Id)
            {
                StatusMessage = "Adres nie istnieje lub nie nale¿y do Ciebie.";
                return RedirectToPage();
            }

            await _userRepository.SetDefaultAddressAsync(user.Id, addressId);
            StatusMessage = "Adres zosta³ ustawiony jako domyœlny.";

            return RedirectToPage();
        }
    }

    }
