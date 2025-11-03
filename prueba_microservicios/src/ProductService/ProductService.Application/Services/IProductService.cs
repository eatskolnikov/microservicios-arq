using ProductService.Application.DTOs;

namespace ProductService.Application.Services;

public interface IProductService
{
    Task<ProductDto> GetByIdAsync(Guid id, string? currency = null);
    Task<IEnumerable<ProductDto>> GetAllAsync(string? currency = null);
    Task<IEnumerable<ProductDto>> GetByCategoryAsync(string category, string? currency = null);
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<ProductPriceHistoryDto>> GetPriceHistoryAsync(Guid productId);
}

