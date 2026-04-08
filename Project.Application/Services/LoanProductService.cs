using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;

namespace Project.Application.Services;

public sealed class LoanProductService : ILoanProductService
{
    private readonly ILoanProductRepository _repository;
    private readonly IValidator<SaveLoanProductRequest> _validator;

    public LoanProductService(ILoanProductRepository repository, IValidator<SaveLoanProductRequest> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<ApiResponse<PagedResult<LoanProductDto>>> GetAllAsync(long tenantId, LoanProductFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetAllAsync(tenantId, filters);
        return ApiResponse<PagedResult<LoanProductDto>>.Ok(PagedResult<LoanProductDto>.Create(items.Select(Map), totalCount, filters));
    }

    public async Task<ApiResponse<LoanProductDto>> GetByIdAsync(long tenantId, long loanProductId)
    {
        var entity = await _repository.GetByIdAsync(tenantId, loanProductId);
        return entity is null ? ApiResponse<LoanProductDto>.NotFound("Loan product not found.") : ApiResponse<LoanProductDto>.Ok(Map(entity));
    }

    public async Task<ApiResponse<LoanProductDto>> CreateAsync(SaveLoanProductRequest request)
        => await SaveAsync(request.TenantId, null, request);

    public async Task<ApiResponse<LoanProductDto>> UpdateAsync(long tenantId, long loanProductId, SaveLoanProductRequest request)
        => await SaveAsync(tenantId, loanProductId, request);

    private async Task<ApiResponse<LoanProductDto>> SaveAsync(long tenantId, long? loanProductId, SaveLoanProductRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<LoanProductDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.ProductCodeExistsAsync(tenantId, request.ProductCode.Trim(), loanProductId))
            return ApiResponse<LoanProductDto>.Fail($"Loan product code '{request.ProductCode}' is already in use.");

        var entity = loanProductId.HasValue ? await _repository.GetByIdAsync(tenantId, loanProductId.Value) : null;
        if (loanProductId.HasValue && entity is null)
            return ApiResponse<LoanProductDto>.NotFound("Loan product not found.");

        entity ??= new LoanProduct { TenantId = tenantId, CreatedAt = DateTime.UtcNow };
        entity.ProductCode = request.ProductCode.Trim();
        entity.ProductName = request.ProductName.Trim();
        entity.DefaultFlatInterestRatePct = request.DefaultFlatInterestRatePct;
        entity.MinFlatInterestRatePct = request.MinFlatInterestRatePct;
        entity.MaxFlatInterestRatePct = request.MaxFlatInterestRatePct;
        entity.DefaultTermMonths = request.DefaultTermMonths;
        entity.MinTermMonths = request.MinTermMonths;
        entity.MaxTermMonths = request.MaxTermMonths;
        entity.MinPrincipalAmount = request.MinPrincipalAmount;
        entity.MaxPrincipalAmount = request.MaxPrincipalAmount;
        entity.DefaultAdminFeeAmount = request.DefaultAdminFeeAmount;
        entity.DefaultPenaltyAmount = request.DefaultPenaltyAmount;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (loanProductId.HasValue)
        {
            await _repository.UpdateAsync(entity);
            return ApiResponse<LoanProductDto>.Ok(Map(entity), "Updated successfully.");
        }

        entity.LoanProductId = await _repository.CreateAsync(entity);
        return ApiResponse<LoanProductDto>.Created(Map(entity));
    }

    private static LoanProductDto Map(LoanProduct entity) => new(entity.LoanProductId, entity.TenantId, entity.ProductCode, entity.ProductName, entity.DefaultFlatInterestRatePct, entity.MinFlatInterestRatePct, entity.MaxFlatInterestRatePct, entity.DefaultTermMonths, entity.MinTermMonths, entity.MaxTermMonths, entity.MinPrincipalAmount, entity.MaxPrincipalAmount, entity.DefaultAdminFeeAmount, entity.DefaultPenaltyAmount, entity.IsActive);
}
