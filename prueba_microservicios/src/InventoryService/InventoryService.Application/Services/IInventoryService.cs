using InventoryService.Application.DTOs;

namespace InventoryService.Application.Services;

public interface IInventoryService
{
    Task<StockDto> GetStockByProductIdAsync(Guid productId);
    Task<IEnumerable<InventoryMovementDto>> GetMovementHistoryAsync(Guid productId);
    Task<InventoryDto> AdjustInventoryAsync(AdjustInventoryDto dto);
}

