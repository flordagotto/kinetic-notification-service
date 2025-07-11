using DAL.Entities;

namespace DAL.Repositories
{
    public interface INotificationRepository
    {
        Task Add(InventoryLog log, CancellationToken cancellationToken);
    }

    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationsDbContext _context;

        public NotificationRepository(NotificationsDbContext context)
        {
            _context = context;
        }

        public async Task Add(InventoryLog log, CancellationToken cancellationToken)
        {
            _context.InventoryLogs.Add(log);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
