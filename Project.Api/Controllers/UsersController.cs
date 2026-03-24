using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "SuperAdmin,Admin")]  
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }


    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] UserFilterParams filters)
    {
        var result = await _userService.GetAllAsync(filters);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("options")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserOptionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOptions([FromQuery] UserOptionFilterParams filters)
    {
        var result = await _userService.GetOptionsAsync(filters);
        return Ok(result);
    }


    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateAsync(request);

        if (!result.Success)
        {
            if (result.Message.Contains("already in use") ||
                result.Message.Contains("already registered"))
                return Conflict(result);

            return BadRequest(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Data!.UserId },
            result);
    }



    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var result = await _userService.UpdateAsync(id, request);

        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            if (result.Message.Contains("already in use") ||
                result.Message.Contains("already registered")) return Conflict(result);
            return BadRequest(result);
        }

        return Ok(result);
    }


    [HttpPost("{id:int}/reset-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        var result = await _userService.ResetPasswordAsync(id, request);

        return result.Success
            ? Ok(result)
            : result.Message.Contains("not found") ? NotFound(result) : BadRequest(result);
    }


    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
