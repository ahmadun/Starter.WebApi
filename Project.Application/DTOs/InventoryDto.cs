using FluentValidation;
using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record ProductCategoryDto(long ProductCategoryId, long TenantId, string CategoryCode, string CategoryName, bool IsActive);
public sealed record SaveProductCategoryRequest(long TenantId, string CategoryCode, string CategoryName, bool IsActive);

public sealed class SaveProductCategoryRequestValidator : AbstractValidator<SaveProductCategoryRequest>
{
    public SaveProductCategoryRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.CategoryCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CategoryName).NotEmpty().MaximumLength(100);
    }
}

public sealed record SupplierDto(long SupplierId, long TenantId, string SupplierCode, string SupplierName, string? ContactName, string? PhoneNumber, string? Email, string? AddressLine, bool IsActive);
public sealed record SaveSupplierRequest(long TenantId, string SupplierCode, string SupplierName, string? ContactName, string? PhoneNumber, string? Email, string? AddressLine, bool IsActive);

public sealed class SaveSupplierRequestValidator : AbstractValidator<SaveSupplierRequest>
{
    public SaveSupplierRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.SupplierCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SupplierName).NotEmpty().MaximumLength(200);
    }
}

public sealed record ProductDto(
    long ProductId,
    long TenantId,
    long? ProductCategoryId,
    string Sku,
    string? Barcode,
    string ProductName,
    string UnitName,
    decimal CostPrice,
    decimal SalePrice,
    bool IsActive,
    decimal OnHandQty,
    decimal MinStockQty);

public sealed record ProductLookupDto(
    long ProductId,
    string Sku,
    string? Barcode,
    string ProductName,
    string UnitName,
    decimal SalePrice,
    decimal OnHandQty,
    decimal MinStockQty);

public sealed record SaveProductRequest(
    long TenantId,
    long? ProductCategoryId,
    string Sku,
    string? Barcode,
    string ProductName,
    string UnitName,
    decimal CostPrice,
    decimal SalePrice,
    bool IsActive,
    decimal MinStockQty);

public sealed class ProductFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public long? ProductCategoryId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class SaveProductRequestValidator : AbstractValidator<SaveProductRequest>
{
    public SaveProductRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStockQty).GreaterThanOrEqualTo(0);
    }
}

public sealed record PurchaseReceiptItemRequest(long ProductId, decimal Quantity, decimal UnitCost);
public sealed record CreatePurchaseReceiptRequest(long TenantId, string ReceiptNo, long? SupplierId, DateTime ReceiptDate, string? Note, IReadOnlyCollection<PurchaseReceiptItemRequest> Items);
public sealed record PurchaseReceiptDto(long PurchaseReceiptId, long TenantId, string ReceiptNo, long? SupplierId, DateTime ReceiptDate, decimal TotalAmount, string? Note);
public sealed record PurchaseReceiptItemDto(long ProductId, string ProductName, decimal Quantity, decimal UnitCost, decimal LineTotalAmount);
public sealed record PurchaseReceiptDetailDto(long PurchaseReceiptId, long TenantId, string ReceiptNo, long? SupplierId, string? SupplierCode, string? SupplierName, DateTime ReceiptDate, string ReceiptStatus, decimal TotalAmount, string? Note, string? CreatedByDisplayName, IReadOnlyCollection<PurchaseReceiptItemDto> Items);

public sealed class PurchaseReceiptFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public long? SupplierId { get; set; }
    public string? ReceiptStatus { get; set; }
}

public sealed class CreatePurchaseReceiptRequestValidator : AbstractValidator<CreatePurchaseReceiptRequest>
{
    public CreatePurchaseReceiptRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.ReceiptNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed record StockAdjustmentItemRequest(long ProductId, decimal ActualQty, decimal? UnitCost, string? Note);
public sealed record CreateStockAdjustmentRequest(long TenantId, string AdjustmentNo, DateTime AdjustmentTs, string AdjustmentType, string Reason, string? Note, IReadOnlyCollection<StockAdjustmentItemRequest> Items);
public sealed record StockAdjustmentDto(long StockAdjustmentId, long TenantId, string AdjustmentNo, DateTime AdjustmentTs, string AdjustmentType, string Reason, string? Note);
public sealed record StockAdjustmentItemDto(long ProductId, string ProductName, decimal SystemQty, decimal ActualQty, decimal AdjustmentQty, decimal? UnitCost, string? Note);
public sealed record StockAdjustmentDetailDto(long StockAdjustmentId, long TenantId, string AdjustmentNo, DateTime AdjustmentTs, string AdjustmentType, string Reason, string? Note, string? CreatedByDisplayName, IReadOnlyCollection<StockAdjustmentItemDto> Items);

public sealed class StockAdjustmentFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public string? AdjustmentType { get; set; }
}

public sealed record StockMovementDto(long StockMovementId, long TenantId, long ProductId, string ProductName, string Sku, DateTime MovementTs, string MovementType, decimal Quantity, decimal? UnitCost, string SourceTable, long SourceId, string? Note, string? CreatedByDisplayName);

public sealed class StockMovementFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public long? ProductId { get; set; }
    public string? MovementType { get; set; }
    public string? SourceTable { get; set; }
}

public sealed class CreateStockAdjustmentRequestValidator : AbstractValidator<CreateStockAdjustmentRequest>
{
    public CreateStockAdjustmentRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.AdjustmentNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AdjustmentType).Must(v => new[] { "stock_opname", "correction", "damaged", "expired" }.Contains(v));
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Items).NotEmpty();
    }
}
