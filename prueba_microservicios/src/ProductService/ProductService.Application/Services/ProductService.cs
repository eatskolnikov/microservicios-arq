using AutoMapper;
using Microsoft.Extensions.Logging;
using ProductService.Application.DTOs;
using ProductService.Application.Events;
using ProductService.Application.Interfaces;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IMapper _mapper;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICurrencyConverterService _currencyConverter;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository repository,
        IPriceHistoryRepository priceHistoryRepository,
        IMapper mapper,
        IEventPublisher eventPublisher,
        ICurrencyConverterService currencyConverter,
        ILogger<ProductService> logger)
    {
        _repository = repository;
        _priceHistoryRepository = priceHistoryRepository;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
        _currencyConverter = currencyConverter;
        _logger = logger;
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, string? currency = null)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product with id {id} not found");

        var dto = _mapper.Map<ProductDto>(product);

        if (!string.IsNullOrEmpty(currency) && currency.ToUpper() != "USD")
        {
            dto.Price = await _currencyConverter.ConvertAsync(dto.Price, "USD", currency.ToUpper());
        }

        return dto;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(string? currency = null)
    {
        var products = await _repository.GetAllAsync();
        var dtos = _mapper.Map<IEnumerable<ProductDto>>(products);

        if (!string.IsNullOrEmpty(currency) && currency.ToUpper() != "USD")
        {
            var convertedDtos = new List<ProductDto>();
            foreach (var dto in dtos)
            {
                dto.Price = await _currencyConverter.ConvertAsync(dto.Price, "USD", currency.ToUpper());
                convertedDtos.Add(dto);
            }
            return convertedDtos;
        }

        return dtos;
    }

    public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(string category, string? currency = null)
    {
        var products = await _repository.GetByCategoryAsync(category);
        var dtos = _mapper.Map<IEnumerable<ProductDto>>(products);

        if (!string.IsNullOrEmpty(currency) && currency.ToUpper() != "USD")
        {
            var convertedDtos = new List<ProductDto>();
            foreach (var dto in dtos)
            {
                dto.Price = await _currencyConverter.ConvertAsync(dto.Price, "USD", currency.ToUpper());
                convertedDtos.Add(dto);
            }
            return convertedDtos;
        }

        return dtos;
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var product = _mapper.Map<Domain.Entities.Product>(dto);
        product.Id = Guid.NewGuid();

        var created = await _repository.CreateAsync(product);

        // Record price history
        await RecordPriceHistoryAsync(created.Id, created.Price, "USD");

        // Publish event
        await _eventPublisher.PublishProductCreatedAsync(new ProductCreatedEvent
        {
            ProductId = created.Id,
            Name = created.Name,
            SKU = created.SKU,
            Category = created.Category
        });

        _logger.LogInformation("Product created: {ProductId}", created.Id);

        return _mapper.Map<ProductDto>(created);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product with id {id} not found");

        var oldPrice = product.Price;

        _mapper.Map(dto, product);
        var updated = await _repository.UpdateAsync(product);

        // Record price history if price changed
        if (dto.Price.HasValue && dto.Price.Value != oldPrice)
        {
            await RecordPriceHistoryAsync(updated.Id, updated.Price, "USD");
        }

        // Publish event
        var changes = new Dictionary<string, object>();
        if (dto.Name != null) changes["Name"] = dto.Name;
        if (dto.Description != null) changes["Description"] = dto.Description;
        if (dto.Price.HasValue) changes["Price"] = dto.Price.Value;
        if (dto.Category != null) changes["Category"] = dto.Category;

        await _eventPublisher.PublishProductUpdatedAsync(new ProductUpdatedEvent
        {
            ProductId = updated.Id,
            Changes = changes
        });

        _logger.LogInformation("Product updated: {ProductId}", updated.Id);

        return _mapper.Map<ProductDto>(updated);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var exists = await _repository.ExistsAsync(id);
        if (!exists)
            return false;

        var deleted = await _repository.DeleteAsync(id);

        if (deleted)
        {
            await _eventPublisher.PublishProductDeletedAsync(new ProductDeletedEvent
            {
                ProductId = id
            });

            _logger.LogInformation("Product deleted: {ProductId}", id);
        }

        return deleted;
    }

    public async Task<IEnumerable<ProductPriceHistoryDto>> GetPriceHistoryAsync(Guid productId)
    {
        var product = await _repository.GetByIdAsync(productId);
        if (product == null)
            throw new KeyNotFoundException($"Product with id {productId} not found");

        return _mapper.Map<IEnumerable<ProductPriceHistoryDto>>(product.PriceHistories.OrderByDescending(h => h.Date));
    }

    private async Task RecordPriceHistoryAsync(Guid productId, decimal price, string currency)
    {
        var priceHistory = new Domain.Entities.PriceHistory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Price = price,
            Currency = currency,
            Date = DateTime.UtcNow
        };

        await _priceHistoryRepository.CreateAsync(priceHistory);
    }
}

