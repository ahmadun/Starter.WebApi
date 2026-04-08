using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/loans")]
[Produces("application/json")]
[Authorize(Roles = "admin,manager,cashier")]
public sealed class LoansController : ControllerBase
{
    private readonly ILoanService _service;

    public LoansController(ILoanService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "admin,manager,cashier")]
    public async Task<IActionResult> GetAll([FromQuery] LoanFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetAllAsync(tenantId, filters));
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "admin,manager,cashier")]
    public async Task<IActionResult> GetById(long id)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.GetByIdAsync(tenantId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> Create([FromBody] CreateLoanRequest request)
    {
        if (!TryGetUserId(out var userId) || !TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.CreateAsync(userId, request with { TenantId = tenantId });
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(long id)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.DeleteAsync(tenantId, id);
        if (result.Success) return Ok(result);
        return result.Message == "Loan not found." ? NotFound(result) : BadRequest(result);
    }

    [HttpGet("payments")]
    [Authorize(Roles = "admin,manager,cashier")]
    public async Task<IActionResult> GetPayments([FromQuery] LoanPaymentFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetPaymentsAsync(tenantId, filters));
    }

    [HttpGet("payments/{paymentId:long}")]
    [Authorize(Roles = "admin,manager,cashier")]
    public async Task<IActionResult> GetPaymentById(long paymentId)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.GetPaymentByIdAsync(tenantId, paymentId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("{loanId:long}/payments")]
    [Authorize(Roles = "admin,manager,cashier")]
    public async Task<IActionResult> GetPaymentsByLoan(long loanId, [FromQuery] LoanPaymentFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        filters.LoanId = loanId;
        return Ok(await _service.GetPaymentsAsync(tenantId, filters));
    }

    [HttpPost("{loanId:long}/payments")]
    [Authorize(Roles = "admin,manager,cashier")]
    public async Task<IActionResult> CreatePayment(long loanId, [FromBody] CreateLoanPaymentRequest request)
    {
        if (!TryGetUserId(out var userId) || !TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.CreatePaymentAsync(userId, tenantId, loanId, request with { TenantId = tenantId });
        return result.Success ? CreatedAtAction(nameof(GetPaymentById), new { paymentId = result.Data!.LoanPaymentId }, result) : BadRequest(result);
    }

    [HttpDelete("payments/{paymentId:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeletePayment(long paymentId)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        var result = await _service.DeletePaymentAsync(tenantId, paymentId);
        if (result.Success) return Ok(result);
        return result.Message == "Loan payment not found." ? NotFound(result) : BadRequest(result);
    }

    private bool TryGetTenantId(out long tenantId) => long.TryParse(User.FindFirstValue("tenant_id"), out tenantId);
    private bool TryGetUserId(out long userId) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
