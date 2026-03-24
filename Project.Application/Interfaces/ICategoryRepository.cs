using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync(int currentUserId, CategoryFilterParams filters);
    Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedAsync(int currentUserId, CategoryPagedFilterParams filters);
    Task<Category?> GetByIdAsync(int id, int currentUserId);
    Task<Category> CreateAsync(Category category);
    Task<bool> UpdateAsync(Category category);
    Task<bool> DeleteAsync(int id);
}
