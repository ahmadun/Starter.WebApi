using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize(Roles = "admin")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] UserFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        return Ok(await _userService.GetAllAsync(tenantId, filters));
    }

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions([FromQuery] UserOptionFilterParams filters)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        return Ok(await _userService.GetOptionsAsync(tenantId, filters));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _userService.GetByIdAsync(tenantId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInternalUserRequest request)
    {
        var result = await _userService.CreateAsync(request);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.UserId }, result) : BadRequest(result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserRequest request)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _userService.UpdateAsync(tenantId, id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpPost("{id:long}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetPasswordRequest request)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _userService.ResetPasswordAsync(tenantId, id, request);
        return result.Success ? Ok(result) : result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(result) : BadRequest(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _userService.DeleteAsync(tenantId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    private bool TryGetTenantId(out long tenantId) => long.TryParse(User.FindFirstValue("tenant_id"), out tenantId);
}
