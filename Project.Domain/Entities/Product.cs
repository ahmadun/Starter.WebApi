namespace Project.Domain.Entities;

public sealed class Product
{
    public long ProductId { get; set; }
    public long TenantId { get; set; }
    public long? ProductCategoryId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal? OnHandQty { get; set; }
    public decimal? MinStockQty { get; set; }
}
