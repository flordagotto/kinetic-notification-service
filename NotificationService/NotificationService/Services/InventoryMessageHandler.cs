using DAL.Entities;
using DAL.Repositories;
using DTOs;
using Microsoft.Extensions.Logging;

public interface IInventoryMessageHandler
{
    Task HandleMessage(EventMessage message);
}

public class InventoryMessageHandler : IInventoryMessageHandler
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<InventoryMessageHandler> _logger;

    public InventoryMessageHandler(INotificationRepository notificationRepository, ILogger<InventoryMessageHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task HandleMessage(EventMessage message)
    {
        try
        {
            var log = new InventoryLog
            {
                Id = Guid.NewGuid(),
                EventType = MapEventType(message.EventType),
                ProductId = message.ProductId,
                Description = GetLogDescription(message),
                EventDate = message.EventDate
            };

            await _notificationRepository.Add(log);
           
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "Error creating a product.");
            throw;
        }
    }

    private string GetLogDescription(EventMessage message)
    {
        var description = "The inventory was modified - ";

        if(message.EventType == ProductEventType.Deleted)
        {
            return $"{description}Product with id {message.ProductId} has been deleted.";
        }

        return $"{description}Product with id {message.ProductId} has been {message.EventType}. Check the Inventory Database to see the new values.";
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
