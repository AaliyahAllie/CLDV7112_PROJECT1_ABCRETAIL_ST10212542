
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models;
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Controllers
{
    public class AdminController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly BlobStorageService _blobStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileShareService _fileShareService;

        public AdminController(
            TableStorageService tableStorageService,
            BlobStorageService blobStorageService,
            QueueStorageService queueStorageService,
            FileShareService fileShareService)
        {
            _tableStorageService = tableStorageService;
            _blobStorageService = blobStorageService;
            _queueStorageService = queueStorageService;
            _fileShareService = fileShareService;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        // ── Dashboard ────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            var customers = await _tableStorageService.GetCustomersAsync();
            var products = await _tableStorageService.GetProductsAsync();
            var queueMessages = await _queueStorageService.GetMessagesAsync();
            var orders = await _tableStorageService.GetAllOrdersAsync();

            ViewBag.CustomerCount = customers.Count;
            ViewBag.ProductCount = products.Count;
            ViewBag.QueueMessageCount = queueMessages.Count;
            ViewBag.OrderCount = orders.Count;

            return View();
        }

        // ── Customers ────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var customers = await _tableStorageService.GetCustomersAsync();
            return View(customers);
        }

        // ── Inventory (Products) ─────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Products()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var products = await _tableStorageService.GetProductsAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            ModelState.Remove(nameof(product.PartitionKey));
            ModelState.Remove(nameof(product.RowKey));
            ModelState.Remove(nameof(product.ImageUrl));

            if (!ModelState.IsValid) return View(product);

            product.PartitionKey = "Product";
            product.RowKey = Guid.NewGuid().ToString();

            // Upload image to Blob Storage
            if (imageFile != null && imageFile.Length > 0)
            {
                using var stream = imageFile.OpenReadStream();
                product.ImageUrl = await _blobStorageService.UploadBlobAsync(product.RowKey, stream);
            }

            await _tableStorageService.UpsertProductAsync(product);

            // Queue inventory message (rubric: inventory process via Queue)
            var queueMsg = $"[INVENTORY-ADD] | ProductId: {product.RowKey} | Name: {product.Name} | " +
                           $"Category: {product.Category} | Price: R{product.Price:F2} | Stock: {product.StockQuantity} | " +
                           $"AddedBy: admin | Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            await _queueStorageService.SendMessageAsync(queueMsg);

            // Log to product log file
            await _fileShareService.AppendProductLogAsync("INFO",
                $"Product ADDED. Id: {product.RowKey} | Name: {product.Name} | Category: {product.Category} | Price: R{product.Price:F2} | Stock: {product.StockQuantity} | AddedBy: admin");

            TempData["Success"] = $"Product '{product.Name}' added successfully!";
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(string rowKey)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            var product = await _tableStorageService.GetProductAsync("Product", rowKey);
            if (product != null)
            {
                await _blobStorageService.DeleteBlobAsync(rowKey);
                await _tableStorageService.DeleteProductAsync("Product", rowKey);

                // Queue inventory removal message
                var queueMsg = $"[INVENTORY-REMOVE] | ProductId: {rowKey} | Name: {product.Name} | " +
                               $"RemovedBy: admin | Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                await _queueStorageService.SendMessageAsync(queueMsg);

                await _fileShareService.AppendProductLogAsync("WARNING",
                    $"Product DELETED. Id: {rowKey} | Name: {product.Name} | RemovedBy: admin");

                TempData["Success"] = $"Product '{product.Name}' deleted successfully.";
            }

            return RedirectToAction("Products");
        }

        // ── Orders Management ────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var orders = await _tableStorageService.GetAllOrdersAsync();
            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string customerId, string orderId, string newStatus)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            await _tableStorageService.UpdateOrderStatusAsync(customerId, orderId, newStatus);

            // Queue status update message (inventory/fulfilment process)
            var queueMsg = $"[ORDER-STATUS-UPDATE] | OrderId: {orderId} | CustomerId: {customerId} | " +
                           $"NewStatus: {newStatus} | UpdatedBy: admin | Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            await _queueStorageService.SendMessageAsync(queueMsg);

            await _fileShareService.AppendOrderLogAsync("INFO",
                $"Order status updated. OrderId: {orderId} | CustomerId: {customerId} | NewStatus: {newStatus} | UpdatedBy: admin");

            TempData["Success"] = $"Order status updated to '{newStatus}'.";
            return RedirectToAction("Orders");
        }

        // ── Queue ────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Queue()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var messages = await _queueStorageService.GetMessagesAsync(20);
            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessQueueMessage()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var msg = await _queueStorageService.DequeueMessageAsync();
            if (msg != null)
            {
                await _fileShareService.AppendOrderLogAsync("INFO", $"Queue message dequeued and processed: {msg.MessageText}");
                TempData["Success"] = "Message dequeued successfully.";
            }
            else
            {
                TempData["Info"] = "Queue is currently empty.";
            }
            return RedirectToAction("Queue");
        }

        [HttpPost]
        public async Task<IActionResult> ClearQueue()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            await _queueStorageService.ClearQueueAsync();
            await _fileShareService.AppendSystemLogAsync("WARNING", "Queue cleared by admin.");
            TempData["Success"] = "Queue cleared successfully.";
            return RedirectToAction("Queue");
        }

        // ── Logs ─────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Logs(string file = "system-logs.txt")
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            var logs = await _fileShareService.ReadLogFileAsync(file);
            ViewBag.LogFileNames = FileShareService.LogFileNames;
            ViewBag.ActiveFile = file;

            return View(logs);
        }

        [HttpPost]
        public async Task<IActionResult> ClearLogs(string file = "system-logs.txt")
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            await _fileShareService.ClearLogFileAsync(file);
            TempData["Success"] = $"Log file '{file}' cleared.";
            return RedirectToAction("Logs", new { file });
        }
    }
}
