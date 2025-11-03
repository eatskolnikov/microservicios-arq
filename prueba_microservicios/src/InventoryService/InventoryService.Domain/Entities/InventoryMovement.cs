namespace InventoryService.Domain.Entities;

public class InventoryMovement
{
    public Guid Id { get; set; }
    public Guid InventoryId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityChange { get; set; }
    public string MovementType { get; set; } = string.Empty; // "In" or "Out"
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Inventory? Inventory { get; set; }
}

