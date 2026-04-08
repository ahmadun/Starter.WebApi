using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using FluentValidation;

namespace Project.Application.Services;

public sealed class MemberPortalService : IMemberPortalService
{
    private readonly IMemberPortalRepository _repository;
    private readonly ILoanProductRepository _loanProductRepository;
    private readonly ISavingsProductRepository _savingsProductRepository;
    private readonly ISavingsTransactionRepository _savingsTransactionRepository;
    private readonly IValidator<CreateMemberLoanRequest> _loanRequestValidator;
    private readonly IValidator<CreateSavingsWithdrawalRequest> _withdrawalRequestValidator;

    public MemberPortalService(
        IMemberPortalRepository repository,
        ILoanProductRepository loanProductRepository,
        ISavingsProductRepository savingsProductRepository,
        ISavingsTransactionRepository savingsTransactionRepository,
        IValidator<CreateMemberLoanRequest> loanRequestValidator,
        IValidator<CreateSavingsWithdrawalRequest> withdrawalRequestValidator)
    {
        _repository = repository;
        _loanProductRepository = loanProductRepository;
        _savingsProductRepository = savingsProductRepository;
        _savingsTransactionRepository = savingsTransactionRepository;
        _loanRequestValidator = loanRequestValidator;
        _withdrawalRequestValidator = withdrawalRequestValidator;
    }

    public async Task<ApiResponse<MemberPortalProfileDto>> GetProfileAsync(long userId)
    {
        var profile = await _repository.GetProfileAsync(userId);
        return profile is null
            ? ApiResponse<MemberPortalProfileDto>.NotFound("Member portal profile not found.")
            : ApiResponse<MemberPortalProfileDto>.Ok(profile);
    }

    public async Task<ApiResponse<MemberPortalDashboardDto>> GetDashboardAsync(long userId)
    {
        var dashboard = await _repository.GetDashboardAsync(userId);
        return dashboard is null
            ? ApiResponse<MemberPortalDashboardDto>.NotFound("Member portal dashboard not found.")
            : ApiResponse<MemberPortalDashboardDto>.Ok(dashboard);
    }

    public async Task<ApiResponse<IReadOnlyCollection<MemberPortalSavingsAccountDto>>> GetSavingsAccountsAsync(long userId)
    {
        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<IReadOnlyCollection<MemberPortalSavingsAccountDto>>.NotFound("Member portal profile not found.");

        var items = await _repository.GetSavingsAccountsAsync(userId);
        return ApiResponse<IReadOnlyCollection<MemberPortalSavingsAccountDto>>.Ok(items);
    }

    public async Task<ApiResponse<IReadOnlyCollection<MemberPortalLoanDto>>> GetLoansAsync(long userId)
    {
        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<IReadOnlyCollection<MemberPortalLoanDto>>.NotFound("Member portal profile not found.");

        var items = await _repository.GetLoansAsync(userId);
        return ApiResponse<IReadOnlyCollection<MemberPortalLoanDto>>.Ok(items);
    }

    public async Task<ApiResponse<IReadOnlyCollection<LoanProductDto>>> GetLoanProductsAsync(long userId)
    {
        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<IReadOnlyCollection<LoanProductDto>>.NotFound("Member portal profile not found.");

        var filters = new LoanProductFilterParams { Page = 1, PageSize = 1000, Search = null };
        var (items, _) = await _loanProductRepository.GetAllAsync(profile.TenantId, filters);
        var result = items.Where(x => x.IsActive)
            .OrderBy(x => x.ProductName)
            .Select(x => new LoanProductDto(
                x.LoanProductId,
                x.TenantId,
                x.ProductCode,
                x.ProductName,
                x.DefaultFlatInterestRatePct,
                x.MinFlatInterestRatePct,
                x.MaxFlatInterestRatePct,
                x.DefaultTermMonths,
                x.MinTermMonths,
                x.MaxTermMonths,
                x.MinPrincipalAmount,
                x.MaxPrincipalAmount,
                x.DefaultAdminFeeAmount,
                x.DefaultPenaltyAmount,
                x.IsActive))
            .ToArray();

        return ApiResponse<IReadOnlyCollection<LoanProductDto>>.Ok(result);
    }

