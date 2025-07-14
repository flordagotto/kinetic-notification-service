using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
    {
        public DbSet<InventoryLog> InventoryLogs { get; set; }
    }
}
