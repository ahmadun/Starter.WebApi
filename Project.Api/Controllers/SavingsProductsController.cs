using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/savings-products")]
[Produces("application/json")]
[Authorize(Roles = "admin,manager")]
public sealed class SavingsProductsController : ControllerBase
{
    private readonly ISavingsProductService _service;

    public SavingsProductsController(ISavingsProductService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SavingsProductFilterParams filters)
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveSavingsProductRequest request)
    {
        var result = await _service.CreateAsync(request);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.SavingsProductId }, result) : BadRequest(result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] SaveSavingsProductRequest request)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.UpdateAsync(tenantId, id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    private bool TryGetTenantId(out long tenantId) => long.TryParse(User.FindFirstValue("tenant_id"), out tenantId);
}
