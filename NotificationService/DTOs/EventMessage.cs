namespace DTOs
{
    public class EventMessage
    {
        public Guid ProductId { get; init; }
        public ProductEventType EventType { get; init; }
        public DateTime EventDate { get; init; }
    }

    public enum ProductEventType
    {
        Created,
        Updated,
        Deleted
    }
}
