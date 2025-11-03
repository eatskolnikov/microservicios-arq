using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using InventoryService.Application.DTOs;
using InventoryService.Application.Services;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;
using System.Text.Json;
using Xunit;

namespace InventoryService.Tests.Services;

public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<InventoryService.Application.Services.InventoryService>> _loggerMock;
    private readonly InventoryService.Application.Services.InventoryService _inventoryService;

    public InventoryServiceTests()
    {
        _repositoryMock = new Mock<IInventoryRepository>();
        _mapperMock = new Mock<IMapper>();
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<InventoryService.Application.Services.InventoryService>>();

        _inventoryService = new InventoryService.Application.Services.InventoryService(
            _repositoryMock.Object,
            _mapperMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetStockByProductIdAsync_WithValidProductId_ReturnsStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = "Test Product",
            ProductSKU = "SKU123",
            CurrentStock = 100
        };

        _repositoryMock.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);
        
        // Note: We can't mock extension methods directly, so we mock GetAsync instead
        // The cache extension methods will call GetAsync internally
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _inventoryService.GetStockByProductIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.ProductName.Should().Be("Test Product");
        result.CurrentStock.Should().Be(100);
        _repositoryMock.Verify(r => r.GetByProductIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task GetStockByProductIdAsync_WithCachedData_ReturnsCachedStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var cachedStock = new StockDto
        {
            ProductId = productId,
            ProductName = "Test Product",
            CurrentStock = 100
        };

        var cachedJson = JsonSerializer.Serialize(cachedStock);
        var cachedBytes = System.Text.Encoding.UTF8.GetBytes(cachedJson);
        
        // Mock GetAsync which is what GetStringAsync uses internally
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedBytes);

        // Act
        var result = await _inventoryService.GetStockByProductIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.CurrentStock.Should().Be(100);
        _repositoryMock.Verify(r => r.GetByProductIdAsync(productId), Times.Never);
    }

    [Fact]
    public async Task GetStockByProductIdAsync_WithInvalidProductId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _repositoryMock.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync((Inventory?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _inventoryService.GetStockByProductIdAsync(productId));
    }

    [Fact]
    public async Task AdjustInventoryAsync_WithInMovement_IncreasesStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = "Test Product",
            CurrentStock = 100
        };

        var adjustDto = new AdjustInventoryDto
        {
            ProductId = productId,
            QuantityChange = 50,
            MovementType = "In",
            Reason = "Restock"
        };

        var inventoryDto = new InventoryDto
        {
            Id = inventory.Id,
            ProductId = productId,
            CurrentStock = 150
        };

        _repositoryMock.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Inventory>()))
            .ReturnsAsync((Inventory inv) => inv);
        _repositoryMock.Setup(r => r.AddMovementAsync(It.IsAny<InventoryMovement>()))
            .ReturnsAsync((InventoryMovement m) => m);
        _mapperMock.Setup(m => m.Map<InventoryDto>(It.IsAny<Inventory>()))
            .Returns(inventoryDto);
        _cacheMock.Setup(c => c.RemoveAsync($"stock:{productId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _inventoryService.AdjustInventoryAsync(adjustDto);

        // Assert
        result.Should().NotBeNull();
        result.CurrentStock.Should().Be(150);
        _repositoryMock.Verify(r => r.AddMovementAsync(It.Is<InventoryMovement>(m => 
            m.MovementType == "In" && m.QuantityChange == 50)), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"stock:{productId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdjustInventoryAsync_WithOutMovement_DecreasesStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = "Test Product",
            CurrentStock = 100
        };

        var adjustDto = new AdjustInventoryDto
        {
            ProductId = productId,
            QuantityChange = 30,
            MovementType = "Out",
            Reason = "Sale"
        };

        var inventoryDto = new InventoryDto
        {
            Id = inventory.Id,
            ProductId = productId,
            CurrentStock = 70
        };

        _repositoryMock.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Inventory>()))
            .ReturnsAsync((Inventory inv) => inv);
        _repositoryMock.Setup(r => r.AddMovementAsync(It.IsAny<InventoryMovement>()))
            .ReturnsAsync((InventoryMovement m) => m);
        _mapperMock.Setup(m => m.Map<InventoryDto>(It.IsAny<Inventory>()))
            .Returns(inventoryDto);
        _cacheMock.Setup(c => c.RemoveAsync($"stock:{productId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _inventoryService.AdjustInventoryAsync(adjustDto);

        // Assert
        result.Should().NotBeNull();
        result.CurrentStock.Should().Be(70);
        _repositoryMock.Verify(r => r.AddMovementAsync(It.Is<InventoryMovement>(m => 
            m.MovementType == "Out" && m.QuantityChange == -30)), Times.Once);
    }

    [Fact]
    public async Task AdjustInventoryAsync_WithInsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CurrentStock = 50
        };

        var adjustDto = new AdjustInventoryDto
        {
            ProductId = productId,
            QuantityChange = 100,
            MovementType = "Out",
            Reason = "Sale"
        };

        _repositoryMock.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _inventoryService.AdjustInventoryAsync(adjustDto));
    }

    [Fact]
    public async Task AdjustInventoryAsync_WithInvalidMovementType_ThrowsArgumentException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CurrentStock = 100
        };

        var adjustDto = new AdjustInventoryDto
        {
            ProductId = productId,
            QuantityChange = 50,
            MovementType = "Invalid",
            Reason = "Test"
        };

        _repositoryMock.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(inventory);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _inventoryService.AdjustInventoryAsync(adjustDto));
    }

    [Fact]
    public async Task GetMovementHistoryAsync_ReturnsMovements()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var movements = new List<InventoryMovement>
        {
            new InventoryMovement
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                MovementType = "In",
                QuantityChange = 50
            }
        };

        var movementDtos = new List<InventoryMovementDto>
        {
            new InventoryMovementDto
            {
                Id = movements[0].Id,
                ProductId = productId,
                MovementType = "In",
                QuantityChange = 50
            }
        };

        _repositoryMock.Setup(r => r.GetMovementsByProductIdAsync(productId))
            .ReturnsAsync(movements);
        _mapperMock.Setup(m => m.Map<IEnumerable<InventoryMovementDto>>(movements))
            .Returns(movementDtos);

        // Act
        var result = await _inventoryService.GetMovementHistoryAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }
}

