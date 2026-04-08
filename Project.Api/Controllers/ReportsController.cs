using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
[Authorize(Roles = "admin,manager")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportingService _service;

    public ReportsController(IReportingService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetDashboardAsync(tenantId));
    }

    [HttpGet("sales-summary")]
    public async Task<IActionResult> GetSalesSummary([FromQuery] ReportingPeriodFilter filter)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.GetSalesSummaryAsync(tenantId, filter);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("member-balances")]
    public async Task<IActionResult> GetMemberBalances([FromQuery] MemberBalanceFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetMemberBalanceSummaryAsync(tenantId, filters));
    }

    [HttpGet("low-stock-products")]
    public async Task<IActionResult> GetLowStockProducts([FromQuery] LowStockFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetLowStockProductsAsync(tenantId, filters));
    }

    private bool TryGetTenantId(out long tenantId) => long.TryParse(User.FindFirstValue("tenant_id"), out tenantId);
}
