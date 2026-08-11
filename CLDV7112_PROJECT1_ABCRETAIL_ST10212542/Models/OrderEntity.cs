using Azure;
using Azure.Data.Tables;
using System;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models
{
    public class OrderEntity : ITableEntity
    {
        // ITableEntity required properties
        public string PartitionKey { get; set; } // Will store CustomerId (Partition Key)
        public string RowKey { get; set; }       // Will store OrderId (Row Key)
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Order properties
        public string ProductName { get; set; }
        public double ProductPrice { get; set; }
        public string ImageUrl { get; set; }
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
        public string Status { get; set; } = "Pending";
    }
}
