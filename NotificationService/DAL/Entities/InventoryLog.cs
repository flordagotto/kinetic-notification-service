namespace DAL.Entities
{
    public class InventoryLog
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = null!;
        public string Description { get; set; } = null!;
        public InventoryEventType EventType { get; set; }
        public DateTime EventDate { get; set; }
    }

    public enum InventoryEventType
    {
        Created,
        Updated,
        Deleted
    }
}
