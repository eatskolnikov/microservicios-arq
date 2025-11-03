using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using ProductService.Infrastructure.Data;

namespace ProductService.Infrastructure.Data.Repositories;

public class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly ProductDbContext _context;

    public PriceHistoryRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<PriceHistory> CreateAsync(PriceHistory priceHistory)
    {
        priceHistory.CreatedAt = DateTime.UtcNow;
        priceHistory.Date = DateTime.UtcNow;
        _context.PriceHistories.Add(priceHistory);
        await _context.SaveChangesAsync();
        return priceHistory;
    }

    public async Task<IEnumerable<PriceHistory>> GetByProductIdAsync(Guid productId)
    {
        return await _context.PriceHistories
            .Where(ph => ph.ProductId == productId)
            .OrderByDescending(ph => ph.Date)
            .ToListAsync();
    }
}

