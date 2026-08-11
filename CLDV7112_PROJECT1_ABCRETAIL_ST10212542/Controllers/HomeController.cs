
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models;
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ABCRetailWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly FileShareService _fileShareService;
        private readonly IConfiguration _configuration;

        public HomeController(
            TableStorageService tableStorageService,
            FileShareService fileShareService,
            IConfiguration configuration)
        {
            _tableStorageService = tableStorageService;
            _fileShareService = fileShareService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _tableStorageService.GetProductsAsync();
            return View(products);
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserRole") != null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string usernameOrEmail, string password)
        {
            if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Username/Email and Password are required.");
                return View();
            }

            // Check Admin Credentials
            var adminUser = _configuration["AdminSettings:Username"];
            var adminPass = _configuration["AdminSettings:Password"];

            if (usernameOrEmail.Equals(adminUser, StringComparison.OrdinalIgnoreCase) && password == adminPass)
            {
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserName", "Administrator");
                await _fileShareService.AppendLogAsync("INFO", "Admin user logged in successfully.");
                return RedirectToAction("Index", "Admin");
            }

            // Check Customer in Table Storage
            var customers = await _tableStorageService.GetCustomersAsync();
            var customer = customers.Find(c => c.Email.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase) && c.Password == password);

            if (customer != null)
            {
                HttpContext.Session.SetString("UserRole", "Customer");
                HttpContext.Session.SetString("UserEmail", customer.Email);
                HttpContext.Session.SetString("UserName", $"{customer.FirstName} {customer.LastName}");
                HttpContext.Session.SetString("UserId", customer.RowKey);
                await _fileShareService.AppendLogAsync("INFO", $"Customer {customer.Email} logged in successfully.");
                return RedirectToAction("Index", "Customer");
            }

            ModelState.AddModelError("", "Invalid credentials.");
            await _fileShareService.AppendLogAsync("WARNING", $"Failed login attempt for user: {usernameOrEmail}");
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("UserRole") != null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(CustomerProfile customer)
        {
            if (string.IsNullOrEmpty(customer.Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return View(customer);
            }

            // Check if customer already exists
            var existing = await _tableStorageService.GetCustomersAsync();
            if (existing.Exists(c => c.Email.Equals(customer.Email, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Email", "A user with this email already exists.");
                return View(customer);
            }

            customer.PartitionKey = "Customer";
            customer.RowKey = Guid.NewGuid().ToString(); // Generate unique customer ID

            // Set validation flags
            ModelState.Remove(nameof(customer.Timestamp));
            ModelState.Remove(nameof(customer.ETag));
            ModelState.Remove(nameof(customer.PartitionKey));
            ModelState.Remove(nameof(customer.RowKey));

            if (ModelState.IsValid)
            {
                await _tableStorageService.UpsertCustomerAsync(customer);
                await _fileShareService.AppendLogAsync("INFO", $"New customer registered: {customer.Email} ({customer.FirstName} {customer.LastName})");

                // Automatically log in the user
                HttpContext.Session.SetString("UserRole", "Customer");
                HttpContext.Session.SetString("UserEmail", customer.Email);
                HttpContext.Session.SetString("UserName", $"{customer.FirstName} {customer.LastName}");
                HttpContext.Session.SetString("UserId", customer.RowKey);

                return RedirectToAction("Index", "Customer");
            }

            return View(customer);
        }

        public async Task<IActionResult> Logout()
        {
            var user = HttpContext.Session.GetString("UserEmail") ?? HttpContext.Session.GetString("UserRole") ?? "Guest";
            await _fileShareService.AppendLogAsync("INFO", $"User {user} logged out.");
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
