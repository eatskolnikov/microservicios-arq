using InventoryService.Domain.Entities;

namespace InventoryService.Domain.Interfaces;

public interface IInventoryRepository
{
    Task<Inventory?> GetByProductIdAsync(Guid productId);
    Task<Inventory?> GetByIdAsync(Guid id);
    Task<IEnumerable<Inventory>> GetAllAsync();
    Task<Inventory> CreateAsync(Inventory inventory);
    Task<Inventory> UpdateAsync(Inventory inventory);
    Task<bool> DeleteAsync(Guid id);
    Task<InventoryMovement> AddMovementAsync(InventoryMovement movement);
    Task<IEnumerable<InventoryMovement>> GetMovementsByProductIdAsync(Guid productId);
    Task<bool> IsEventProcessedAsync(Guid eventId);
    Task MarkEventAsProcessedAsync(Guid eventId, string eventType);
}

