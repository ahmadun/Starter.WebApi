using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Produces("application/json")]
[Authorize(Roles = "admin,cashier,manager")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;

    public InventoryController(IInventoryService service)
    {
        _service = service;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetCategoriesAsync(tenantId));
    }

    [HttpPost("categories")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> CreateCategory([FromBody] SaveProductCategoryRequest request)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.CreateCategoryAsync(request with { TenantId = tenantId }));
    }

    [HttpPut("categories/{id:long}")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> UpdateCategory(long id, [FromBody] SaveProductCategoryRequest request)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.UpdateCategoryAsync(tenantId, id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers()
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetSuppliersAsync(tenantId));
    }

    [HttpPost("suppliers")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> CreateSupplier([FromBody] SaveSupplierRequest request)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.CreateSupplierAsync(request with { TenantId = tenantId }));
    }

    [HttpPut("suppliers/{id:long}")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> UpdateSupplier(long id, [FromBody] SaveSupplierRequest request)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.UpdateSupplierAsync(tenantId, id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] ProductFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetProductsAsync(tenantId, filters));
    }

    [HttpGet("products/{id:long}")]
    public async Task<IActionResult> GetProductById(long id)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.GetProductByIdAsync(tenantId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("products/lookup")]
    public async Task<IActionResult> LookupProducts([FromQuery] string query, [FromQuery] int limit = 10)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.LookupProductsAsync(tenantId, query, limit));
    }

    [HttpPost("products")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> CreateProduct([FromBody] SaveProductRequest request)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.CreateProductAsync(request with { TenantId = tenantId }));
    }

    [HttpPut("products/{id:long}")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> UpdateProduct(long id, [FromBody] SaveProductRequest request)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.UpdateProductAsync(tenantId, id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpGet("purchase-receipts")]
    public async Task<IActionResult> GetPurchaseReceipts([FromQuery] PurchaseReceiptFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetPurchaseReceiptsAsync(tenantId, filters));
    }

    [HttpGet("purchase-receipts/{id:long}")]
    public async Task<IActionResult> GetPurchaseReceiptById(long id)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.GetPurchaseReceiptByIdAsync(tenantId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("purchase-receipts")]
    [Authorize(Roles = "admin,manager,cashier")]
    public async Task<IActionResult> CreatePurchaseReceipt([FromBody] CreatePurchaseReceiptRequest request)
    {
        if (!TryGetUserId(out var userId) || !TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.CreatePurchaseReceiptAsync(userId, request with { TenantId = tenantId });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("stock-adjustments")]
    public async Task<IActionResult> GetStockAdjustments([FromQuery] StockAdjustmentFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetStockAdjustmentsAsync(tenantId, filters));
    }

    [HttpGet("stock-adjustments/{id:long}")]
    public async Task<IActionResult> GetStockAdjustmentById(long id)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.GetStockAdjustmentByIdAsync(tenantId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("stock-adjustments")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> CreateStockAdjustment([FromBody] CreateStockAdjustmentRequest request)
    {
        if (!TryGetUserId(out var userId) || !TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.CreateStockAdjustmentAsync(userId, request with { TenantId = tenantId });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("stock-movements")]
    public async Task<IActionResult> GetStockMovements([FromQuery] StockMovementFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetStockMovementsAsync(tenantId, filters));
    }

    private bool TryGetTenantId(out long tenantId) => long.TryParse(User.FindFirstValue("tenant_id"), out tenantId);
    private bool TryGetUserId(out long userId) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
