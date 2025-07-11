using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class NotificationsDbContext : DbContext
    {
        public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
            : base(options)
        {
        }

        public DbSet<InventoryLog> InventoryLogs { get; set; }
    }
}
