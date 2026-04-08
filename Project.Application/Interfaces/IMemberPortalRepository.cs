using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface IMemberPortalRepository
{
    Task<MemberPortalProfileDto?> GetProfileAsync(long userId);
    Task<MemberPortalDashboardDto?> GetDashboardAsync(long userId);
    Task<IReadOnlyCollection<MemberPortalSavingsAccountDto>> GetSavingsAccountsAsync(long userId);
    Task<IReadOnlyCollection<MemberPortalLoanDto>> GetLoansAsync(long userId);
    Task<(IEnumerable<LoanPaymentDto> Items, int TotalCount)> GetLoanPaymentsAsync(long userId, PaginationParams filters);
    Task<(IEnumerable<MemberPortalPurchaseDto> Items, int TotalCount)> GetPurchasesAsync(long userId, MemberPortalPurchaseFilterParams filters);
    Task<(IEnumerable<MemberPortalTransactionDto> Items, int TotalCount)> GetTransactionsAsync(long userId, MemberPortalTransactionFilterParams filters);
    Task<(IEnumerable<MemberLoanRequestDto> Items, int TotalCount)> GetLoanRequestsAsync(long userId, MemberLoanRequestFilterParams filters);
    Task<MemberLoanRequestDto?> GetLoanRequestByIdAsync(long userId, long memberLoanRequestId);
    Task<long> CreateLoanRequestAsync(long userId, CreateMemberLoanRequest request);
    Task<(IEnumerable<MemberLoanRequestDto> Items, int TotalCount)> GetLoanRequestsForApprovalAsync(long tenantId, MemberLoanRequestFilterParams filters);
    Task<MemberLoanRequestDto?> ApproveLoanRequestAsync(long approverUserId, long tenantId, long memberLoanRequestId, ApproveMemberLoanRequest request);
    Task<MemberLoanRequestDto?> RejectLoanRequestAsync(long approverUserId, long tenantId, long memberLoanRequestId, RejectMemberLoanRequest request);
    Task<(IEnumerable<SavingsWithdrawalRequestDto> Items, int TotalCount)> GetSavingsWithdrawalRequestsAsync(long userId, SavingsWithdrawalRequestFilterParams filters);
    Task<SavingsWithdrawalRequestDto?> GetSavingsWithdrawalRequestByIdAsync(long userId, long savingsWithdrawalRequestId);
    Task<long> CreateSavingsWithdrawalRequestAsync(long userId, CreateSavingsWithdrawalRequest request);
    Task<(IEnumerable<SavingsWithdrawalRequestDto> Items, int TotalCount)> GetSavingsWithdrawalRequestsForApprovalAsync(long tenantId, SavingsWithdrawalRequestFilterParams filters);
    Task<SavingsWithdrawalRequestDto?> ApproveSavingsWithdrawalRequestAsync(long approverUserId, long tenantId, long savingsWithdrawalRequestId, ApproveSavingsWithdrawalRequest request);
    Task<SavingsWithdrawalRequestDto?> RejectSavingsWithdrawalRequestAsync(long approverUserId, long tenantId, long savingsWithdrawalRequestId, RejectSavingsWithdrawalRequest request);
}
