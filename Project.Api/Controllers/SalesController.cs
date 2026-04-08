using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Produces("application/json")]
[Authorize(Roles = "admin,cashier,manager")]
public sealed class SalesController : ControllerBase
{
    private readonly ISaleService _service;

    public SalesController(ISaleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SaleFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetAllAsync(tenantId, filters));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.GetByIdAsync(tenantId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id:long}/receipt")]
    public async Task<IActionResult> GetReceipt(long id)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.GetReceiptAsync(tenantId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleRequest request)
    {
        if (!TryGetUserId(out var userId) || !TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.CreateAsync(userId, request with { TenantId = tenantId });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{saleId:long}/convert-to-loan")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> ConvertToLoan(long saleId, [FromBody] ConvertMemberCreditSaleRequest request)
    {
        if (!TryGetUserId(out var userId) || !TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.ConvertMemberCreditToLoanAsync(userId, tenantId, saleId, request with { TenantId = tenantId });
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    private bool TryGetTenantId(out long tenantId) => long.TryParse(User.FindFirstValue("tenant_id"), out tenantId);
    private bool TryGetUserId(out long userId) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
