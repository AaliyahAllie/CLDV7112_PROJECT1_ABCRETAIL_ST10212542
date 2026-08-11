using System;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models
{
    public class QueueMessageModel
    {
        public string MessageId { get; set; }
        public string PopReceipt { get; set; }
        public string MessageText { get; set; }
        public DateTimeOffset? InsertionTime { get; set; }
        public DateTimeOffset? ExpirationTime { get; set; }
    }
}
