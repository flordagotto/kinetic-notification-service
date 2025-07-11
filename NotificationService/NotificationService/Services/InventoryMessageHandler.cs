using DAL;
using DAL.Entities;
using DTOs;

public class InventoryMessageHandler
{
    private readonly NotificationsDbContext _context;

    public InventoryMessageHandler(NotificationsDbContext context)
    {
        _context = context;
    }

    public async Task HandleMessage(string eventType, EventMessage message)
    {
        var notification = new InventoryLog
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
