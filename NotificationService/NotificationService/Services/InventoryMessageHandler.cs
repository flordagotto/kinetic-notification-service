using DAL.Entities;
using DAL.Repositories;
using DTOs;

public class InventoryMessageHandler
{
    private readonly INotificationRepository _notificationRepository;

    public InventoryMessageHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task HandleMessage(string eventType, EventMessage message)
    {
        //var notification = new InventoryLog
        //{
        //    EventType = eventType,
        //    ProductId = product.Id.ToString(),
        //    ProductName = product.Name,
        //    ReceivedAt = DateTime.UtcNow
        //};

        //_notificationRepository.Notifications.Add(notification);
        //await _context.SaveChangesAsync();
    }

    private InventoryEventType MapEventType(ProductEventType dtoType)
    {
        return dtoType switch
        {
            ProductEventType.Created => InventoryEventType.Created,
            ProductEventType.Updated => InventoryEventType.Updated,
            ProductEventType.Deleted => InventoryEventType.Deleted,
            _ => throw new ArgumentOutOfRangeException(nameof(dtoType))
        };
    }

}
