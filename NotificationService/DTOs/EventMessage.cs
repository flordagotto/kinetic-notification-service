namespace DTOs
{
    public class EventMessage
    {
        public Guid ProductId { get; set; }
        public ProductEventType EventType { get; set; }
        public DateTime EventDate { get; set; }
    }

    public enum ProductEventType
    {
        Created,
        Updated,
        Deleted
    }
}
