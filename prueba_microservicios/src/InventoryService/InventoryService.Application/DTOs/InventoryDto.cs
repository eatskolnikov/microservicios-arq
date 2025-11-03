namespace InventoryService.Application.DTOs;

public class InventoryDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class StockDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
}

public class AdjustInventoryDto
{
    public Guid ProductId { get; set; }
    public int QuantityChange { get; set; }
    public string MovementType { get; set; } = string.Empty; // "In" or "Out"
    public string? Reason { get; set; }
}

public class InventoryMovementDto
{
    public Guid Id { get; set; }
    public Guid InventoryId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityChange { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
}

