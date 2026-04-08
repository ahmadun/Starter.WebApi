using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface ILoanProductService
{
    Task<ApiResponse<PagedResult<LoanProductDto>>> GetAllAsync(long tenantId, LoanProductFilterParams filters);
    Task<ApiResponse<LoanProductDto>> GetByIdAsync(long tenantId, long loanProductId);
    Task<ApiResponse<LoanProductDto>> CreateAsync(SaveLoanProductRequest request);
    Task<ApiResponse<LoanProductDto>> UpdateAsync(long tenantId, long loanProductId, SaveLoanProductRequest request);
}
