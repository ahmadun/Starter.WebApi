using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface IRequestApprovalService
{
    Task<ApiResponse<PagedResult<MemberLoanRequestDto>>> GetLoanRequestsAsync(long tenantId, MemberLoanRequestFilterParams filters);
    Task<ApiResponse<MemberLoanRequestDto>> ApproveLoanRequestAsync(long approverUserId, long tenantId, long memberLoanRequestId, ApproveMemberLoanRequest request);
    Task<ApiResponse<MemberLoanRequestDto>> RejectLoanRequestAsync(long approverUserId, long tenantId, long memberLoanRequestId, RejectMemberLoanRequest request);
    Task<ApiResponse<PagedResult<SavingsWithdrawalRequestDto>>> GetSavingsWithdrawalRequestsAsync(long tenantId, SavingsWithdrawalRequestFilterParams filters);
    Task<ApiResponse<SavingsWithdrawalRequestDto>> ApproveSavingsWithdrawalRequestAsync(long approverUserId, long tenantId, long savingsWithdrawalRequestId, ApproveSavingsWithdrawalRequest request);
    Task<ApiResponse<SavingsWithdrawalRequestDto>> RejectSavingsWithdrawalRequestAsync(long approverUserId, long tenantId, long savingsWithdrawalRequestId, RejectSavingsWithdrawalRequest request);
}
