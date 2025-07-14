namespace DAL.Entities
{
    public class InventoryLog
    {
        public Guid Id { get; init; }
        public Guid ProductId { get; init; }
        public string Description { get; init; } = null!;
        public InventoryEventType EventType { get; init; }
        public DateTimeOffset EventDate { get; init; }
    }

    public enum InventoryEventType
    {
        Created,
        Updated,
        Deleted
    }
}
