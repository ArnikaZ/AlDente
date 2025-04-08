using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AlDentev2.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly ICartRepository _cartRepository;

        public UserController(IUserRepository userRepository, ICartRepository cartRepository)
        {
            _userRepository = userRepository;
            _cartRepository = cartRepository;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Nieprawidłowy email lub hasło");
                return View();
            }


            //logika logowania np. przy użyciu ASP.NET Identity

            string sessionId = HttpContext.Session.Id;
            await _cartRepository.TransferCartAsync(sessionId, user.Id);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Hasła nie są identycze");
                return View();
            }
            var existingUser = await _userRepository.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Użytkownik z tym adresem email już istnieje");
                return View();
            }

            var user = new User
            {
                Email = email,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.CreateUserAsync(user);

            //logika logowania po rejestracji

            string sessionId = HttpContext.Session.Id;
            await _cartRepository.TransferCartAsync(sessionId, user.Id);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            int userId = GetCurrentUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phoneNumber)
        {
            int userId = GetCurrentUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = phoneNumber;
            await _userRepository.UpdateUserAsync(user);
            return RedirectToAction("Profile");
        }

        [Authorize]
        public async Task<IActionResult> Addresses()
        {
            int userId = GetCurrentUserId();
            var addressess = await _userRepository.GetUserAddressesAsync(userId);
            return View(addressess);
        }
        [Authorize]
        public IActionResult AddAddress()
        {
            return View();
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddAddress(Address address)
        {
            int userId = GetCurrentUserId();
            address.UserId = userId;
            await _userRepository.CreateAddressAsync(address);
            return RedirectToAction("Addresses");
        }

        [Authorize]
        public async Task<IActionResult> EditAddress(int id)
        {
            var address = await _userRepository.GetAddressByIdAsync(id);
            if (address == null)
            {
                return NotFound();
            }
            int userId = GetCurrentUserId();
            if (address.UserId != userId)
            {
                return Forbid();
            }
            return View(address);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditAddress(Address address)
        {
            int userId = GetCurrentUserId();
            if (address.UserId != userId)
            {
                return Forbid();
            }
            await _userRepository.UpdateAddressAsync(address);
            return RedirectToAction("Addresses");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var address = await _userRepository.GetAddressByIdAsync(id);
            if (address == null)
            {
                return NotFound();
            }
            int userId = GetCurrentUserId();
            if (address.UserId != userId)
            {
                return Forbid();
            }
            await _userRepository.DeleteAddressAsync(id);
            return RedirectToAction("Addresses");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var address = await _userRepository.GetAddressByIdAsync(id);
            if (address == null)
            {
                return NotFound();
            }
            int userId = GetCurrentUserId();
            if (address.UserId != userId)
            {
                return Forbid();
            }
            await _userRepository.SetDefaultAddressAsync(userId, id);
            return RedirectToAction("Addresses");
        }

        public IActionResult Logout()
        {
            //logika wylogowania np SingInMaganer.SingOutAAsync()
            return RedirectToAction("Index", "Home");
        }
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }
        private string HashPassword(string password)
        {
            using(var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}
