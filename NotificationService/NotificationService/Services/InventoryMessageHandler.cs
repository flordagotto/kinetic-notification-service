using DAL.Entities;
using DAL.Repositories;
using DTOs;
using Microsoft.Extensions.Logging;

namespace NotificationService.Services
{
    public interface IInventoryMessageHandler
    {
        Task HandleMessage(EventMessage message);
    }

    public class InventoryMessageHandler(INotificationRepository notificationRepository, ILogger<InventoryMessageHandler> logger) : IInventoryMessageHandler
    {
        private readonly INotificationRepository _notificationRepository = notificationRepository;
        private readonly ILogger<InventoryMessageHandler> _logger = logger;

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

        private static string GetLogDescription(EventMessage message)
        {
            var description = "The inventory was modified - ";

            return $"{description}Product with id {message.ProductId} has been {message.EventType}. Check the Inventory Database to see the new values.";
        }

        private static InventoryEventType MapEventType(ProductEventType dtoType)
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
}