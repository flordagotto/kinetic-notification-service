using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DAL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddDbContext<NotificationsDbContext>(options =>
                options.UseSqlite("Data Source=notifications.db"));

            return services;
        }
    }
}
