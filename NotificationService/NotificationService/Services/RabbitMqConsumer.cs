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
        private readonly int MaximumRetries;

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

            MaximumRetries = int.Parse(_configuration["RabbitMQ:MaximumRetries"]);

            _factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"],
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
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
                var props = ea.BasicProperties;
                var routingKey = ea.RoutingKey;
                int retries = 0;

                if (props.Headers != null && props.Headers.ContainsKey("x-retries"))
                {
                    retries = Convert.ToInt32(props.Headers["x-retries"]);
                }
                var messageString = Encoding.UTF8.GetString(body);

                try
                {
                    if (!string.IsNullOrEmpty(messageString))
                        await ProcessMessage(messageString);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    if (retries < MaximumRetries)
                    {
                        _logger.LogInformation($"Retrying ({retries + 1}/{MaximumRetries}).");

                        await _channel.BasicPublishAsync(
                            EXCHANGE_NAME,
                            routingKey,
                            mandatory: true,
                            basicProperties: new BasicProperties
                            {
                                Headers = new Dictionary<string, object?>
                                {
                                    {"x-retries", retries + 1 }
                                }
                            },
                            body: body);
                    }
                    else
                    {
                        _logger.LogInformation($"Maximum attemps reached. Sending to DLQ.");

                        var failedMessage = new FailedEventMessage
                        {
                            OriginalMessage = messageString,
                            Error = ex.Message.ToString()
                        };

                        var failedBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(failedMessage));

                        await _channel.BasicPublishAsync(
                            EXCHANGE_NAME,
                            $"{routingKey}.dlq",
                            mandatory: true,
                            basicProperties: new BasicProperties
                            {
                                Headers = new Dictionary<string, object?>
                                {
                                    {"x-retries", retries }
                                }
                            },
                            body: failedBody);
                    }
                }
            };

            foreach (var queue in queues)
            {
                await _channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer);
                _logger.LogInformation($"Consumer instantiated for queue: {queue}");
            }

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task ProcessMessage(string messageString)
        {
            var eventMessage = JsonSerializer.Deserialize<EventMessage>(messageString, _options);

            if (eventMessage != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IInventoryMessageHandler>();
                await handler.HandleMessage(eventMessage);
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
