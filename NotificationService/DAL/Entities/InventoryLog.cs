namespace DAL.Entities
{
    public class InventoryLog
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
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
