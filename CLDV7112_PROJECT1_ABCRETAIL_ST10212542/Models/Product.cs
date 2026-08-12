using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel.DataAnnotations;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";

        [Required]
        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Range(0.01, 1000000.00, ErrorMessage = "Price must be greater than zero.")]
        public double Price { get; set; }

        [Required]
        public string Category { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Range(0, 10000, ErrorMessage = "Stock quantity must be between 0 and 10,000.")]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; }
    }
}
