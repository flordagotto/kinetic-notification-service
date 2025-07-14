using Microsoft.Extensions.DependencyInjection;

namespace NotificationService
{
    public static class DependencyInjection
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IInventoryMessageHandler, InventoryMessageHandler>();
        }
    }
}
