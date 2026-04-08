namespace Project.Domain.Entities;

public sealed class ProductCategory
{
    public long ProductCategoryId { get; set; }
    public long TenantId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
