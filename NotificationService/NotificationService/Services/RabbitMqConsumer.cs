using DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Wrap;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
        private const string DEAD_LETTER_QUEUE_ROUTING_KEY = "error";

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
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(10));

            _policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
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

            var queues = new[]
                {
                    PRODUCT_CREATED_QUEUE,
                    PRODUCT_UPDATED_QUEUE,
                    PRODUCT_DELETED_QUEUE,
                };

            await _channel.ExchangeDeclareAsync(EXCHANGE_NAME, ExchangeType.Direct, durable: true);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageString = Encoding.UTF8.GetString(body);

                EventMessage eventMessage = null;

                try
                {
                    eventMessage = JsonSerializer.Deserialize<EventMessage>(messageString, _options);
                }
                catch (Exception ex)
                {
                    var attempts = ea.BasicProperties.Headers?.ContainsKey("attempts") == true
                     ? (int)ea.BasicProperties.Headers["attempts"] + 1
                     : 1;

                    if (attempts >= 3)
                    {
                        _logger.LogError("Message failed 3 times, sending to DLQ");
                        await SendToErrorQueue(body); 
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false); 
                        return;
                    }
                    else
                    {
                        ea.BasicProperties.Headers["attempts"] = attempts;
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, true); 
                    }
                }

                if (eventMessage != null)
                {
                    _logger.LogInformation($"Message received from queue: {ea.RoutingKey}");

                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IInventoryMessageHandler>();
                    await handler.HandleMessage(eventMessage, stoppingToken);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            foreach (var queue in queues)
            {
                await _channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer);
                _logger.LogInformation($"Consumer instantiated for queue: {queue}");
            }

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task SendToErrorQueue(ReadOnlyMemory<byte> body)
        {
            await _channel.BasicPublishAsync(
                exchange: EXCHANGE_NAME,
                routingKey: DEAD_LETTER_QUEUE_ROUTING_KEY,
                mandatory: true,
                basicProperties: new BasicProperties { Persistent = true },
                body: body);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
