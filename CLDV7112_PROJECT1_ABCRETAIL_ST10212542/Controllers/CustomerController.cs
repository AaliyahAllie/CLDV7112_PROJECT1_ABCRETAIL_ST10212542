
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models;
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileShareService _fileShareService;
        private readonly StripePaymentService _stripeService;
        private readonly IConfiguration _configuration;
        private const string CartSessionKey = "Cart";

        public CustomerController(
            TableStorageService tableStorageService,
            QueueStorageService queueStorageService,
            FileShareService fileShareService,
            StripePaymentService stripeService,
            IConfiguration configuration)
        {
            _tableStorageService = tableStorageService;
            _queueStorageService = queueStorageService;
            _fileShareService = fileShareService;
            _stripeService = stripeService;
            _configuration = configuration;
        }

        private bool IsCustomer() => HttpContext.Session.GetString("UserRole") == "Customer";

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            return string.IsNullOrEmpty(json)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }

        // ── Shop ────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");
            var products = await _tableStorageService.GetProductsAsync();
            return View(products);
        }

        // ── Checkout ─────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");

            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty. Add some products before checking out.";
                return RedirectToAction("Index", "Cart");
            }

            var totalAmount = cart.Sum(x => x.LineTotal);
            var amountInCents = (long)(totalAmount * 100);

            // Create Stripe PaymentIntent
            var paymentIntent = await _stripeService.CreatePaymentIntentAsync(amountInCents, "zar");

            ViewBag.ClientSecret = paymentIntent.ClientSecret;
            ViewBag.PublishableKey = _configuration["Stripe:PublishableKey"];
            ViewBag.TotalAmount = totalAmount;

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(string paymentIntentId)
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");

            // Verify payment with Stripe
            var paymentIntent = await _stripeService.GetPaymentIntentAsync(paymentIntentId);
            if (paymentIntent.Status != "succeeded")
            {
                TempData["Error"] = "Payment was not successful. Please try again.";
                await _fileShareService.AppendErrorLogAsync("ERROR", $"Payment failed. PaymentIntentId: {paymentIntentId}, Status: {paymentIntent.Status}");
                return RedirectToAction("Checkout");
            }

            var cart = GetCart();
            var customerId = HttpContext.Session.GetString("UserId");
            var customerName = HttpContext.Session.GetString("UserName");
            var customerEmail = HttpContext.Session.GetString("UserEmail");
            var totalAmount = cart.Sum(x => x.LineTotal);

            // Create an OrderEntity per cart item
            foreach (var item in cart)
            {
                var orderId = Guid.NewGuid().ToString();

                var order = new OrderEntity
                {
                    PartitionKey = customerId,
                    RowKey = orderId,
                    ProductName = item.ProductName,
                    ProductPrice = item.Price,
                    ImageUrl = item.ImageUrl,
                    Quantity = item.Quantity,
                    TotalAmount = item.LineTotal,
                    OrderDate = DateTimeOffset.UtcNow,
                    Status = "Processing",
                    PaymentStatus = "Paid",
                    PaymentIntentId = paymentIntentId,
                    CustomerName = customerName,
                    CustomerEmail = customerEmail
                };

                await _tableStorageService.UpsertOrderAsync(order);
                await _tableStorageService.UpdateProductStockAsync(item.ProductId, item.Quantity);

                // Queue the order transaction (rubric: transactions via Queue)
                var queueMsg = $"[ORDER-TRANSACTION] | OrderId: {orderId} | Customer: {customerName} ({customerEmail}) | " +
                               $"Product: {item.ProductName} | Qty: {item.Quantity} | Amount: R{item.LineTotal:F2} | " +
                               $"PaymentStatus: Paid | PaymentIntent: {paymentIntentId} | Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                await _queueStorageService.SendMessageAsync(queueMsg);

                // Log to order log file
                await _fileShareService.AppendOrderLogAsync("INFO",
                    $"Order PAID. OrderId: {orderId} | Customer: {customerEmail} | Product: {item.ProductName} | Qty: {item.Quantity} | Total: R{item.LineTotal:F2} | PaymentIntent: {paymentIntentId}");
            }

            // Clear the cart
            HttpContext.Session.Remove(CartSessionKey);

            TempData["PaymentSuccess"] = $"Payment of R{totalAmount:F2} confirmed! {cart.Count} item(s) ordered.";
            TempData["PaymentIntentId"] = paymentIntentId;
            TempData["ItemCount"] = cart.Count.ToString();

            return RedirectToAction("PaymentSuccess");
        }

        [HttpGet]
        public IActionResult PaymentSuccess()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");
            return View();
        }

        [HttpGet]
        public IActionResult PaymentCancelled()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");
            return View();
        }

        // ── My Orders ────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");
            var customerId = HttpContext.Session.GetString("UserId");
            var orders = await _tableStorageService.GetOrdersForCustomerAsync(customerId);
            return View(orders);
        }
    }
}
