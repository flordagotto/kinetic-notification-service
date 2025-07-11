using DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Wrap;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotificationService.Services
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        private readonly ConnectionFactory _factory;
        private readonly AsyncPolicyWrap _policyWrap;
        private readonly JsonSerializerOptions _options;
        private IConnection _connection;
        private IChannel _channel;

        private const string PRODUCT_CREATED_QUEUE = "ProductCreated";
        private const string PRODUCT_UPDATED_QUEUE = "ProductUpdated";
        private const string PRODUCT_DELETED_QUEUE = "ProductDeleted";

        private const string EXCHANGE_NAME = "inventory_exchange";

        public RabbitMqConsumer(ILogger<RabbitMqConsumer> logger, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;

            _factory = new ConnectionFactory()
            {
                //HostName = _configuration["RabbitMQ:HostName"],
                //Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                HostName = "localhost",
                Port = 5672,
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };

            var retryPolicy = Policy
                .Handle<BrokerUnreachableException>()
                .Or<Exception>()
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan) =>
                    {
                        _logger.LogWarning($"Retrying RabbitMQ connection in {timeSpan.TotalSeconds}s due to: {exception.Message}");
                    });

            // retry de conexion a rabbit por si se cae y la reconexion propia de rabbit no funciona (para robustez, pero para este servicio tan pequeño es un poco innecesario)

            _policyWrap = Policy.WrapAsync(retryPolicy);
        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection != null && _connection.IsOpen)
                return;

            await _policyWrap.ExecuteAsync(async () =>
            {
                _connection?.Dispose();
                _connection = await _factory.CreateConnectionAsync();

                _channel?.Dispose();
                _channel = await _connection.CreateChannelAsync();
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await EnsureConnectionAsync();

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageString = Encoding.UTF8.GetString(body);

                EventMessage? eventMessage = null;
                try
                {
                    eventMessage = JsonSerializer.Deserialize<EventMessage>(messageString);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false); 
                    return;
                }

                if (eventMessage != null)
                {
                    if (eventMessage != null)
                    {
                        _logger.LogInformation($"Received message: {messageString}");

                        using var scope = _serviceProvider.CreateScope();
                        var logService = scope.ServiceProvider.GetRequiredService<IInventoryMessageHandler>();

                        await logService.HandleMessage(eventMessage, stoppingToken);
                    }
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            _channel.BasicConsumeAsync(queue: QUEUE_NAME, autoAck: false, consumer: consumer);

            await Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
