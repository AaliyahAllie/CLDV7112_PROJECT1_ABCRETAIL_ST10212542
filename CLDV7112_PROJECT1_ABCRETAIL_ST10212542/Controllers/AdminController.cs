
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models;
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ABCRetailWeb.Controllers
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

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Home");
            }

            var customers = await _tableStorageService.GetCustomersAsync();
            var products = await _tableStorageService.GetProductsAsync();
            var queueMessages = await _queueStorageService.GetMessagesAsync();
            var logs = await _fileShareService.ReadLogsAsync();

            ViewBag.CustomerCount = customers.Count;
            ViewBag.ProductCount = products.Count;
            ViewBag.QueueCount = queueMessages.Count;
            ViewBag.LogCount = logs.Count;

            return View();
        }

        // --- CUSTOMER MANAGEMENT ---
        public async Task<IActionResult> Customers()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Home");
            }

            var customers = await _tableStorageService.GetCustomersAsync();
            return View(customers);
        }

        // --- PRODUCT MANAGEMENT ---
        public async Task<IActionResult> Products()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Home");
            }

            var products = await _tableStorageService.GetProductsAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile imageFile)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Home");
            }

            ModelState.Remove(nameof(product.Timestamp));
            ModelState.Remove(nameof(product.ETag));
            ModelState.Remove(nameof(product.ImageUrl));
            ModelState.Remove(nameof(product.PartitionKey));
            ModelState.Remove(nameof(product.RowKey));

            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("ImageUrl", "Product image is required.");
            }

            if (ModelState.IsValid && imageFile != null)
            {
                try
                {
                    // 1. Upload file to Blob Storage
                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string imageUrl = "";
                    using (var stream = imageFile.OpenReadStream())
                    {
                        imageUrl = await _blobStorageService.UploadBlobAsync(uniqueFileName, stream);
                    }

                    // Set Product Properties
                    product.PartitionKey = "Product";
                    product.RowKey = Guid.NewGuid().ToString();
                    product.ImageUrl = imageUrl;

                    // 2. Save product info to Table Storage
                    await _tableStorageService.UpsertProductAsync(product);

                    // 3. Queue messaging: "Uploading image imageName" as per requirement
                    string queueMsg = $"Uploading image: '{uniqueFileName}' (original: '{imageFile.FileName}') for product: '{product.Name}'. Price: {product.Price:C}. Status: Complete.";
                    await _queueStorageService.SendMessageAsync(queueMsg);

                    // 4. File Logging
                    await _fileShareService.AppendLogAsync("INFO", $"Product added. Name: {product.Name}, Price: {product.Price}, Blob Image: {uniqueFileName}, Queue event dispatched.");

                    TempData["SuccessMessage"] = $"Product '{product.Name}' added successfully.";
                    return RedirectToAction("Products");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred during product addition: {ex.Message}");
                    await _fileShareService.AppendLogAsync("ERROR", $"Failed to add product {product.Name}: {ex.Message}");
                }
            }

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(string rowKey)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Home");
            }

            var product = await _tableStorageService.GetProductAsync("Product", rowKey);
            if (product != null)
            {
                // Delete image from Blob Storage
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    try
                    {
                        var uri = new Uri(product.ImageUrl);
                        string fileName = Path.GetFileName(uri.LocalPath);
                        await _blobStorageService.DeleteBlobAsync(fileName);
                    }
                    catch (Exception ex)
                    {
                        await _fileShareService.AppendLogAsync("WARNING", $"Could not delete blob for product {product.Name}: {ex.Message}");
                    }
                }

                // Delete product entity
                await _tableStorageService.DeleteProductAsync("Product", rowKey);
                await _fileShareService.AppendLogAsync("INFO", $"Product '{product.Name}' (ID: {rowKey}) deleted by administrator.");
            }

            return RedirectToAction("Products");
        }

        // --- QUEUE MANAGEMENT ---
        public async Task<IActionResult> Queue()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Home");
            }

            var messages = await _queueStorageService.GetMessagesAsync();
            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessQueueMessage()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Home");
            }

            var processedMessage = await _queueStorageService.DequeueMessageAsync();
            if (processedMessage != null)
            {
                await _fileShareService.AppendLogAsync("INFO", $"Queue message processed by Admin: ID {processedMessage.MessageId} - Content: '{processedMessage.MessageText}'");
                TempData["ProcessSuccess"] = $"Successfully processed message: \"{processedMessage.MessageText}\"";
            }
            else
            {
                TempData["ProcessSuccess"] = "The queue is empty. No messages to process.";
            }

            return RedirectToAction("Queue");
        }

        [HttpPost]
        public async Task<IActionResult> ClearQueue()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Home");
            }

            await _queueStorageService.ClearQueueAsync();
            await _fileShareService.AppendLogAsync("INFO", "Queue cleared by administrator.");
            return RedirectToAction("Queue");
        }

        // --- LOGS MANAGEMENT ---
        public async Task<IActionResult> Logs()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Home");
            }

            var logs = await _fileShareService.ReadLogsAsync();
            return View(logs);
        }

        [HttpPost]
        public async Task<IActionResult> ClearLogs()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Home");
            }

            await _fileShareService.ClearLogsAsync();
            return RedirectToAction("Logs");
        }
    }
}
