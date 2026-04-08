using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;

namespace Project.Application.Services;

public sealed class SavingsProductService : ISavingsProductService
{
    private readonly ISavingsProductRepository _repository;
    private readonly IValidator<SaveSavingsProductRequest> _validator;

    public SavingsProductService(ISavingsProductRepository repository, IValidator<SaveSavingsProductRequest> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<ApiResponse<PagedResult<SavingsProductDto>>> GetAllAsync(long tenantId, SavingsProductFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetAllAsync(tenantId, filters);
        return ApiResponse<PagedResult<SavingsProductDto>>.Ok(PagedResult<SavingsProductDto>.Create(items.Select(Map), totalCount, filters));
    }

    public async Task<ApiResponse<SavingsProductDto>> GetByIdAsync(long tenantId, long savingsProductId)
    {
        var entity = await _repository.GetByIdAsync(tenantId, savingsProductId);
        return entity is null ? ApiResponse<SavingsProductDto>.NotFound("Savings product not found.") : ApiResponse<SavingsProductDto>.Ok(Map(entity));
    }

    public async Task<ApiResponse<SavingsProductDto>> CreateAsync(SaveSavingsProductRequest request)
        => await SaveAsync(request.TenantId, null, request);

    public async Task<ApiResponse<SavingsProductDto>> UpdateAsync(long tenantId, long savingsProductId, SaveSavingsProductRequest request)
        => await SaveAsync(tenantId, savingsProductId, request);

    private async Task<ApiResponse<SavingsProductDto>> SaveAsync(long tenantId, long? savingsProductId, SaveSavingsProductRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<SavingsProductDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.ProductCodeExistsAsync(tenantId, request.ProductCode.Trim(), savingsProductId))
            return ApiResponse<SavingsProductDto>.Fail($"Savings product code '{request.ProductCode}' is already in use.");

        var entity = savingsProductId.HasValue ? await _repository.GetByIdAsync(tenantId, savingsProductId.Value) : null;
        if (savingsProductId.HasValue && entity is null)
            return ApiResponse<SavingsProductDto>.NotFound("Savings product not found.");

        entity ??= new SavingsProduct { TenantId = tenantId, CreatedAt = DateTime.UtcNow };
        entity.ProductCode = request.ProductCode.Trim();
        entity.ProductName = request.ProductName.Trim();
        entity.SavingsKind = request.SavingsKind;
        entity.Periodicity = request.Periodicity;
        entity.DefaultAmount = request.DefaultAmount;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (savingsProductId.HasValue)
        {
            await _repository.UpdateAsync(entity);
            return ApiResponse<SavingsProductDto>.Ok(Map(entity), "Updated successfully.");
        }

        entity.SavingsProductId = await _repository.CreateAsync(entity);
        return ApiResponse<SavingsProductDto>.Created(Map(entity));
    }

    private static SavingsProductDto Map(SavingsProduct entity) => new(entity.SavingsProductId, entity.TenantId, entity.ProductCode, entity.ProductName, entity.SavingsKind, entity.Periodicity, entity.DefaultAmount, entity.IsActive);
}
