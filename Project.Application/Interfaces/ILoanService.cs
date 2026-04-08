using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface ILoanService
{
    Task<ApiResponse<PagedResult<LoanDto>>> GetAllAsync(long tenantId, LoanFilterParams filters);
    Task<ApiResponse<LoanDto>> GetByIdAsync(long tenantId, long loanId);
    Task<ApiResponse<LoanDto>> CreateAsync(long userId, CreateLoanRequest request);
    Task<ApiResponse<object>> DeleteAsync(long tenantId, long loanId);
    Task<ApiResponse<PagedResult<LoanPaymentDto>>> GetPaymentsAsync(long tenantId, LoanPaymentFilterParams filters);
    Task<ApiResponse<LoanPaymentDto>> GetPaymentByIdAsync(long tenantId, long loanPaymentId);
    Task<ApiResponse<LoanPaymentDto>> CreatePaymentAsync(long userId, long tenantId, long loanId, CreateLoanPaymentRequest request);
    Task<ApiResponse<object>> DeletePaymentAsync(long tenantId, long loanPaymentId);
}
