using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Application.Services;

public sealed class RequestApprovalService : IRequestApprovalService
{
    private readonly IMemberPortalRepository _repository;
    private readonly IValidator<ApproveMemberLoanRequest> _approveLoanValidator;
    private readonly IValidator<RejectMemberLoanRequest> _rejectLoanValidator;
    private readonly IValidator<ApproveSavingsWithdrawalRequest> _approveWithdrawalValidator;
    private readonly IValidator<RejectSavingsWithdrawalRequest> _rejectWithdrawalValidator;

    public RequestApprovalService(
        IMemberPortalRepository repository,
        IValidator<ApproveMemberLoanRequest> approveLoanValidator,
        IValidator<RejectMemberLoanRequest> rejectLoanValidator,
        IValidator<ApproveSavingsWithdrawalRequest> approveWithdrawalValidator,
        IValidator<RejectSavingsWithdrawalRequest> rejectWithdrawalValidator)
    {
        _repository = repository;
        _approveLoanValidator = approveLoanValidator;
        _rejectLoanValidator = rejectLoanValidator;
        _approveWithdrawalValidator = approveWithdrawalValidator;
        _rejectWithdrawalValidator = rejectWithdrawalValidator;
    }

    public async Task<ApiResponse<PagedResult<MemberLoanRequestDto>>> GetLoanRequestsAsync(long tenantId, MemberLoanRequestFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetLoanRequestsForApprovalAsync(tenantId, filters);
        return ApiResponse<PagedResult<MemberLoanRequestDto>>.Ok(PagedResult<MemberLoanRequestDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<MemberLoanRequestDto>> ApproveLoanRequestAsync(long approverUserId, long tenantId, long memberLoanRequestId, ApproveMemberLoanRequest request)
    {
        var validation = await _approveLoanValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<MemberLoanRequestDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        try
        {
            var result = await _repository.ApproveLoanRequestAsync(approverUserId, tenantId, memberLoanRequestId, request);
            return result is null
                ? ApiResponse<MemberLoanRequestDto>.NotFound("Loan request not found.")
                : ApiResponse<MemberLoanRequestDto>.Ok(result, "Loan request approved successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<MemberLoanRequestDto>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse<MemberLoanRequestDto>> RejectLoanRequestAsync(long approverUserId, long tenantId, long memberLoanRequestId, RejectMemberLoanRequest request)
    {
        var validation = await _rejectLoanValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<MemberLoanRequestDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        try
        {
            var result = await _repository.RejectLoanRequestAsync(approverUserId, tenantId, memberLoanRequestId, request);
            return result is null
                ? ApiResponse<MemberLoanRequestDto>.NotFound("Loan request not found.")
                : ApiResponse<MemberLoanRequestDto>.Ok(result, "Loan request rejected successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<MemberLoanRequestDto>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse<PagedResult<SavingsWithdrawalRequestDto>>> GetSavingsWithdrawalRequestsAsync(long tenantId, SavingsWithdrawalRequestFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetSavingsWithdrawalRequestsForApprovalAsync(tenantId, filters);
        return ApiResponse<PagedResult<SavingsWithdrawalRequestDto>>.Ok(PagedResult<SavingsWithdrawalRequestDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<SavingsWithdrawalRequestDto>> ApproveSavingsWithdrawalRequestAsync(long approverUserId, long tenantId, long savingsWithdrawalRequestId, ApproveSavingsWithdrawalRequest request)
    {
        var validation = await _approveWithdrawalValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<SavingsWithdrawalRequestDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        try
        {
            var result = await _repository.ApproveSavingsWithdrawalRequestAsync(approverUserId, tenantId, savingsWithdrawalRequestId, request);
            return result is null
                ? ApiResponse<SavingsWithdrawalRequestDto>.NotFound("Savings withdrawal request not found.")
                : ApiResponse<SavingsWithdrawalRequestDto>.Ok(result, "Savings withdrawal request approved successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<SavingsWithdrawalRequestDto>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse<SavingsWithdrawalRequestDto>> RejectSavingsWithdrawalRequestAsync(long approverUserId, long tenantId, long savingsWithdrawalRequestId, RejectSavingsWithdrawalRequest request)
    {
        var validation = await _rejectWithdrawalValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<SavingsWithdrawalRequestDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        try
        {
            var result = await _repository.RejectSavingsWithdrawalRequestAsync(approverUserId, tenantId, savingsWithdrawalRequestId, request);
            return result is null
                ? ApiResponse<SavingsWithdrawalRequestDto>.NotFound("Savings withdrawal request not found.")
                : ApiResponse<SavingsWithdrawalRequestDto>.Ok(result, "Savings withdrawal request rejected successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<SavingsWithdrawalRequestDto>.Fail(ex.Message);
        }
    }
}
