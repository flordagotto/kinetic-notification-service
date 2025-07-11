using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DAL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<NotificationsDbContext>(options =>
                 options.UseSqlite(connectionString));

            services.AddScoped<INotificationRepository, NotificationRepository>();

            return services;
        }
    }
}
