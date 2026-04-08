using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;

public interface IInventoryRepository
{
    Task<IEnumerable<ProductCategory>> GetCategoriesAsync(long tenantId);
    Task<ProductCategory?> GetCategoryByIdAsync(long tenantId, long productCategoryId);
    Task<bool> ProductCategoryCodeExistsAsync(long tenantId, string categoryCode, long? excludeId = null);
    Task<long> CreateCategoryAsync(ProductCategory category);
    Task<bool> UpdateCategoryAsync(ProductCategory category);
    Task<IEnumerable<Supplier>> GetSuppliersAsync(long tenantId);
    Task<Supplier?> GetSupplierByIdAsync(long tenantId, long supplierId);
    Task<bool> SupplierCodeExistsAsync(long tenantId, string supplierCode, long? excludeId = null);
    Task<long> CreateSupplierAsync(Supplier supplier);
    Task<bool> UpdateSupplierAsync(Supplier supplier);
    Task<(IEnumerable<Product> Items, int TotalCount)> GetProductsAsync(long tenantId, ProductFilterParams filters);
    Task<Product?> GetProductByIdAsync(long tenantId, long productId);
    Task<IEnumerable<Product>> LookupProductsAsync(long tenantId, string query, int limit);
    Task<bool> ProductSkuExistsAsync(long tenantId, string sku, long? excludeId = null);
    Task<bool> ProductBarcodeExistsAsync(long tenantId, string? barcode, long? excludeId = null);
    Task<long> CreateProductAsync(Product product);
    Task<bool> UpdateProductAsync(Product product);
    Task<(IEnumerable<PurchaseReceiptDto> Items, int TotalCount)> GetPurchaseReceiptsAsync(long tenantId, PurchaseReceiptFilterParams filters);
    Task<PurchaseReceiptDetailDto?> GetPurchaseReceiptByIdAsync(long tenantId, long purchaseReceiptId);
    Task<long> CreatePurchaseReceiptAsync(long userId, CreatePurchaseReceiptRequest request);
    Task<(IEnumerable<StockAdjustmentDto> Items, int TotalCount)> GetStockAdjustmentsAsync(long tenantId, StockAdjustmentFilterParams filters);
    Task<StockAdjustmentDetailDto?> GetStockAdjustmentByIdAsync(long tenantId, long stockAdjustmentId);
    Task<long> CreateStockAdjustmentAsync(long userId, CreateStockAdjustmentRequest request);
    Task<(IEnumerable<StockMovementDto> Items, int TotalCount)> GetStockMovementsAsync(long tenantId, StockMovementFilterParams filters);
}
