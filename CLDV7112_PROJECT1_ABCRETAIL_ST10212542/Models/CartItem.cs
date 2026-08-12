namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models
{
    public class CartItem
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public double Price { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; } = 1;
        public double LineTotal => Price * Quantity;
    }
}
