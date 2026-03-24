using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/Users/me")]
[Produces("application/json")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<MyProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MyProfileDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MyProfileDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _userService.GetMyProfileAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<MyProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MyProfileDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MyProfileDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MyProfileDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MyProfileDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromBody] UpdateMyProfileRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        var result = await _userService.UpdateMyProfileAsync(userId, request);
        if (!result.Success)
        {
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(result);
            if (result.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
                return Conflict(result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    private bool TryGetUserId(out int userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out userId);
    }
}
