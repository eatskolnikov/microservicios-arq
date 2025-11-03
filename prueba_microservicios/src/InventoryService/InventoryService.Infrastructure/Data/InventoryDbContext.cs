using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;

namespace InventoryService.Infrastructure.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Inventory configuration
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ProductSKU).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.ProductId).IsUnique();
            entity.HasIndex(e => e.ProductSKU);
            
            entity.HasMany(e => e.Movements)
                .WithOne(e => e.Inventory)
                .HasForeignKey(e => e.InventoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // InventoryMovement configuration
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MovementType).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.Timestamp);
        });

        // ProcessedEvent configuration for idempotency
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.EventId).IsUnique();
        });
    }
}

