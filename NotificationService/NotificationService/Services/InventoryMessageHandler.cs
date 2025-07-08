using DAL;

public class InventoryMessageHandler
{
    private readonly NotificationsDbContext _context;

    public InventoryMessageHandler(NotificationsDbContext context)
    {
        _context = context;
    }

    public async Task HandleMessage(string eventType, ProductDto product)
    {
        var notification = new InventoryNotification
        {
            EventType = eventType,
            ProductId = product.Id.ToString(),
            ProductName = product.Name,
            ReceivedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }
}
