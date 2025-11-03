using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProductService.Application.DTOs;
using ProductService.Application.Events;
using ProductService.Application.Interfaces;
using ProductService.Application.Services;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using Xunit;

namespace ProductService.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ICurrencyConverterService> _currencyConverterMock;
    private readonly Mock<ILogger<ProductService.Application.Services.ProductService>> _loggerMock;
    private readonly ProductService.Application.Services.ProductService _productService;

    public ProductServiceTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _priceHistoryRepositoryMock = new Mock<IPriceHistoryRepository>();
        _mapperMock = new Mock<IMapper>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _currencyConverterMock = new Mock<ICurrencyConverterService>();
        _loggerMock = new Mock<ILogger<ProductService.Application.Services.ProductService>>();

        _productService = new ProductService.Application.Services.ProductService(
            _repositoryMock.Object,
            _priceHistoryRepositoryMock.Object,
            _mapperMock.Object,
            _eventPublisherMock.Object,
            _currencyConverterMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsProductDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Price = 100.00m,
            Category = "Electronics"
        };

        var productDto = new ProductDto
        {
            Id = productId,
            Name = "Test Product",
            Price = 100.00m,
            Category = "Electronics"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);
        _mapperMock.Setup(m => m.Map<ProductDto>(product))
            .Returns(productDto);

        // Act
        var result = await _productService.GetByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Name.Should().Be("Test Product");
        _repositoryMock.Verify(r => r.GetByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _productService.GetByIdAsync(productId));
    }

    [Fact]
    public async Task GetByIdAsync_WithCurrency_ConvertsPrice()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Price = 100.00m
        };

        var productDto = new ProductDto
        {
            Id = productId,
            Name = "Test Product",
            Price = 100.00m
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);
        _mapperMock.Setup(m => m.Map<ProductDto>(product))
            .Returns(productDto);
        _currencyConverterMock.Setup(c => c.ConvertAsync(100.00m, "USD", "EUR"))
            .ReturnsAsync(90.00m);

        // Act
        var result = await _productService.GetByIdAsync(productId, "EUR");

        // Assert
        result.Price.Should().Be(90.00m);
        _currencyConverterMock.Verify(c => c.ConvertAsync(100.00m, "USD", "EUR"), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesProductAndPublishesEvent()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "New Product",
            Description = "Description",
            Price = 50.00m,
            Category = "Electronics",
            SKU = "SKU123"
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Description = createDto.Description,
            Price = createDto.Price,
            Category = createDto.Category,
            SKU = createDto.SKU
        };

        var productDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price
        };

        _mapperMock.Setup(m => m.Map<Product>(createDto))
            .Returns(product);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);
        _mapperMock.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns(productDto);
        _priceHistoryRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<PriceHistory>()))
            .ReturnsAsync((PriceHistory ph) => ph);

        // Act
        var result = await _productService.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Product");
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Once);
        _eventPublisherMock.Verify(e => e.PublishProductCreatedAsync(It.IsAny<ProductCreatedEvent>()), Times.Once);
        _priceHistoryRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<PriceHistory>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithValidId_UpdatesProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product
        {
            Id = productId,
            Name = "Old Name",
            Price = 100.00m
        };

        var updateDto = new UpdateProductDto
        {
            Name = "New Name",
            Price = 150.00m
        };

        var updatedProduct = new Product
        {
            Id = productId,
            Name = "New Name",
            Price = 150.00m
        };

        var productDto = new ProductDto
        {
            Id = productId,
            Name = "New Name",
            Price = 150.00m
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(existingProduct);
        _mapperMock.Setup(m => m.Map(updateDto, existingProduct));
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);
        _mapperMock.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns(productDto);
        _priceHistoryRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<PriceHistory>()))
            .ReturnsAsync((PriceHistory ph) => ph);

        // Act
        var result = await _productService.UpdateAsync(productId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        result.Price.Should().Be(150.00m);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Once);
        _eventPublisherMock.Verify(e => e.PublishProductUpdatedAsync(It.IsAny<ProductUpdatedEvent>()), Times.Once);
        _priceHistoryRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<PriceHistory>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync(productId))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.DeleteAsync(productId))
            .ReturnsAsync(true);

        // Act
        var result = await _productService.DeleteAsync(productId);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(productId), Times.Once);
        _eventPublisherMock.Verify(e => e.PublishProductDeletedAsync(It.IsAny<ProductDeletedEvent>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync(productId))
            .ReturnsAsync(false);

        // Act
        var result = await _productService.DeleteAsync(productId);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(productId), Times.Never);
        _eventPublisherMock.Verify(e => e.PublishProductDeletedAsync(It.IsAny<ProductDeletedEvent>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = Guid.NewGuid(), Name = "Product 1", Price = 10.00m },
            new Product { Id = Guid.NewGuid(), Name = "Product 2", Price = 20.00m }
        };

        var productDtos = new List<ProductDto>
        {
            new ProductDto { Id = products[0].Id, Name = "Product 1", Price = 10.00m },
            new ProductDto { Id = products[1].Id, Name = "Product 2", Price = 20.00m }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(products);
        _mapperMock.Setup(m => m.Map<IEnumerable<ProductDto>>(products))
            .Returns(productDtos);

        // Act
        var result = await _productService.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }
}

