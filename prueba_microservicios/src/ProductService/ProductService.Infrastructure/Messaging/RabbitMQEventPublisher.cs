using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductService.Application.Interfaces;
using RabbitMQ.Client;

namespace ProductService.Infrastructure.Messaging;

public class RabbitMQEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private const string ExchangeName = "product_events";

    public RabbitMQEventPublisher(IConfiguration configuration, ILogger<RabbitMQEventPublisher> logger)
    {
        _logger = logger;
        
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
        
        _logger.LogInformation("RabbitMQ connection established");
    }

    public async Task PublishProductCreatedAsync(Application.Events.ProductCreatedEvent evt)
    {
        await PublishEventAsync("product.created", evt);
    }

    public async Task PublishProductUpdatedAsync(Application.Events.ProductUpdatedEvent evt)
    {
        await PublishEventAsync("product.updated", evt);
    }

    public async Task PublishProductDeletedAsync(Application.Events.ProductDeletedEvent evt)
    {
        await PublishEventAsync("product.deleted", evt);
    }

    private async Task PublishEventAsync(string routingKey, object evt)
    {
        try
        {
            var message = JsonSerializer.Serialize(evt);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Event published: {RoutingKey} - {EventId}", routingKey, 
                evt.GetType().GetProperty("EventId")?.GetValue(evt)?.ToString() ?? "unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event: {RoutingKey}", routingKey);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}

