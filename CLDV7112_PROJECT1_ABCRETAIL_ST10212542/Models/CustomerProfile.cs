using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel.DataAnnotations;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models
{
    public class CustomerProfile : ITableEntity
    {
        // ITableEntity required properties
        public string PartitionKey { get; set; } = "Customer";
        
        [Required]
        public string RowKey { get; set; } // CustomerId
        
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Custom profile properties
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
