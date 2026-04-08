using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Application.Services;

public sealed class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ILoanProductRepository _loanProductRepository;
    private readonly IValidator<CreateSaleRequest> _validator;
    private readonly IValidator<ConvertMemberCreditSaleRequest> _conversionValidator;

    public SaleService(
        ISaleRepository saleRepository,
        ILoanRepository loanRepository,
        ILoanProductRepository loanProductRepository,
        IValidator<CreateSaleRequest> validator,
        IValidator<ConvertMemberCreditSaleRequest> conversionValidator)
    {
        _saleRepository = saleRepository;
        _loanRepository = loanRepository;
        _loanProductRepository = loanProductRepository;
        _validator = validator;
        _conversionValidator = conversionValidator;
    }

    public async Task<ApiResponse<PagedResult<SaleDto>>> GetAllAsync(long tenantId, SaleFilterParams filters)
    {
        var (items, totalCount) = await _saleRepository.GetAllAsync(tenantId, filters);
        return ApiResponse<PagedResult<SaleDto>>.Ok(PagedResult<SaleDto>.Create(items.Select(Map), totalCount, filters));
    }

    public async Task<ApiResponse<SaleDto>> GetByIdAsync(long tenantId, long saleId)
    {
        var item = await _saleRepository.GetByIdAsync(tenantId, saleId);
        return item is null ? ApiResponse<SaleDto>.NotFound("Sale not found.") : ApiResponse<SaleDto>.Ok(Map(item));
    }

    public async Task<ApiResponse<SaleReceiptDto>> GetReceiptAsync(long tenantId, long saleId)
    {
        var receipt = await _saleRepository.GetReceiptAsync(tenantId, saleId);
        return receipt is null ? ApiResponse<SaleReceiptDto>.NotFound("Sale receipt not found.") : ApiResponse<SaleReceiptDto>.Ok(receipt);
    }

    public async Task<ApiResponse<SaleDto>> CreateAsync(long userId, CreateSaleRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<SaleDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _saleRepository.SaleNoExistsAsync(request.TenantId, request.SaleNo.Trim()))
            return ApiResponse<SaleDto>.Fail($"Sale number '{request.SaleNo}' is already in use.");

        var id = await _saleRepository.CreateAsync(userId, request);
        return await GetByIdAsync(request.TenantId, id);
    }

    public async Task<ApiResponse<MemberCreditConversionDto>> ConvertMemberCreditToLoanAsync(long userId, long tenantId, long saleId, ConvertMemberCreditSaleRequest request)
    {
        var validation = await _conversionValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<MemberCreditConversionDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var sale = await _saleRepository.GetByIdAsync(tenantId, saleId);
        if (sale is null)
            return ApiResponse<MemberCreditConversionDto>.NotFound("Sale not found.");

        if (!string.Equals(sale.SaleType, "member_credit", StringComparison.OrdinalIgnoreCase) || !sale.MemberId.HasValue)
            return ApiResponse<MemberCreditConversionDto>.Fail("Only member credit sales can be converted to loans.");

        if (!string.Equals(sale.SaleStatus, "posted", StringComparison.OrdinalIgnoreCase))
            return ApiResponse<MemberCreditConversionDto>.Fail("Only posted sales can be converted to loans.");

        if (await _saleRepository.HasMemberCreditConversionAsync(tenantId, saleId))
            return ApiResponse<MemberCreditConversionDto>.Fail("This member credit sale has already been converted to a loan.");

        if (await _loanRepository.LoanNoExistsAsync(tenantId, request.LoanNo.Trim()))
            return ApiResponse<MemberCreditConversionDto>.Fail($"Loan number '{request.LoanNo}' is already in use.");

        var loanProduct = await _loanProductRepository.GetByIdAsync(tenantId, request.LoanProductId);
        if (loanProduct is null || !loanProduct.IsActive)
            return ApiResponse<MemberCreditConversionDto>.Fail("Loan product was not found or is inactive.");

        var principalAmount = sale.TotalAmount;
        if (loanProduct.MinPrincipalAmount.HasValue && principalAmount < loanProduct.MinPrincipalAmount.Value)
            return ApiResponse<MemberCreditConversionDto>.Fail("Sale total is below the loan product minimum principal amount.");

        if (loanProduct.MaxPrincipalAmount.HasValue && principalAmount > loanProduct.MaxPrincipalAmount.Value)
            return ApiResponse<MemberCreditConversionDto>.Fail("Sale total exceeds the loan product maximum principal amount.");

        try
        {
            var result = await _loanRepository.ConvertMemberCreditSaleToLoanAsync(
                userId,
                sale,
                loanProduct,
                request with { TenantId = tenantId, LoanNo = request.LoanNo.Trim() });

            return ApiResponse<MemberCreditConversionDto>.Created(result, "Member credit sale converted to loan successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<MemberCreditConversionDto>.Fail(ex.Message);
        }
    }

    private static SaleDto Map(Project.Domain.Entities.Sale item) => new(item.SaleId, item.TenantId, item.SaleNo, item.ReceiptNo, item.SaleTs, item.MemberId, item.SaleType, item.TotalAmount, item.PaidAmount, item.ChangeAmount, item.Note);
}
