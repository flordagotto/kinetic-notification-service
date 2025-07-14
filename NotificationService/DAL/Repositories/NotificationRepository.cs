using DAL.Entities;

namespace DAL.Repositories
{
    public interface INotificationRepository
    {
        Task Add(InventoryLog log);
    }

    public class NotificationRepository(NotificationsDbContext context) : INotificationRepository
    {
        private readonly NotificationsDbContext _context = context;

        public async Task Add(InventoryLog log)
        {
            _context.InventoryLogs.Add(log);

            await _context.SaveChangesAsync();
        }
    }
}
