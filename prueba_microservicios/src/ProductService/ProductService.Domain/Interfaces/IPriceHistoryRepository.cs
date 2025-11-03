using ProductService.Domain.Entities;

namespace ProductService.Domain.Interfaces;

public interface IPriceHistoryRepository
{
    Task<PriceHistory> CreateAsync(PriceHistory priceHistory);
    Task<IEnumerable<PriceHistory>> GetByProductIdAsync(Guid productId);
}