    public async Task<ApiResponse<IReadOnlyCollection<SavingsProductDto>>> GetSavingsProductsAsync(long userId)
    {
        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<IReadOnlyCollection<SavingsProductDto>>.NotFound("Member portal profile not found.");

        var filters = new SavingsProductFilterParams { Page = 1, PageSize = 1000, Search = null };
        var (items, _) = await _savingsProductRepository.GetAllAsync(profile.TenantId, filters);
        var result = items.Where(x => x.IsActive)
            .OrderBy(x => x.ProductName)
            .Select(x => new SavingsProductDto(
                x.SavingsProductId,
                x.TenantId,
                x.ProductCode,
                x.ProductName,
                x.SavingsKind,
                x.Periodicity,
                x.DefaultAmount,
                x.IsActive))
            .ToArray();

        return ApiResponse<IReadOnlyCollection<SavingsProductDto>>.Ok(result);
    }

    public async Task<ApiResponse<PagedResult<LoanPaymentDto>>> GetLoanPaymentsAsync(long userId, PaginationParams filters)
    {
        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<PagedResult<LoanPaymentDto>>.NotFound("Member portal profile not found.");

        var (items, totalCount) = await _repository.GetLoanPaymentsAsync(userId, filters);
        return ApiResponse<PagedResult<LoanPaymentDto>>.Ok(PagedResult<LoanPaymentDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<PagedResult<MemberPortalPurchaseDto>>> GetPurchasesAsync(long userId, MemberPortalPurchaseFilterParams filters)
    {
        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<PagedResult<MemberPortalPurchaseDto>>.NotFound("Member portal profile not found.");

        var (items, totalCount) = await _repository.GetPurchasesAsync(userId, filters);
        return ApiResponse<PagedResult<MemberPortalPurchaseDto>>.Ok(PagedResult<MemberPortalPurchaseDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<PagedResult<MemberPortalTransactionDto>>> GetTransactionsAsync(long userId, MemberPortalTransactionFilterParams filters)
    {
        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<PagedResult<MemberPortalTransactionDto>>.NotFound("Member portal profile not found.");

        var (items, totalCount) = await _repository.GetTransactionsAsync(userId, filters);
        return ApiResponse<PagedResult<MemberPortalTransactionDto>>.Ok(PagedResult<MemberPortalTransactionDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<PagedResult<MemberLoanRequestDto>>> GetLoanRequestsAsync(long userId, MemberLoanRequestFilterParams filters)
    {
        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<PagedResult<MemberLoanRequestDto>>.NotFound("Member portal profile not found.");

        var (items, totalCount) = await _repository.GetLoanRequestsAsync(userId, filters);
        return ApiResponse<PagedResult<MemberLoanRequestDto>>.Ok(PagedResult<MemberLoanRequestDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<MemberLoanRequestDto>> CreateLoanRequestAsync(long userId, CreateMemberLoanRequest request)
    {
        var validation = await _loanRequestValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<MemberLoanRequestDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<MemberLoanRequestDto>.NotFound("Member portal profile not found.");

        if (!string.Equals(profile.MemberStatus, "active", StringComparison.OrdinalIgnoreCase))
            return ApiResponse<MemberLoanRequestDto>.Fail("Only active members can submit loan requests.");

        var product = await _loanProductRepository.GetByIdAsync(profile.TenantId, request.LoanProductId);
        if (product is null)
            return ApiResponse<MemberLoanRequestDto>.Fail("Loan product was not found.");

        if (!product.IsActive)
            return ApiResponse<MemberLoanRequestDto>.Fail("Loan product is inactive.");

        if (product.MinPrincipalAmount.HasValue && request.PrincipalAmount < product.MinPrincipalAmount.Value)
            return ApiResponse<MemberLoanRequestDto>.Fail($"Minimum loan principal is {product.MinPrincipalAmount.Value:N2}.");

        if (product.MaxPrincipalAmount.HasValue && request.PrincipalAmount > product.MaxPrincipalAmount.Value)
            return ApiResponse<MemberLoanRequestDto>.Fail($"Maximum loan principal is {product.MaxPrincipalAmount.Value:N2}.");

        var proposedTerm = request.ProposedTermMonths ?? product.DefaultTermMonths;
        if (product.MinTermMonths.HasValue && proposedTerm < product.MinTermMonths.Value)
            return ApiResponse<MemberLoanRequestDto>.Fail($"Minimum loan term is {product.MinTermMonths.Value} months.");

        if (product.MaxTermMonths.HasValue && proposedTerm > product.MaxTermMonths.Value)
            return ApiResponse<MemberLoanRequestDto>.Fail($"Maximum loan term is {product.MaxTermMonths.Value} months.");

        var requestId = await _repository.CreateLoanRequestAsync(userId, request with { ProposedTermMonths = proposedTerm });
        var created = await _repository.GetLoanRequestByIdAsync(userId, requestId);
        return created is null
            ? ApiResponse<MemberLoanRequestDto>.Fail("Loan request was created but could not be reloaded.")
            : ApiResponse<MemberLoanRequestDto>.Created(created);
    }

    public async Task<ApiResponse<PagedResult<SavingsWithdrawalRequestDto>>> GetSavingsWithdrawalRequestsAsync(long userId, SavingsWithdrawalRequestFilterParams filters)
    {
        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<PagedResult<SavingsWithdrawalRequestDto>>.NotFound("Member portal profile not found.");

        var (items, totalCount) = await _repository.GetSavingsWithdrawalRequestsAsync(userId, filters);
        return ApiResponse<PagedResult<SavingsWithdrawalRequestDto>>.Ok(PagedResult<SavingsWithdrawalRequestDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<SavingsWithdrawalRequestDto>> CreateSavingsWithdrawalRequestAsync(long userId, CreateSavingsWithdrawalRequest request)
    {
        var validation = await _withdrawalRequestValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<SavingsWithdrawalRequestDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var profile = await _repository.GetProfileAsync(userId);
        if (profile is null)
            return ApiResponse<SavingsWithdrawalRequestDto>.NotFound("Member portal profile not found.");

        if (!string.Equals(profile.MemberStatus, "active", StringComparison.OrdinalIgnoreCase))
            return ApiResponse<SavingsWithdrawalRequestDto>.Fail("Only active members can submit withdrawal requests.");

        var product = await _savingsProductRepository.GetByIdAsync(profile.TenantId, request.SavingsProductId);
        if (product is null)
            return ApiResponse<SavingsWithdrawalRequestDto>.Fail("Savings product was not found.");

        if (!product.IsActive)
            return ApiResponse<SavingsWithdrawalRequestDto>.Fail("Savings product is inactive.");

        var savingsContext = await _savingsTransactionRepository.GetContextAsync(profile.TenantId, profile.MemberId, request.SavingsProductId);
        if (savingsContext?.MemberSavingsAccountId is null)
            return ApiResponse<SavingsWithdrawalRequestDto>.Fail("Savings account for this product was not found.");

        var balance = await _savingsTransactionRepository.GetBalanceAsync(savingsContext.MemberSavingsAccountId.Value);
        if (balance < request.Amount)
            return ApiResponse<SavingsWithdrawalRequestDto>.Fail("Insufficient savings balance for this withdrawal request.");

        var requestId = await _repository.CreateSavingsWithdrawalRequestAsync(userId, request);
        var created = await _repository.GetSavingsWithdrawalRequestByIdAsync(userId, requestId);
        return created is null
            ? ApiResponse<SavingsWithdrawalRequestDto>.Fail("Withdrawal request was created but could not be reloaded.")
            : ApiResponse<SavingsWithdrawalRequestDto>.Created(created);
    }
}
