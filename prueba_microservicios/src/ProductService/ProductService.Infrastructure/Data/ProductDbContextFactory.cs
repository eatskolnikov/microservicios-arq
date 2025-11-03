using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductService.Infrastructure.Data;

public class ProductDbContextFactory : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductDbContext>();
        
        // For design-time, use a default connection string
        // This will be overridden by the actual connection string at runtime
        optionsBuilder.UseNpgsql("Host=localhost;Database=products;Username=postgres;Password=postgres");

        return new ProductDbContext(optionsBuilder.Options);
    }
}

