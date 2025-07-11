namespace DTOs
{
    public class ProductDeletedEventMessage : EventMessage
    {
        public ProductDeletedEventMessage()
        {
            EventType = ProductEventType.Deleted;
        }
    }
}
