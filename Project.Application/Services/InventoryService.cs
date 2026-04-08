using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;

namespace Project.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _repository;
    private readonly IValidator<SaveProductCategoryRequest> _categoryValidator;
    private readonly IValidator<SaveSupplierRequest> _supplierValidator;
    private readonly IValidator<SaveProductRequest> _productValidator;
    private readonly IValidator<CreatePurchaseReceiptRequest> _purchaseReceiptValidator;
    private readonly IValidator<CreateStockAdjustmentRequest> _stockAdjustmentValidator;

    public InventoryService(
        IInventoryRepository repository,
        IValidator<SaveProductCategoryRequest> categoryValidator,
        IValidator<SaveSupplierRequest> supplierValidator,
        IValidator<SaveProductRequest> productValidator,
        IValidator<CreatePurchaseReceiptRequest> purchaseReceiptValidator,
        IValidator<CreateStockAdjustmentRequest> stockAdjustmentValidator)
    {
        _repository = repository;
        _categoryValidator = categoryValidator;
        _supplierValidator = supplierValidator;
        _productValidator = productValidator;
        _purchaseReceiptValidator = purchaseReceiptValidator;
        _stockAdjustmentValidator = stockAdjustmentValidator;
    }

    public async Task<ApiResponse<IEnumerable<ProductCategoryDto>>> GetCategoriesAsync(long tenantId)
        => ApiResponse<IEnumerable<ProductCategoryDto>>.Ok((await _repository.GetCategoriesAsync(tenantId)).Select(x => new ProductCategoryDto(x.ProductCategoryId, x.TenantId, x.CategoryCode, x.CategoryName, x.IsActive)));

    public async Task<ApiResponse<ProductCategoryDto>> CreateCategoryAsync(SaveProductCategoryRequest request)
        => await SaveCategoryAsync(request.TenantId, null, request);

    public async Task<ApiResponse<ProductCategoryDto>> UpdateCategoryAsync(long tenantId, long productCategoryId, SaveProductCategoryRequest request)
        => await SaveCategoryAsync(tenantId, productCategoryId, request);

    public async Task<ApiResponse<IEnumerable<SupplierDto>>> GetSuppliersAsync(long tenantId)
        => ApiResponse<IEnumerable<SupplierDto>>.Ok((await _repository.GetSuppliersAsync(tenantId)).Select(MapSupplier));

    public async Task<ApiResponse<SupplierDto>> CreateSupplierAsync(SaveSupplierRequest request)
        => await SaveSupplierAsync(request.TenantId, null, request);

    public async Task<ApiResponse<SupplierDto>> UpdateSupplierAsync(long tenantId, long supplierId, SaveSupplierRequest request)
        => await SaveSupplierAsync(tenantId, supplierId, request);

    public async Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(long tenantId, ProductFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetProductsAsync(tenantId, filters);
        return ApiResponse<PagedResult<ProductDto>>.Ok(PagedResult<ProductDto>.Create(items.Select(MapProduct), totalCount, filters));
    }

    public async Task<ApiResponse<ProductDto>> GetProductByIdAsync(long tenantId, long productId)
    {
        var entity = await _repository.GetProductByIdAsync(tenantId, productId);
        return entity is null ? ApiResponse<ProductDto>.NotFound("Product not found.") : ApiResponse<ProductDto>.Ok(MapProduct(entity));
    }

    public async Task<ApiResponse<IEnumerable<ProductLookupDto>>> LookupProductsAsync(long tenantId, string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return ApiResponse<IEnumerable<ProductLookupDto>>.Ok(Array.Empty<ProductLookupDto>());

        var items = await _repository.LookupProductsAsync(tenantId, query.Trim(), limit);
        return ApiResponse<IEnumerable<ProductLookupDto>>.Ok(items.Select(MapProductLookup));
    }

    public async Task<ApiResponse<ProductDto>> CreateProductAsync(SaveProductRequest request)
        => await SaveProductAsync(request.TenantId, null, request);

    public async Task<ApiResponse<ProductDto>> UpdateProductAsync(long tenantId, long productId, SaveProductRequest request)
        => await SaveProductAsync(tenantId, productId, request);

    public async Task<ApiResponse<PagedResult<PurchaseReceiptDto>>> GetPurchaseReceiptsAsync(long tenantId, PurchaseReceiptFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetPurchaseReceiptsAsync(tenantId, filters);
        return ApiResponse<PagedResult<PurchaseReceiptDto>>.Ok(PagedResult<PurchaseReceiptDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<PurchaseReceiptDetailDto>> GetPurchaseReceiptByIdAsync(long tenantId, long purchaseReceiptId)
    {
        var item = await _repository.GetPurchaseReceiptByIdAsync(tenantId, purchaseReceiptId);
        return item is null ? ApiResponse<PurchaseReceiptDetailDto>.NotFound("Purchase receipt not found.") : ApiResponse<PurchaseReceiptDetailDto>.Ok(item);
    }

    public async Task<ApiResponse<PurchaseReceiptDto>> CreatePurchaseReceiptAsync(long userId, CreatePurchaseReceiptRequest request)
    {
        var validation = await _purchaseReceiptValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<PurchaseReceiptDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var id = await _repository.CreatePurchaseReceiptAsync(userId, request);
        var total = request.Items.Sum(x => x.Quantity * x.UnitCost);
        return ApiResponse<PurchaseReceiptDto>.Created(new PurchaseReceiptDto(id, request.TenantId, request.ReceiptNo, request.SupplierId, request.ReceiptDate, total, request.Note));
    }

    public async Task<ApiResponse<PagedResult<StockAdjustmentDto>>> GetStockAdjustmentsAsync(long tenantId, StockAdjustmentFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetStockAdjustmentsAsync(tenantId, filters);
        return ApiResponse<PagedResult<StockAdjustmentDto>>.Ok(PagedResult<StockAdjustmentDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<StockAdjustmentDetailDto>> GetStockAdjustmentByIdAsync(long tenantId, long stockAdjustmentId)
    {
        var item = await _repository.GetStockAdjustmentByIdAsync(tenantId, stockAdjustmentId);
        return item is null ? ApiResponse<StockAdjustmentDetailDto>.NotFound("Stock adjustment not found.") : ApiResponse<StockAdjustmentDetailDto>.Ok(item);
    }

    public async Task<ApiResponse<StockAdjustmentDto>> CreateStockAdjustmentAsync(long userId, CreateStockAdjustmentRequest request)
    {
        var validation = await _stockAdjustmentValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<StockAdjustmentDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var id = await _repository.CreateStockAdjustmentAsync(userId, request);
        return ApiResponse<StockAdjustmentDto>.Created(new StockAdjustmentDto(id, request.TenantId, request.AdjustmentNo, request.AdjustmentTs, request.AdjustmentType, request.Reason, request.Note));
    }

    public async Task<ApiResponse<PagedResult<StockMovementDto>>> GetStockMovementsAsync(long tenantId, StockMovementFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetStockMovementsAsync(tenantId, filters);
        return ApiResponse<PagedResult<StockMovementDto>>.Ok(PagedResult<StockMovementDto>.Create(items, totalCount, filters));
    }

    private async Task<ApiResponse<ProductCategoryDto>> SaveCategoryAsync(long tenantId, long? id, SaveProductCategoryRequest request)
    {
        var validation = await _categoryValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<ProductCategoryDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.ProductCategoryCodeExistsAsync(tenantId, request.CategoryCode.Trim(), id))
            return ApiResponse<ProductCategoryDto>.Fail($"Category code '{request.CategoryCode}' is already in use.");

        var entity = id.HasValue ? await _repository.GetCategoryByIdAsync(tenantId, id.Value) : null;
        if (id.HasValue && entity is null)
            return ApiResponse<ProductCategoryDto>.NotFound("Category not found.");

        entity ??= new ProductCategory { TenantId = tenantId, CreatedAt = DateTime.UtcNow };
        entity.CategoryCode = request.CategoryCode.Trim();
        entity.CategoryName = request.CategoryName.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (id.HasValue)
        {
            await _repository.UpdateCategoryAsync(entity);
            return ApiResponse<ProductCategoryDto>.Ok(new ProductCategoryDto(entity.ProductCategoryId, entity.TenantId, entity.CategoryCode, entity.CategoryName, entity.IsActive), "Updated successfully.");
        }

        entity.ProductCategoryId = await _repository.CreateCategoryAsync(entity);
        return ApiResponse<ProductCategoryDto>.Created(new ProductCategoryDto(entity.ProductCategoryId, entity.TenantId, entity.CategoryCode, entity.CategoryName, entity.IsActive));
    }

    private async Task<ApiResponse<SupplierDto>> SaveSupplierAsync(long tenantId, long? id, SaveSupplierRequest request)
    {
        var validation = await _supplierValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<SupplierDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.SupplierCodeExistsAsync(tenantId, request.SupplierCode.Trim(), id))
            return ApiResponse<SupplierDto>.Fail($"Supplier code '{request.SupplierCode}' is already in use.");

        var entity = id.HasValue ? await _repository.GetSupplierByIdAsync(tenantId, id.Value) : null;
        if (id.HasValue && entity is null)
            return ApiResponse<SupplierDto>.NotFound("Supplier not found.");

        entity ??= new Supplier { TenantId = tenantId, CreatedAt = DateTime.UtcNow };
        entity.SupplierCode = request.SupplierCode.Trim();
        entity.SupplierName = request.SupplierName.Trim();
        entity.ContactName = request.ContactName?.Trim();
        entity.PhoneNumber = request.PhoneNumber?.Trim();
        entity.Email = request.Email?.Trim().ToLowerInvariant();
        entity.AddressLine = request.AddressLine?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (id.HasValue)
        {
            await _repository.UpdateSupplierAsync(entity);
            return ApiResponse<SupplierDto>.Ok(MapSupplier(entity), "Updated successfully.");
        }

        entity.SupplierId = await _repository.CreateSupplierAsync(entity);
        return ApiResponse<SupplierDto>.Created(MapSupplier(entity));
    }

    private async Task<ApiResponse<ProductDto>> SaveProductAsync(long tenantId, long? id, SaveProductRequest request)
    {
        var validation = await _productValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<ProductDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.ProductSkuExistsAsync(tenantId, request.Sku.Trim(), id))
            return ApiResponse<ProductDto>.Fail($"SKU '{request.Sku}' is already in use.");

        if (await _repository.ProductBarcodeExistsAsync(tenantId, request.Barcode?.Trim(), id))
            return ApiResponse<ProductDto>.Fail($"Barcode '{request.Barcode}' is already in use.");

        var entity = id.HasValue ? await _repository.GetProductByIdAsync(tenantId, id.Value) : null;
        if (id.HasValue && entity is null)
            return ApiResponse<ProductDto>.NotFound("Product not found.");

        entity ??= new Product { TenantId = tenantId, CreatedAt = DateTime.UtcNow };
        entity.ProductCategoryId = request.ProductCategoryId;
        entity.Sku = request.Sku.Trim();
        entity.Barcode = request.Barcode?.Trim();
        entity.ProductName = request.ProductName.Trim();
        entity.UnitName = request.UnitName.Trim();
        entity.CostPrice = request.CostPrice;
        entity.SalePrice = request.SalePrice;
        entity.IsActive = request.IsActive;
        entity.MinStockQty = request.MinStockQty;
        entity.UpdatedAt = DateTime.UtcNow;

        if (id.HasValue)
        {
            await _repository.UpdateProductAsync(entity);
            return ApiResponse<ProductDto>.Ok(MapProduct(entity), "Updated successfully.");
        }

        entity.ProductId = await _repository.CreateProductAsync(entity);
        entity.OnHandQty = 0;
        return ApiResponse<ProductDto>.Created(MapProduct(entity));
    }

    private static SupplierDto MapSupplier(Supplier x) => new(x.SupplierId, x.TenantId, x.SupplierCode, x.SupplierName, x.ContactName, x.PhoneNumber, x.Email, x.AddressLine, x.IsActive);
    private static ProductDto MapProduct(Product x) => new(x.ProductId, x.TenantId, x.ProductCategoryId, x.Sku, x.Barcode, x.ProductName, x.UnitName, x.CostPrice, x.SalePrice, x.IsActive, x.OnHandQty ?? 0, x.MinStockQty ?? 0);
    private static ProductLookupDto MapProductLookup(Product x) => new(x.ProductId, x.Sku, x.Barcode, x.ProductName, x.UnitName, x.SalePrice, x.OnHandQty ?? 0, x.MinStockQty ?? 0);
}
