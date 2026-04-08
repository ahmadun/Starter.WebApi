using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface IInventoryService
{
    Task<ApiResponse<IEnumerable<ProductCategoryDto>>> GetCategoriesAsync(long tenantId);
    Task<ApiResponse<ProductCategoryDto>> CreateCategoryAsync(SaveProductCategoryRequest request);
    Task<ApiResponse<ProductCategoryDto>> UpdateCategoryAsync(long tenantId, long productCategoryId, SaveProductCategoryRequest request);
    Task<ApiResponse<IEnumerable<SupplierDto>>> GetSuppliersAsync(long tenantId);
    Task<ApiResponse<SupplierDto>> CreateSupplierAsync(SaveSupplierRequest request);
    Task<ApiResponse<SupplierDto>> UpdateSupplierAsync(long tenantId, long supplierId, SaveSupplierRequest request);
    Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(long tenantId, ProductFilterParams filters);
    Task<ApiResponse<ProductDto>> GetProductByIdAsync(long tenantId, long productId);
    Task<ApiResponse<IEnumerable<ProductLookupDto>>> LookupProductsAsync(long tenantId, string query, int limit = 10);
    Task<ApiResponse<ProductDto>> CreateProductAsync(SaveProductRequest request);
    Task<ApiResponse<ProductDto>> UpdateProductAsync(long tenantId, long productId, SaveProductRequest request);
    Task<ApiResponse<PagedResult<PurchaseReceiptDto>>> GetPurchaseReceiptsAsync(long tenantId, PurchaseReceiptFilterParams filters);
    Task<ApiResponse<PurchaseReceiptDetailDto>> GetPurchaseReceiptByIdAsync(long tenantId, long purchaseReceiptId);
    Task<ApiResponse<PurchaseReceiptDto>> CreatePurchaseReceiptAsync(long userId, CreatePurchaseReceiptRequest request);
    Task<ApiResponse<PagedResult<StockAdjustmentDto>>> GetStockAdjustmentsAsync(long tenantId, StockAdjustmentFilterParams filters);
    Task<ApiResponse<StockAdjustmentDetailDto>> GetStockAdjustmentByIdAsync(long tenantId, long stockAdjustmentId);
    Task<ApiResponse<StockAdjustmentDto>> CreateStockAdjustmentAsync(long userId, CreateStockAdjustmentRequest request);
    Task<ApiResponse<PagedResult<StockMovementDto>>> GetStockMovementsAsync(long tenantId, StockMovementFilterParams filters);
}
