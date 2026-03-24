using Microsoft.Extensions.Logging;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.Application.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ICategoryRepository categoryRepo, ILogger<CategoryService> logger)
    {
        _categoryRepo = categoryRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CategoryDto>>> GetAllAsync(int currentUserId, CategoryFilterParams filters)
    {
        var categories = await _categoryRepo.GetAllAsync(currentUserId, filters);
        var dtos = categories.Select(MapToDto);
        return ApiResponse<IEnumerable<CategoryDto>>.Ok(dtos, "Categories retrieved successfully.");
    }

    public async Task<ApiResponse<PagedResult<CategoryDto>>> GetPagedAsync(int currentUserId, CategoryPagedFilterParams filters)
    {
        var (items, totalCount) = await _categoryRepo.GetPagedAsync(currentUserId, filters);
        var dtos = items.Select(MapToDto);
        var result = PagedResult<CategoryDto>.Create(dtos, totalCount, filters);
        return ApiResponse<PagedResult<CategoryDto>>.Ok(result, "Categories retrieved successfully.");
    }

    public async Task<ApiResponse<CategoryDto>> GetByIdAsync(int id, int currentUserId)
    {
        var category = await _categoryRepo.GetByIdAsync(id, currentUserId);
        if (category == null) return ApiResponse<CategoryDto>.Fail("Category not found.");

        return ApiResponse<CategoryDto>.Ok(MapToDto(category), "Category retrieved successfully.");
    }

    public async Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto request, int currentUserId)
    {
        var validationError = ValidateRequest(request.Visibility, request.DepartmentId);
        if (validationError != null) return ApiResponse<CategoryDto>.Fail(validationError);

        var entity = new Category
        {
            CategoryName = request.CategoryName.Trim(),
            Visibility = request.Visibility,
            DepartmentId = request.Visibility == CategoryVisibility.Department ? request.DepartmentId : null,
            OwnerUserId = request.Visibility == CategoryVisibility.Private ? currentUserId : null,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _categoryRepo.CreateAsync(entity);
        var loaded = await _categoryRepo.GetByIdAsync(created.CategoryId, currentUserId) ?? created;
        return ApiResponse<CategoryDto>.Ok(MapToDto(loaded), "Category created successfully.");
    }

    public async Task<ApiResponse<CategoryDto>> UpdateAsync(int id, UpdateCategoryDto request, int currentUserId)
    {
        var category = await _categoryRepo.GetByIdAsync(id, currentUserId);
        if (category == null) return ApiResponse<CategoryDto>.Fail("Category not found.");

        if (category.Visibility == CategoryVisibility.Private && category.OwnerUserId != currentUserId)
            return ApiResponse<CategoryDto>.Fail("Only the owner can update a private category.");

        var validationError = ValidateRequest(request.Visibility, request.DepartmentId);
        if (validationError != null) return ApiResponse<CategoryDto>.Fail(validationError);

        category.CategoryName = request.CategoryName.Trim();
        category.Visibility = request.Visibility;
        category.DepartmentId = request.Visibility == CategoryVisibility.Department ? request.DepartmentId : null;
        category.OwnerUserId = request.Visibility == CategoryVisibility.Private ? currentUserId : null;
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        var updated = await _categoryRepo.UpdateAsync(category);
        if (!updated) return ApiResponse<CategoryDto>.Fail("Failed to update category.");

        var loaded = await _categoryRepo.GetByIdAsync(id, currentUserId) ?? category;
        return ApiResponse<CategoryDto>.Ok(MapToDto(loaded), "Category updated successfully.");
    }

    public async Task<ApiResponse> DeleteAsync(int id, int currentUserId)
    {
        var category = await _categoryRepo.GetByIdAsync(id, currentUserId);
        if (category == null) return ApiResponse.Fail("Category not found.");

        if (category.Visibility == CategoryVisibility.Private && category.OwnerUserId != currentUserId)
            return ApiResponse.Fail("Only the owner can delete a private category.");

        var deleted = await _categoryRepo.DeleteAsync(id);
        if (!deleted) return ApiResponse.Fail("Category not found.");

        return ApiResponse.Ok("Category deleted successfully.");
    }

    private static string? ValidateRequest(string visibility, int? departmentId)
    {
        if (!CategoryVisibility.All.Contains(visibility))
            return "Invalid category visibility.";

        if (visibility == CategoryVisibility.Department && !departmentId.HasValue)
            return "Department category requires a department.";

        if (visibility != CategoryVisibility.Department && departmentId.HasValue)
            return "Department can only be set for department categories.";

        return null;
    }

    private static CategoryDto MapToDto(Category c) => new(
        c.CategoryId,
        c.CategoryName,
        c.Visibility,
        c.DepartmentId,
        c.DepartmentName,
        c.OwnerUserId,
        c.IsActive,
        c.CreatedAt,
        c.UpdatedAt
    );
}
