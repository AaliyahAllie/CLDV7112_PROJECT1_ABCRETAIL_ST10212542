
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services;
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileShareService _fileShareService;

        public CustomerController(
            TableStorageService tableStorageService,
            QueueStorageService queueStorageService,
            FileShareService fileShareService)
        {
            _tableStorageService = tableStorageService;
            _queueStorageService = queueStorageService;
            _fileShareService = fileShareService;
        }

        private bool IsCustomer()
        {
            return HttpContext.Session.GetString("UserRole") == "Customer";
        }

        public async Task<IActionResult> Index()
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Home");
            }

            var products = await _tableStorageService.GetProductsAsync();
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Order(string productId)
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Home");
            }

            var product = await _tableStorageService.GetProductAsync("Product", productId);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string productId)
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Home");
            }

            var product = await _tableStorageService.GetProductAsync("Product", productId);
            if (product == null)
            {
                return NotFound();
            }

            var customerName = HttpContext.Session.GetString("UserName");
            var customerEmail = HttpContext.Session.GetString("UserEmail");
            var customerId = HttpContext.Session.GetString("UserId");

            // 1. Persist the Order in Table Storage (Orders Table)
            var order = new OrderEntity
            {
                PartitionKey = customerId,
                RowKey = Guid.NewGuid().ToString(),
                ProductName = product.Name,
                ProductPrice = product.Price,
                ImageUrl = product.ImageUrl,
                OrderDate = DateTimeOffset.UtcNow,
                Status = "Pending"
            };
            await _tableStorageService.UpsertOrderAsync(order);

            // 2. Create a queue processing message detailing transaction
            string queueMessageText = $"Processing order for Customer '{customerName}' (ID: {customerId}). Product: '{product.Name}', Price: {product.Price:C}. Status: Pending processing.";
            await _queueStorageService.SendMessageAsync(queueMessageText);

            // 3. Log transaction to File Share
            await _fileShareService.AppendLogAsync("INFO", $"Order placed. Customer: {customerEmail}, Product: {product.Name}, Price: {product.Price}. Message added to Queue.");

            return RedirectToAction("OrderConfirmation", new { productName = product.Name, imageUrl = product.ImageUrl });
        }

        public IActionResult OrderConfirmation(string productName, string imageUrl)
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Home");
            }

            ViewBag.ProductName = productName;
            ViewBag.ImageUrl = imageUrl;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Home");
            }

            var customerId = HttpContext.Session.GetString("UserId");
            var orders = await _tableStorageService.GetOrdersForCustomerAsync(customerId);
            return View(orders);
        }
    }
}
