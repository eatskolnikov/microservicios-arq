using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.DTOs;
using ProductService.Application.Services;

namespace ProductService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los productos
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll([FromQuery] string? currency = null)
    {
        try
        {
            var products = await _productService.GetAllAsync(currency);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all products");
            return StatusCode(500, "An error occurred while retrieving products");
        }
    }

    /// <summary>
    /// Obtiene productos por categoría
    /// </summary>
    [HttpGet("category/{category}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetByCategory(
        string category, 
        [FromQuery] string? currency = null)
    {
        try
        {
            var products = await _productService.GetByCategoryAsync(category, currency);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by category: {Category}", category);
            return StatusCode(500, "An error occurred while retrieving products");
        }
    }

    /// <summary>
    /// Obtiene un producto por ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, [FromQuery] string? currency = null)
    {
        try
        {
            var product = await _productService.GetByIdAsync(id, currency);
            return Ok(product);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Product with id {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by id: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the product");
        }
    }

    /// <summary>
    /// Crea un nuevo producto
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        try
        {
            var product = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, "An error occurred while creating the product");
        }
    }

    /// <summary>
    /// Actualiza un producto existente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        try
        {
            var product = await _productService.UpdateAsync(id, dto);
            return Ok(product);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Product with id {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product: {Id}", id);
            return StatusCode(500, "An error occurred while updating the product");
        }
    }

    /// <summary>
    /// Elimina un producto
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _productService.DeleteAsync(id);
            if (!deleted)
                return NotFound($"Product with id {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product: {Id}", id);
            return StatusCode(500, "An error occurred while deleting the product");
        }
    }

    /// <summary>
    /// Obtiene el historial de precios de un producto
    /// </summary>
    [HttpGet("{id}/price-history")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductPriceHistoryDto>>> GetPriceHistory(Guid id)
    {
        try
        {
            var history = await _productService.GetPriceHistoryAsync(id);
            return Ok(history);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Product with id {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history for product: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving price history");
        }
    }
}

