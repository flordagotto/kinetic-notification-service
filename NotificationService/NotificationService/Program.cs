using DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService;
using NotificationService.Services;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false);
        })
        .ConfigureServices((context, services) =>
        {
            services.AddInfrastructureServices(context.Configuration.GetConnectionString("DefaultConnection"));
            services.AddApplicationServices();

            services.AddLogging(configure => configure.AddConsole());

            services.AddHostedService<RabbitMqConsumer>();
        })
        .Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            db.Database.Migrate();
        }

        await host.RunAsync();
     }
}