using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InventoryService.Infrastructure.Data;

public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        
        // For design-time, use a default connection string
        // This will be overridden by the actual connection string at runtime
        optionsBuilder.UseNpgsql("Host=localhost;Database=inventory;Username=postgres;Password=postgres");

        return new InventoryDbContext(optionsBuilder.Options);
    }
}

