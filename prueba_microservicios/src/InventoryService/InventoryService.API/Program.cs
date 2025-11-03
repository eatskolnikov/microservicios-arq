using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using InventoryService.Application.Events.Handlers;
using InventoryService.Application.Mappings;
using InventoryService.Application.Services;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Caching;
using InventoryService.Infrastructure.Data;
using InventoryService.Infrastructure.Data.Repositories;
using InventoryService.Infrastructure.Messaging;
using Serilog;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;

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

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Inventory Service API", 
        Version = "v1",
        Description = "Microservicio de gestión de inventario"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure JWT Authentication (validate tokens issued by AuthService)
var jwtSecret = builder.Configuration["JWT:SecretKey"] 
    ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "AuthService";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("InventoryDb")
    ?? "Host=localhost;Database=inventory;Username=postgres;Password=postgres";

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Redis
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddRedisDistributedCache(redisConnection);

// Register Repositories
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

// Register Services
builder.Services.AddScoped<IInventoryService, InventoryService.Application.Services.InventoryService>();

// Register Event Handlers (as Scoped - will be resolved from scope in BackgroundService)
builder.Services.AddScoped<ProductCreatedEventHandler>();
builder.Services.AddScoped<ProductUpdatedEventHandler>();
builder.Services.AddScoped<ProductDeletedEventHandler>();

// Register RabbitMQ Consumer as Background Service
builder.Services.AddHostedService<RabbitMQEventConsumer>();

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(InventoryMappingProfile));

// Configure FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<InventoryService.Application.Validators.AdjustInventoryValidator>();

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();
}

Log.Information("Inventory Service starting up...");

app.Run();
