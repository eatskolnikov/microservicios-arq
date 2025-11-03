using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Data;

namespace InventoryService.Infrastructure.Data.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _context;

    public InventoryRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<Inventory?> GetByProductIdAsync(Guid productId)
    {
        return await _context.Inventories
            .Include(i => i.Movements.OrderByDescending(m => m.Timestamp))
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.IsActive);
    }

    public async Task<Inventory?> GetByIdAsync(Guid id)
    {
        return await _context.Inventories
            .Include(i => i.Movements.OrderByDescending(m => m.Timestamp))
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Inventory>> GetAllAsync()
    {
        return await _context.Inventories
            .Where(i => i.IsActive)
            .Include(i => i.Movements)
            .ToListAsync();
    }

    public async Task<Inventory> CreateAsync(Inventory inventory)
    {
        inventory.LastUpdated = DateTime.UtcNow;
        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();
        return inventory;
    }

    public async Task<Inventory> UpdateAsync(Inventory inventory)
    {
        inventory.LastUpdated = DateTime.UtcNow;
        _context.Inventories.Update(inventory);
        await _context.SaveChangesAsync();
        return inventory;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var inventory = await _context.Inventories.FindAsync(id);
        if (inventory == null)
            return false;

        // Soft delete
        inventory.IsActive = false;
        inventory.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<InventoryMovement> AddMovementAsync(InventoryMovement movement)
    {
        movement.Timestamp = DateTime.UtcNow;
        _context.InventoryMovements.Add(movement);
        await _context.SaveChangesAsync();
        return movement;
    }

    public async Task<IEnumerable<InventoryMovement>> GetMovementsByProductIdAsync(Guid productId)
    {
        return await _context.InventoryMovements
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<bool> IsEventProcessedAsync(Guid eventId)
    {
        return await _context.ProcessedEvents.AnyAsync(e => e.EventId == eventId);
    }

    public async Task MarkEventAsProcessedAsync(Guid eventId, string eventType)
    {
        var processedEvent = new ProcessedEvent
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = eventType,
            ProcessedAt = DateTime.UtcNow
        };

        _context.ProcessedEvents.Add(processedEvent);
        await _context.SaveChangesAsync();
    }
}

