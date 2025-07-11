namespace DTOs
{
    public class EventMessage
    {
        public Guid Id { get; set; }
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
