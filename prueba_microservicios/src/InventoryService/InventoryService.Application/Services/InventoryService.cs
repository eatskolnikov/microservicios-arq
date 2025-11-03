using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using InventoryService.Application.DTOs;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;

namespace InventoryService.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _repository;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IInventoryRepository repository,
        IMapper mapper,
        IDistributedCache cache,
        ILogger<InventoryService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<StockDto> GetStockByProductIdAsync(Guid productId)
    {
        // Try cache first
        var cacheKey = $"stock:{productId}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<StockDto>(cached)!;
        }

        var inventory = await _repository.GetByProductIdAsync(productId);
        if (inventory == null)
            throw new KeyNotFoundException($"Inventory for product {productId} not found");

        var stock = new StockDto
        {
            ProductId = inventory.ProductId,
            ProductName = inventory.ProductName,
            ProductSKU = inventory.ProductSKU,
            CurrentStock = inventory.CurrentStock
        };

        // Cache for 2 minutes
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(stock), options);

        return stock;
    }

    public async Task<IEnumerable<InventoryMovementDto>> GetMovementHistoryAsync(Guid productId)
    {
        var movements = await _repository.GetMovementsByProductIdAsync(productId);
        return _mapper.Map<IEnumerable<InventoryMovementDto>>(movements);
    }

    public async Task<InventoryDto> AdjustInventoryAsync(AdjustInventoryDto dto)
    {
        var inventory = await _repository.GetByProductIdAsync(dto.ProductId);
        
        if (inventory == null)
            throw new KeyNotFoundException($"Inventory for product {dto.ProductId} not found");

        // Adjust stock
        if (dto.MovementType == "In")
        {
            inventory.CurrentStock += dto.QuantityChange;
        }
        else if (dto.MovementType == "Out")
        {
            inventory.CurrentStock -= dto.QuantityChange;
            
            if (inventory.CurrentStock < 0)
                throw new InvalidOperationException("Insufficient stock");
        }
        else
        {
            throw new ArgumentException("MovementType must be 'In' or 'Out'");
        }

        // Record movement
        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            InventoryId = inventory.Id,
            ProductId = dto.ProductId,
            QuantityChange = dto.MovementType == "In" ? dto.QuantityChange : -dto.QuantityChange,
            MovementType = dto.MovementType,
            Reason = dto.Reason
        };

        await _repository.AddMovementAsync(movement);
        await _repository.UpdateAsync(inventory);

        // Clear cache
        await _cache.RemoveAsync($"stock:{dto.ProductId}");

        _logger.LogInformation("Inventory adjusted for product {ProductId}: {MovementType} {Quantity}",
            dto.ProductId, dto.MovementType, dto.QuantityChange);

        return _mapper.Map<InventoryDto>(inventory);
    }
}

