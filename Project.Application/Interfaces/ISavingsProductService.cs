using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface ISavingsProductService
{
    Task<ApiResponse<PagedResult<SavingsProductDto>>> GetAllAsync(long tenantId, SavingsProductFilterParams filters);
    Task<ApiResponse<SavingsProductDto>> GetByIdAsync(long tenantId, long savingsProductId);
    Task<ApiResponse<SavingsProductDto>> CreateAsync(SaveSavingsProductRequest request);
    Task<ApiResponse<SavingsProductDto>> UpdateAsync(long tenantId, long savingsProductId, SaveSavingsProductRequest request);
}
