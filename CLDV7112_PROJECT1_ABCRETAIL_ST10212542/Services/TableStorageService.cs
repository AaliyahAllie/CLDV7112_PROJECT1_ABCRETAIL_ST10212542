
using Azure.Data.Tables;
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models;
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

            // Ensure tables exist
            _customerTableClient.CreateIfNotExists();
            _productTableClient.CreateIfNotExists();
            _orderTableClient.CreateIfNotExists();
        }

        // Customer Operations
        public async Task<List<CustomerProfile>> GetCustomersAsync()
        {
            var customers = new List<CustomerProfile>();
            await foreach (var customer in _customerTableClient.QueryAsync<CustomerProfile>())
            {
                customers.Add(customer);
            }
            return customers;
        }

        public async Task<CustomerProfile> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _customerTableClient.GetEntityAsync<CustomerProfile>(partitionKey, rowKey);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task UpsertCustomerAsync(CustomerProfile customer)
        {
            await _customerTableClient.UpsertEntityAsync(customer);
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _customerTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // Product Operations
        public async Task<List<Product>> GetProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var product in _productTableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }
            return products;
        }

        public async Task<Product> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _productTableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task UpsertProductAsync(Product product)
        {
            await _productTableClient.UpsertEntityAsync(product);
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _productTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // Order Operations
        public async Task<List<OrderEntity>> GetOrdersForCustomerAsync(string customerId)
        {
            var orders = new List<OrderEntity>();
            await foreach (var order in _orderTableClient.QueryAsync<OrderEntity>(o => o.PartitionKey == customerId))
            {
                orders.Add(order);
            }
            // Sort by order date descending (latest first)
            orders.Sort((x, y) => y.OrderDate.CompareTo(x.OrderDate));
            return orders;
        }

        public async Task UpsertOrderAsync(OrderEntity order)
        {
            await _orderTableClient.UpsertEntityAsync(order);
        }
    }
}
