using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface ISavingsTransactionService
{
    Task<ApiResponse<PagedResult<SavingsAccountDto>>> GetAccountsAsync(long tenantId, SavingsAccountFilterParams filters);
    Task<ApiResponse<PagedResult<SavingsTransactionDto>>> GetTransactionsAsync(long tenantId, SavingsTransactionFilterParams filters);
    Task<ApiResponse<SavingsTransactionDto>> GetByIdAsync(long tenantId, long savingsTransactionId);
    Task<ApiResponse<SavingsTransactionDto>> CreateAsync(long userId, CreateSavingsTransactionRequest request);
    Task<ApiResponse<object>> DeleteAsync(long tenantId, long savingsTransactionId);
}
