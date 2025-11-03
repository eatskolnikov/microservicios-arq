using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using AuthService.Application.Services;
using AuthService.Application.Validators;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Data.Repositories;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Auth Service API", 
        Version = "v1",
        Description = "Microservicio de autenticación y autorización"
    });
});

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("AuthDb")
    ?? "Host=localhost;Database=auth;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register Services
builder.Services.AddScoped<IAuthService, AuthService.Application.Services.AuthService>();

// Configure AutoMapper (if needed later)
// Not needed for now

// Configure FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();
    
    // Seed initial users if database is empty
    if (!db.Users.Any())
    {
        await SeedInitialUsersAsync(db);
    }
}

Log.Information("Auth Service starting up...");

app.Run();

async Task SeedInitialUsersAsync(AuthDbContext db)
{
    var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
    var userPasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!");

    var adminUser = new AuthService.Domain.Entities.User
    {
        Id = Guid.NewGuid(),
        Username = "admin",
        Email = "admin@test.com",
        PasswordHash = adminPasswordHash,
        Role = "Admin",
        CreatedAt = DateTime.UtcNow
    };

    var normalUser = new AuthService.Domain.Entities.User
    {
        Id = Guid.NewGuid(),
        Username = "user",
        Email = "user@test.com",
        PasswordHash = userPasswordHash,
        Role = "User",
        CreatedAt = DateTime.UtcNow
    };

    db.Users.Add(adminUser);
    db.Users.Add(normalUser);
    await db.SaveChangesAsync();

    Log.Information("Initial users seeded");
}
