using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/savings-transactions")]
[Produces("application/json")]
[Authorize(Roles = "admin,manager,cashier")]
public sealed class SavingsTransactionsController : ControllerBase
{
    private readonly ISavingsTransactionService _service;

    public SavingsTransactionsController(ISavingsTransactionService service)
    {
        _service = service;
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts([FromQuery] SavingsAccountFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetAccountsAsync(tenantId, filters));
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions([FromQuery] SavingsTransactionFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetTransactionsAsync(tenantId, filters));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.GetByIdAsync(tenantId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSavingsTransactionRequest request)
    {
        if (!TryGetUserId(out var userId) || !TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.CreateAsync(userId, request with { TenantId = tenantId });
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.SavingsTransactionId }, result) : BadRequest(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.DeleteAsync(tenantId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    private bool TryGetTenantId(out long tenantId) => long.TryParse(User.FindFirstValue("tenant_id"), out tenantId);
    private bool TryGetUserId(out long userId) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
