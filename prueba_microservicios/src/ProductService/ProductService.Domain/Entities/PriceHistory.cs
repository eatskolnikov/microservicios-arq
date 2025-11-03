namespace ProductService.Domain.Entities;

public class PriceHistory
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Product? Product { get; set; }
}

