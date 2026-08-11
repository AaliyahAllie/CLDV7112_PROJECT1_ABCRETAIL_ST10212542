using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel.DataAnnotations;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models
{
    public class Product : ITableEntity
    {
        // ITableEntity required properties
        public string PartitionKey { get; set; } = "Product";

        [Required]
        public string RowKey { get; set; } // ProductId

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Product specific properties
        [Required]
        public string Name { get; set; }

        [Required]
        [Range(0.01, 1000000.00, ErrorMessage = "Price must be greater than zero.")]
        public double Price { get; set; }

        [Required]
        public string Category { get; set; }

        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } // Uploaded to blob storage
    }
}
