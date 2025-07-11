using DAL.Entities;

namespace DAL.Repositories
{
    public interface INotificationRepository
    {
        Task Add(InventoryLog product);
        Task<IEnumerable<InventoryLog>> Get();
    }

    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationsDbContext _context;

        public NotificationRepository(NotificationsDbContext context)
        {
            _context = context;
        }

        public Task Add(InventoryLog product)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<InventoryLog>> Get()
        {
            throw new NotImplementedException();
        }
    }
}
