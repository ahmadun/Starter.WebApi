using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Application.Services;

public sealed class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly ILoanProductRepository _loanProductRepository;
    private readonly IValidator<CreateLoanRequest> _validator;
    private readonly IValidator<CreateLoanPaymentRequest> _paymentValidator;

    public LoanService(
        ILoanRepository loanRepository,
        ILoanProductRepository loanProductRepository,
        IValidator<CreateLoanRequest> validator,
        IValidator<CreateLoanPaymentRequest> paymentValidator)
    {
        _loanRepository = loanRepository;
        _loanProductRepository = loanProductRepository;
        _validator = validator;
        _paymentValidator = paymentValidator;
    }

    public async Task<ApiResponse<PagedResult<LoanDto>>> GetAllAsync(long tenantId, LoanFilterParams filters)
    {
        var (items, totalCount) = await _loanRepository.GetAllAsync(tenantId, filters);
        var mapped = new List<LoanDto>();
        foreach (var item in items)
        {
            mapped.Add(new LoanDto(item.LoanId, item.TenantId, item.MemberId, item.LoanProductId, item.LoanNo, item.LoanDate, item.PrincipalAmount, item.FlatInterestRatePct, item.TermMonths, item.AdminFeeAmount, item.PenaltyAmount, item.InstallmentAmount, item.TotalInterestAmount, item.TotalPayableAmount, item.OutstandingPrincipalAmount, item.OutstandingTotalAmount, item.Status, item.Note, await _loanRepository.GetInstallmentsAsync(item.LoanId)));
        }

        return ApiResponse<PagedResult<LoanDto>>.Ok(PagedResult<LoanDto>.Create(mapped, totalCount, filters));
    }

    public async Task<ApiResponse<LoanDto>> GetByIdAsync(long tenantId, long loanId)
    {
        var item = await _loanRepository.GetByIdAsync(tenantId, loanId);
        if (item is null)
            return ApiResponse<LoanDto>.NotFound("Loan not found.");

        return ApiResponse<LoanDto>.Ok(new LoanDto(item.LoanId, item.TenantId, item.MemberId, item.LoanProductId, item.LoanNo, item.LoanDate, item.PrincipalAmount, item.FlatInterestRatePct, item.TermMonths, item.AdminFeeAmount, item.PenaltyAmount, item.InstallmentAmount, item.TotalInterestAmount, item.TotalPayableAmount, item.OutstandingPrincipalAmount, item.OutstandingTotalAmount, item.Status, item.Note, await _loanRepository.GetInstallmentsAsync(item.LoanId)));
    }

    public async Task<ApiResponse<LoanDto>> CreateAsync(long userId, CreateLoanRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<LoanDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _loanRepository.LoanNoExistsAsync(request.TenantId, request.LoanNo.Trim()))
            return ApiResponse<LoanDto>.Fail($"Loan number '{request.LoanNo}' is already in use.");

        var product = await _loanProductRepository.GetByIdAsync(request.TenantId, request.LoanProductId);
        if (product is null)
            return ApiResponse<LoanDto>.Fail("Loan product was not found.");

        var id = await _loanRepository.CreateAsync(userId, request, product);
        return await GetByIdAsync(request.TenantId, id);
    }

    public async Task<ApiResponse<object>> DeleteAsync(long tenantId, long loanId)
    {
        var existing = await _loanRepository.GetByIdAsync(tenantId, loanId);
        if (existing is null)
            return ApiResponse<object>.NotFound("Loan not found.");

        try
        {
            var deleted = await _loanRepository.DeleteAsync(tenantId, loanId);
            return deleted
                ? ApiResponse<object>.Ok(null, "Loan deleted successfully.")
                : ApiResponse<object>.Fail("Failed to delete loan.");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<object>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse<PagedResult<LoanPaymentDto>>> GetPaymentsAsync(long tenantId, LoanPaymentFilterParams filters)
    {
        var (items, totalCount) = await _loanRepository.GetPaymentsAsync(tenantId, filters);
        return ApiResponse<PagedResult<LoanPaymentDto>>.Ok(PagedResult<LoanPaymentDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<LoanPaymentDto>> GetPaymentByIdAsync(long tenantId, long loanPaymentId)
    {
        var item = await _loanRepository.GetPaymentByIdAsync(tenantId, loanPaymentId);
        return item is null
            ? ApiResponse<LoanPaymentDto>.NotFound("Loan payment not found.")
            : ApiResponse<LoanPaymentDto>.Ok(item);
    }

    public async Task<ApiResponse<LoanPaymentDto>> CreatePaymentAsync(long userId, long tenantId, long loanId, CreateLoanPaymentRequest request)
    {
        var validation = await _paymentValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<LoanPaymentDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _loanRepository.PaymentNoExistsAsync(tenantId, request.PaymentNo.Trim()))
            return ApiResponse<LoanPaymentDto>.Fail($"Loan payment number '{request.PaymentNo}' is already in use.");

        try
        {
            var paymentId = await _loanRepository.CreatePaymentAsync(
                userId,
                tenantId,
                loanId,
                request with { TenantId = tenantId, PaymentNo = request.PaymentNo.Trim() });

            return await GetPaymentByIdAsync(tenantId, paymentId);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<LoanPaymentDto>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse<object>> DeletePaymentAsync(long tenantId, long loanPaymentId)
    {
        var existing = await _loanRepository.GetPaymentByIdAsync(tenantId, loanPaymentId);
        if (existing is null)
            return ApiResponse<object>.NotFound("Loan payment not found.");

        try
        {
            var deleted = await _loanRepository.DeletePaymentAsync(tenantId, loanPaymentId);
            return deleted
                ? ApiResponse<object>.Ok(null, "Loan payment cancelled successfully.")
                : ApiResponse<object>.Fail("Failed to cancel loan payment.");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<object>.Fail(ex.Message);
        }
    }
}
