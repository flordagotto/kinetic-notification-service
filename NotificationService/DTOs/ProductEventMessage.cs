namespace DTOs
{
    public class ProductEventMessage : EventMessage
    {
        public string ProductName { get; set; } = default!;
        public string Description { get; set; } = default!;
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; } = default!;
    }
}
