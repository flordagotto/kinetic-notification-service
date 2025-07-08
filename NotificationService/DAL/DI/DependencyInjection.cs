using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DAL.DI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<NotificationsDbContext>(options =>
                options.UseSqlite("Data Source=notifications.db"));

            return services;
        }
    }
}
