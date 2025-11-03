using System.Text.Json;
using Microsoft.Extensions.Logging;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;

namespace InventoryService.Application.Events.Handlers;

public class ProductCreatedEventHandler
{
    private readonly IInventoryRepository _repository;
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(
        IInventoryRepository repository,
        ILogger<ProductCreatedEventHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task HandleAsync(string messageBody)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<ProductCreatedEvent>(messageBody);
            if (evt == null)
            {
                _logger.LogWarning("Failed to deserialize ProductCreatedEvent");
                return;
            }

            // Check if event was already processed (idempotency)
            var isProcessed = await _repository.IsEventProcessedAsync(evt.EventId);
            if (isProcessed)
            {
                _logger.LogInformation("Event {EventId} already processed, skipping", evt.EventId);
                return;
            }

            // Create inventory record
            var inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                ProductId = evt.ProductId,
                ProductName = evt.Name,
                ProductSKU = evt.SKU,
                CurrentStock = 0, // Initial stock is 0
                IsActive = true
            };

            await _repository.CreateAsync(inventory);
            await _repository.MarkEventAsProcessedAsync(evt.EventId, "ProductCreated");

            _logger.LogInformation("Product created event processed: ProductId={ProductId}, EventId={EventId}",
                evt.ProductId, evt.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ProductCreatedEvent: {MessageBody}", messageBody);
            throw;
        }
    }

    private class ProductCreatedEvent
    {
        public Guid EventId { get; set; }
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}

public class ProductUpdatedEventHandler
{
    private readonly IInventoryRepository _repository;
    private readonly ILogger<ProductUpdatedEventHandler> _logger;

    public ProductUpdatedEventHandler(
        IInventoryRepository repository,
        ILogger<ProductUpdatedEventHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task HandleAsync(string messageBody)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<ProductUpdatedEvent>(messageBody);
            if (evt == null)
            {
                _logger.LogWarning("Failed to deserialize ProductUpdatedEvent");
                return;
            }

            // Check idempotency
            var isProcessed = await _repository.IsEventProcessedAsync(evt.EventId);
            if (isProcessed)
            {
                _logger.LogInformation("Event {EventId} already processed, skipping", evt.EventId);
                return;
            }

            var inventory = await _repository.GetByProductIdAsync(evt.ProductId);
            if (inventory != null)
            {
                // Update product name if changed
                if (evt.Changes.ContainsKey("Name"))
                {
                    inventory.ProductName = evt.Changes["Name"]?.ToString() ?? inventory.ProductName;
                }

                await _repository.UpdateAsync(inventory);
                await _repository.MarkEventAsProcessedAsync(evt.EventId, "ProductUpdated");

                _logger.LogInformation("Product updated event processed: ProductId={ProductId}, EventId={EventId}",
                    evt.ProductId, evt.EventId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ProductUpdatedEvent: {MessageBody}", messageBody);
            throw;
        }
    }

    private class ProductUpdatedEvent
    {
        public Guid EventId { get; set; }
        public Guid ProductId { get; set; }
        public Dictionary<string, object> Changes { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }
}

public class ProductDeletedEventHandler
{
    private readonly IInventoryRepository _repository;
    private readonly ILogger<ProductDeletedEventHandler> _logger;

    public ProductDeletedEventHandler(
        IInventoryRepository repository,
        ILogger<ProductDeletedEventHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task HandleAsync(string messageBody)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<ProductDeletedEvent>(messageBody);
            if (evt == null)
            {
                _logger.LogWarning("Failed to deserialize ProductDeletedEvent");
                return;
            }

            // Check idempotency
            var isProcessed = await _repository.IsEventProcessedAsync(evt.EventId);
            if (isProcessed)
            {
                _logger.LogInformation("Event {EventId} already processed, skipping", evt.EventId);
                return;
            }

            // Soft delete inventory
            var inventory = await _repository.GetByProductIdAsync(evt.ProductId);
            if (inventory != null)
            {
                await _repository.DeleteAsync(inventory.Id);
                await _repository.MarkEventAsProcessedAsync(evt.EventId, "ProductDeleted");

                _logger.LogInformation("Product deleted event processed: ProductId={ProductId}, EventId={EventId}",
                    evt.ProductId, evt.EventId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ProductDeletedEvent: {MessageBody}", messageBody);
            throw;
        }
    }

    private class ProductDeletedEvent
    {
        public Guid EventId { get; set; }
        public Guid ProductId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

