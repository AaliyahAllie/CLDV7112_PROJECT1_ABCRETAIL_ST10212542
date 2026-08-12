using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models;
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Controllers
{
    public class CartController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private const string CartSessionKey = "Cart";

        public CartController(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        private bool IsCustomer() => HttpContext.Session.GetString("UserRole") == "Customer";

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            return string.IsNullOrEmpty(json)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
            => HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));

        [HttpGet]
        public IActionResult Index()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");
            return View(GetCart());
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int quantity = 1)
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");

            var product = await _tableStorageService.GetProductAsync("Product", productId);
            if (product == null) return NotFound();

            if (product.StockQuantity <= 0)
            {
                TempData["Error"] = $"Sorry, '{product.Name}' is currently out of stock.";
                return RedirectToAction("Index", "Customer");
            }

            var cart = GetCart();
            var existing = cart.FirstOrDefault(x => x.ProductId == productId);

            if (existing != null)
            {
                existing.Quantity = Math.Min(existing.Quantity + quantity, product.StockQuantity);
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Category = product.Category,
                    Quantity = Math.Min(quantity, product.StockQuantity)
                });
            }

            SaveCart(cart);
            TempData["Success"] = $"'{product.Name}' added to your cart!";
            return RedirectToAction("Index", "Customer");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(string productId, int quantity)
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");

            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                    cart.Remove(item);
                else
                    item.Quantity = quantity;
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveItem(string productId)
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");

            var cart = GetCart();
            cart.RemoveAll(x => x.ProductId == productId);
            SaveCart(cart);
            TempData["Success"] = "Item removed from cart.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Clear()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToAction("Index");
        }
    }
}
