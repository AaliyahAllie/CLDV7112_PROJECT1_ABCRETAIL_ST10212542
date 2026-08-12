using Azure;
using Azure.Data.Tables;
using System;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models
{
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; }   // CustomerId
        public string RowKey { get; set; }          // OrderId (GUID)
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string ProductName { get; set; }
        public double ProductPrice { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set; } = 1;
        public double TotalAmount { get; set; }
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;

        // Fulfillment status: Processing | Shipped | Delivered | Cancelled
        public string Status { get; set; } = "Processing";

        // Payment status: Pending | Paid | Failed
        public string PaymentStatus { get; set; } = "Pending";
        public string PaymentIntentId { get; set; }

        // Denormalised customer info for admin view
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
    }
}
