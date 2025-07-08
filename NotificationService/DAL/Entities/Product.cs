namespace DAL.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; } = null!; // TODO: create an enum
    }
}
