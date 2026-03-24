using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface ICategoryService
{
    Task<ApiResponse<IEnumerable<CategoryDto>>> GetAllAsync(int currentUserId, CategoryFilterParams filters);
    Task<ApiResponse<PagedResult<CategoryDto>>> GetPagedAsync(int currentUserId, CategoryPagedFilterParams filters);
    Task<ApiResponse<CategoryDto>> GetByIdAsync(int id, int currentUserId);
    Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto request, int currentUserId);
    Task<ApiResponse<CategoryDto>> UpdateAsync(int id, UpdateCategoryDto request, int currentUserId);
    Task<ApiResponse> DeleteAsync(int id, int currentUserId);
}
