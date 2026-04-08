using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface IMemberPortalService
{
    Task<ApiResponse<MemberPortalProfileDto>> GetProfileAsync(long userId);
    Task<ApiResponse<MemberPortalDashboardDto>> GetDashboardAsync(long userId);
    Task<ApiResponse<IReadOnlyCollection<MemberPortalSavingsAccountDto>>> GetSavingsAccountsAsync(long userId);
    Task<ApiResponse<IReadOnlyCollection<MemberPortalLoanDto>>> GetLoansAsync(long userId);
    Task<ApiResponse<IReadOnlyCollection<LoanProductDto>>> GetLoanProductsAsync(long userId);
    Task<ApiResponse<IReadOnlyCollection<SavingsProductDto>>> GetSavingsProductsAsync(long userId);
    Task<ApiResponse<PagedResult<LoanPaymentDto>>> GetLoanPaymentsAsync(long userId, PaginationParams filters);
    Task<ApiResponse<PagedResult<MemberPortalPurchaseDto>>> GetPurchasesAsync(long userId, MemberPortalPurchaseFilterParams filters);
    Task<ApiResponse<PagedResult<MemberPortalTransactionDto>>> GetTransactionsAsync(long userId, MemberPortalTransactionFilterParams filters);
    Task<ApiResponse<PagedResult<MemberLoanRequestDto>>> GetLoanRequestsAsync(long userId, MemberLoanRequestFilterParams filters);
    Task<ApiResponse<MemberLoanRequestDto>> CreateLoanRequestAsync(long userId, CreateMemberLoanRequest request);
    Task<ApiResponse<PagedResult<SavingsWithdrawalRequestDto>>> GetSavingsWithdrawalRequestsAsync(long userId, SavingsWithdrawalRequestFilterParams filters);
    Task<ApiResponse<SavingsWithdrawalRequestDto>> CreateSavingsWithdrawalRequestAsync(long userId, CreateSavingsWithdrawalRequest request);
}
