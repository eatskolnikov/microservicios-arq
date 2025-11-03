namespace ProductService.Application.Events;

public class ProductCreatedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ProductUpdatedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Dictionary<string, object> Changes { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ProductDeletedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

