
using Azure.Data.Tables;
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services
{
    public class TableStorageService
    {
        private readonly TableClient _customerTableClient;
        private readonly TableClient _productTableClient;
        private readonly TableClient _orderTableClient;

        public TableStorageService(string connectionString)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _customerTableClient = serviceClient.GetTableClient("Customers");
            _productTableClient = serviceClient.GetTableClient("Products");
            _orderTableClient = serviceClient.GetTableClient("Orders");

            _customerTableClient.CreateIfNotExists();
            _productTableClient.CreateIfNotExists();
            _orderTableClient.CreateIfNotExists();
        }

        // ── Customer Operations ──────────────────────────────────────────────────

        public async Task<List<CustomerProfile>> GetCustomersAsync()
        {
            var customers = new List<CustomerProfile>();
            await foreach (var c in _customerTableClient.QueryAsync<CustomerProfile>())
                customers.Add(c);
            return customers;
        }

        public async Task<CustomerProfile> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _customerTableClient.GetEntityAsync<CustomerProfile>(partitionKey, rowKey);
                return response.Value;
            }
            catch { return null; }
        }

        public async Task<CustomerProfile> GetCustomerByEmailAsync(string email)
        {
            await foreach (var c in _customerTableClient.QueryAsync<CustomerProfile>(x => x.Email == email))
                return c;
            return null;
        }

        public async Task UpsertCustomerAsync(CustomerProfile customer)
            => await _customerTableClient.UpsertEntityAsync(customer);

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
            => await _customerTableClient.DeleteEntityAsync(partitionKey, rowKey);

        // ── Product Operations ───────────────────────────────────────────────────

        public async Task<List<Product>> GetProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var p in _productTableClient.QueryAsync<Product>())
                products.Add(p);
            return products;
        }

        public async Task<Product> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _productTableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch { return null; }
        }

        public async Task UpsertProductAsync(Product product)
            => await _productTableClient.UpsertEntityAsync(product);

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
            => await _productTableClient.DeleteEntityAsync(partitionKey, rowKey);

        public async Task UpdateProductStockAsync(string productId, int quantityToReduce)
        {
            try
            {
                var response = await _productTableClient.GetEntityAsync<Product>("Product", productId);
                var product = response.Value;
                product.StockQuantity = Math.Max(0, product.StockQuantity - quantityToReduce);
                await _productTableClient.UpsertEntityAsync(product);
            }
            catch { /* Non-fatal – stock update failure should not block order */ }
        }

        // ── Order Operations (Customer) ──────────────────────────────────────────

        public async Task<List<OrderEntity>> GetOrdersForCustomerAsync(string customerId)
        {
            var orders = new List<OrderEntity>();
            await foreach (var o in _orderTableClient.QueryAsync<OrderEntity>(o => o.PartitionKey == customerId))
                orders.Add(o);
            orders.Sort((x, y) => y.OrderDate.CompareTo(x.OrderDate));
            return orders;
        }

        // ── Order Operations (Admin) ─────────────────────────────────────────────

        public async Task<List<OrderEntity>> GetAllOrdersAsync()
        {
            var orders = new List<OrderEntity>();
            await foreach (var o in _orderTableClient.QueryAsync<OrderEntity>())
                orders.Add(o);
            orders.Sort((x, y) => y.OrderDate.CompareTo(x.OrderDate));
            return orders;
        }

        public async Task UpsertOrderAsync(OrderEntity order)
            => await _orderTableClient.UpsertEntityAsync(order);

        public async Task UpdateOrderStatusAsync(string customerId, string orderId, string newStatus)
        {
            try
            {
                var response = await _orderTableClient.GetEntityAsync<OrderEntity>(customerId, orderId);
                var order = response.Value;
                order.Status = newStatus;
                await _orderTableClient.UpsertEntityAsync(order);
            }
            catch { /* Non-fatal */ }
        }
    }
}
