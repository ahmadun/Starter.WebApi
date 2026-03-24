using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using System.Security.Claims;

namespace Project.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] CategoryFilterParams filters)
    {
        var result = await _categoryService.GetAllAsync(CurrentUserId, filters);
        return Ok(result);
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] CategoryPagedFilterParams filters)
    {
        var result = await _categoryService.GetPagedAsync(CurrentUserId, filters);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _categoryService.GetByIdAsync(id, CurrentUserId);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto request)
    {
        var result = await _categoryService.CreateAsync(request, CurrentUserId);
        if (!result.Success) return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.CategoryId }, result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto request)
    {
        var result = await _categoryService.UpdateAsync(id, request, CurrentUserId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categoryService.DeleteAsync(id, CurrentUserId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
