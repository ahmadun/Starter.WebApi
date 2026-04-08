using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/member-portal")]
[Produces("application/json")]
[Authorize(Roles = "member")]
public sealed class MemberPortalController : ControllerBase
{
    private readonly IMemberPortalService _service;

    public MemberPortalController(IMemberPortalService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.GetProfileAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.GetDashboardAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("savings")]
    public async Task<IActionResult> GetSavingsAccounts()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.GetSavingsAccountsAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("savings-products")]
    public async Task<IActionResult> GetSavingsProducts()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.GetSavingsProductsAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("loans")]
    public async Task<IActionResult> GetLoans()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.GetLoansAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("loan-products")]
    public async Task<IActionResult> GetLoanProducts()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.GetLoanProductsAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("loan-payments")]
    public async Task<IActionResult> GetLoanPayments([FromQuery] PaginationParams filters)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.GetLoanPaymentsAsync(userId, filters);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("purchases")]
    public async Task<IActionResult> GetPurchases([FromQuery] MemberPortalPurchaseFilterParams filters)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.GetPurchasesAsync(userId, filters);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] MemberPortalTransactionFilterParams filters)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.GetTransactionsAsync(userId, filters);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("requests/loans")]
    public async Task<IActionResult> GetLoanRequests([FromQuery] MemberLoanRequestFilterParams filters)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.GetLoanRequestsAsync(userId, filters);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("requests/loans")]
    public async Task<IActionResult> CreateLoanRequest([FromBody] CreateMemberLoanRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.CreateLoanRequestAsync(userId, request);
        return result.Success ? StatusCode(StatusCodes.Status201Created, result) : BadRequest(result);
    }

    [HttpGet("requests/withdrawals")]
    public async Task<IActionResult> GetSavingsWithdrawalRequests([FromQuery] SavingsWithdrawalRequestFilterParams filters)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.GetSavingsWithdrawalRequestsAsync(userId, filters);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("requests/withdrawals")]
    public async Task<IActionResult> CreateSavingsWithdrawalRequest([FromBody] CreateSavingsWithdrawalRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _service.CreateSavingsWithdrawalRequestAsync(userId, request);
        return result.Success ? StatusCode(StatusCodes.Status201Created, result) : BadRequest(result);
    }

    private bool TryGetUserId(out long userId)
        => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
