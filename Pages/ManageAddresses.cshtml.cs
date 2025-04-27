using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AlDentev2.Pages
{
    public class ManageAddressesModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ManageAddressesModel> _logger;

        public ManageAddressesModel(UserManager<User> userManager, IUserRepository userRepository, ILogger<ManageAddressesModel> logger)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _logger = logger;
        }

        public IEnumerable<Address> Addresses { get; set; } = new List<Address>();

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Adres jest wymagany")]
            [StringLength(50, ErrorMessage = "Adres nie mo¿e przekraczaæ 50 znaków")]
            public string AddressLine1 { get; set; } = string.Empty;

            [StringLength(50, ErrorMessage = "Dodatkowy adres nie mo¿e przekraczaæ 50 znaków")]
            public string? AddressLine2 { get; set; }

            [Required(ErrorMessage = "Miasto jest wymagane")]
            [StringLength(50, ErrorMessage = "Miasto nie mo¿e przekraczaæ 50 znaków")]
            [RegularExpression(@"^[^\d]*$", ErrorMessage = "Miasto nie mo¿e zawieraæ liczb")]
            public string City { get; set; } = string.Empty;

            [Required(ErrorMessage = "Kod pocztowy jest wymagany")]
            [StringLength(10, ErrorMessage = "Kod pocztowy nie mo¿e przekraczaæ 10 znaków")]
            [RegularExpression(@"^\d{2}-\d{3}$", ErrorMessage = "Kod pocztowy musi byæ w formacie 00-000")]
            public string PostalCode { get; set; } = string.Empty;

            [Required(ErrorMessage = "Kraj jest wymagany")]
            [StringLength(50, ErrorMessage = "Kraj nie mo¿e przekraczaæ 50 znaków")]
            [RegularExpression(@"^[^\d]*$", ErrorMessage = "Kraj nie mo¿e zawieraæ liczb")]
            public string Country { get; set; } = string.Empty;

            public bool IsDefault { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAddressAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            // Log form data for debugging
            _logger.LogInformation("Received address data: Id={Id}, AddressLine1={AddressLine1}, City={City}, PostalCode={PostalCode}, Country={Country}, IsDefault={IsDefault}",
                Input.Id, Input.AddressLine1, Input.City, Input.PostalCode, Input.Country, Input.IsDefault);

            // Fallback for invalid Id
            if (ModelState.ContainsKey("Input.Id") && ModelState["Input.Id"].Errors.Any(e => e.ErrorMessage.Contains("invalid")))
            {
                Input.Id = 0; // Set to 0 for new addresses
                ModelState.ClearValidationState("Input.Id");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(kvp => kvp.Value.Errors.Select(e => $"{kvp.Key}: {e.ErrorMessage}"))
                    .ToList();
                StatusMessage = "B³¹d walidacji: " + string.Join("; ", errors);
                _logger.LogWarning("Validation failed: {Errors}", string.Join("; ", errors));
                Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                return Page();
            }

            try
            {
                _logger.LogInformation("Processing address save for user {UserId}, AddressId: {AddressId}", user.Id, Input.Id);
                if (Input.Id == 0) // Nowy adres
                {
                    var address = new Address
                    {
                        UserId = user.Id,
                        AddressLine1 = Input.AddressLine1,
                        AddressLine2 = Input.AddressLine2,
                        City = Input.City,
                        PostalCode = Input.PostalCode,
                        Country = Input.Country,
                        IsDefault = Input.IsDefault
                    };
                    await _userRepository.CreateAddressAsync(address);
                    StatusMessage = "Adres zosta³ dodany pomyœlnie.";
                    _logger.LogInformation("Address created for user {UserId}, AddressId: {AddressId}", user.Id, address.Id);
                }
                else // Edycja istniej¹cego adresu
                {
                    var address = await _userRepository.GetAddressByIdAsync(Input.Id);
                    if (address == null || address.UserId != user.Id)
                    {
                        StatusMessage = "B³¹d: Adres nie istnieje lub nie nale¿y do u¿ytkownika.";
                        _logger.LogWarning("Address {AddressId} not found or does not belong to user {UserId}", Input.Id, user.Id);
                        Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                        return Page();
                    }

                    address.AddressLine1 = Input.AddressLine1;
                    address.AddressLine2 = Input.AddressLine2;
                    address.City = Input.City;
                    address.PostalCode = Input.PostalCode;
                    address.Country = Input.Country;
                    address.IsDefault = Input.IsDefault;

                    await _userRepository.UpdateAddressAsync(address);
                    StatusMessage = "Adres zosta³ zaktualizowany pomyœlnie.";
                    _logger.LogInformation("Address updated for user {UserId}, AddressId: {AddressId}", user.Id, address.Id);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"B³¹d podczas zapisywania adresu: {ex.Message}";
                _logger.LogError(ex, "Error saving address for user {UserId}, AddressId: {AddressId}", user.Id, Input.Id);
                Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                return Page();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSetDefaultAsync(int addressId)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            var address = await _userRepository.GetAddressByIdAsync(addressId);
            if (address == null || address.UserId != user.Id)
            {
                StatusMessage = "B³¹d: Adres nie istnieje lub nie nale¿y do u¿ytkownika.";
                _logger.LogWarning("Address {AddressId} not found or does not belong to user {UserId}", addressId, user.Id);
                Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                return Page();
            }

            await _userRepository.SetDefaultAddressAsync(user.Id, addressId);
            StatusMessage = "Adres zosta³ pomyœlnie ustawiony jako domyœlny.";
            _logger.LogInformation("Address {AddressId} set as default for user {UserId}", addressId, user.Id);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int addressId)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            var address = await _userRepository.GetAddressByIdAsync(addressId);
            if (address == null || address.UserId != user.Id)
            {
                StatusMessage = "B³¹d: Adres nie istnieje lub nie nale¿y do u¿ytkownika.";
                _logger.LogWarning("Address {AddressId} not found or does not belong to user {UserId}", addressId, user.Id);
                Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                return Page();
            }

            if (address.IsDefault)
            {
                StatusMessage = "B³¹d: Nie mo¿na usun¹æ domyœlnego adresu. Najpierw ustaw inny adres jako domyœlny.";
                _logger.LogWarning("Attempted to delete default address {AddressId} for user {UserId}", addressId, user.Id);
                Addresses = await _userRepository.GetUserAddressesAsync(user.Id);
                return Page();
            }

            await _userRepository.DeleteAddressAsync(addressId);
            StatusMessage = "Adres zosta³ usuniêty pomyœlnie.";
            _logger.LogInformation("Address {AddressId} deleted for user {UserId}", addressId, user.Id);
            return RedirectToPage();
        }
    }
}
