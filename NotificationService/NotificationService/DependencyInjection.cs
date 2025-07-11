using Microsoft.Extensions.DependencyInjection;

namespace NotificationService
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IInventoryMessageHandler, InventoryMessageHandler>();

            return services;
        }
    }
}
