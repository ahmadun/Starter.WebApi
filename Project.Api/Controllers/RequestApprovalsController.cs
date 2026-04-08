using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/request-approvals")]
[Produces("application/json")]
[Authorize(Roles = "admin,manager")]
public sealed class RequestApprovalsController : ControllerBase
{
    private readonly IRequestApprovalService _service;

    public RequestApprovalsController(IRequestApprovalService service)
    {
        _service = service;
    }

    [HttpGet("loans")]
    public async Task<IActionResult> GetLoanRequests([FromQuery] MemberLoanRequestFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetLoanRequestsAsync(tenantId, filters));
    }

    [HttpPost("loans/{memberLoanRequestId:long}/approve")]
    public async Task<IActionResult> ApproveLoanRequest(long memberLoanRequestId, [FromBody] ApproveMemberLoanRequest request)
    {
        if (!TryGetTenantId(out var tenantId) || !TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.ApproveLoanRequestAsync(userId, tenantId, memberLoanRequestId, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpPost("loans/{memberLoanRequestId:long}/reject")]
    public async Task<IActionResult> RejectLoanRequest(long memberLoanRequestId, [FromBody] RejectMemberLoanRequest request)
    {
        if (!TryGetTenantId(out var tenantId) || !TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.RejectLoanRequestAsync(userId, tenantId, memberLoanRequestId, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpGet("withdrawals")]
    public async Task<IActionResult> GetSavingsWithdrawalRequests([FromQuery] SavingsWithdrawalRequestFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId)) return Unauthorized();
        return Ok(await _service.GetSavingsWithdrawalRequestsAsync(tenantId, filters));
    }

    [HttpPost("withdrawals/{savingsWithdrawalRequestId:long}/approve")]
    public async Task<IActionResult> ApproveSavingsWithdrawalRequest(long savingsWithdrawalRequestId, [FromBody] ApproveSavingsWithdrawalRequest request)
    {
        if (!TryGetTenantId(out var tenantId) || !TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.ApproveSavingsWithdrawalRequestAsync(userId, tenantId, savingsWithdrawalRequestId, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpPost("withdrawals/{savingsWithdrawalRequestId:long}/reject")]
    public async Task<IActionResult> RejectSavingsWithdrawalRequest(long savingsWithdrawalRequestId, [FromBody] RejectSavingsWithdrawalRequest request)
    {
        if (!TryGetTenantId(out var tenantId) || !TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.RejectSavingsWithdrawalRequestAsync(userId, tenantId, savingsWithdrawalRequestId, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    private bool TryGetTenantId(out long tenantId) => long.TryParse(User.FindFirstValue("tenant_id"), out tenantId);
    private bool TryGetUserId(out long userId) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
