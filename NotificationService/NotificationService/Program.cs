using DAL;
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
        services.AddInfrastructureServices();
        services.AddApplicationServices();

        // Configurar logger
        services.AddLogging(configure => configure.AddConsole());

        // Configurar servicio consumidor
        services.AddHostedService<RabbitMqConsumer>();
    })
    .Build();

        await host.RunAsync();
    }
}