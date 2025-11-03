using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthService.Infrastructure.Data;

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        
        // For design-time, use a default connection string
        optionsBuilder.UseNpgsql("Host=localhost;Database=auth;Username=postgres;Password=postgres");

        return new AuthDbContext(optionsBuilder.Options);
    }
}

