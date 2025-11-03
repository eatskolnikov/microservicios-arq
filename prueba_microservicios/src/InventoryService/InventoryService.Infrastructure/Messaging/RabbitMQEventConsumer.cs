using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using InventoryService.Application.Events.Handlers;

namespace InventoryService.Infrastructure.Messaging;

public class RabbitMQEventConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private const string ExchangeName = "product_events";
    private const string QueueName = "inventory_service_queue";

    public RabbitMQEventConsumer(
        IConfiguration configuration,
        ILogger<RabbitMQEventConsumer> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var connectionString = configuration["RabbitMQ:ConnectionString"] 
            ?? "amqp://guest:guest@localhost:5672/";
        
        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Declare exchange
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
        
        // Declare queue
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, null);
        
        // Bind queue to exchange with routing keys
        _channel.QueueBind(QueueName, ExchangeName, "product.created");
        _channel.QueueBind(QueueName, ExchangeName, "product.updated");
        _channel.QueueBind(QueueName, ExchangeName, "product.deleted");
        
        _logger.LogInformation("RabbitMQ consumer initialized");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            try
            {
                _logger.LogInformation("Received event: {RoutingKey}", routingKey);

                // Create a scope for scoped services (Event Handlers and Repositories)
                using (var scope = _serviceProvider.CreateScope())
                {
                    var productCreatedHandler = scope.ServiceProvider.GetRequiredService<ProductCreatedEventHandler>();
                    var productUpdatedHandler = scope.ServiceProvider.GetRequiredService<ProductUpdatedEventHandler>();
                    var productDeletedHandler = scope.ServiceProvider.GetRequiredService<ProductDeletedEventHandler>();

                    switch (routingKey)
                    {
                        case "product.created":
                            await productCreatedHandler.HandleAsync(message);
                            break;
                        case "product.updated":
                            await productUpdatedHandler.HandleAsync(message);
                            break;
                        case "product.deleted":
                            await productDeletedHandler.HandleAsync(message);
                            break;
                        default:
                            _logger.LogWarning("Unknown routing key: {RoutingKey}", routingKey);
                            break;
                    }
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {RoutingKey}", routingKey);
                _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue message
            }
        };

        _channel.BasicConsume(QueueName, autoAck: false, consumer);

        _logger.LogInformation("RabbitMQ consumer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}

