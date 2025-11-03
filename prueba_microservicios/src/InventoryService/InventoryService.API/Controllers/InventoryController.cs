using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryService.Application.DTOs;
using InventoryService.Application.Services;

namespace InventoryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el stock disponible de un producto
    /// </summary>
    [HttpGet("{productId}/stock")]
    [AllowAnonymous]
    public async Task<ActionResult<StockDto>> GetStock(Guid productId)
    {
        try
        {
            var stock = await _inventoryService.GetStockByProductIdAsync(productId);
            return Ok(stock);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Inventory for product {productId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock for product: {ProductId}", productId);
            return StatusCode(500, "An error occurred while retrieving stock");
        }
    }

    /// <summary>
    /// Obtiene el historial de movimientos de inventario de un producto
    /// </summary>
    [HttpGet("{productId}/movements")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<InventoryMovementDto>>> GetMovements(Guid productId)
    {
        try
        {
            var movements = await _inventoryService.GetMovementHistoryAsync(productId);
            return Ok(movements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movements for product: {ProductId}", productId);
            return StatusCode(500, "An error occurred while retrieving movements");
        }
    }

    /// <summary>
    /// Ajusta el inventario de un producto (entrada o salida)
    /// </summary>
    [HttpPost("adjust")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InventoryDto>> AdjustInventory([FromBody] AdjustInventoryDto dto)
    {
        try
        {
            var inventory = await _inventoryService.AdjustInventoryAsync(dto);
            return Ok(inventory);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Inventory for product {dto.ProductId} not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting inventory for product: {ProductId}", dto.ProductId);
            return StatusCode(500, "An error occurred while adjusting inventory");
        }
    }
}

