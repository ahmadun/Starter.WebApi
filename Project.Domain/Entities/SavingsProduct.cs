namespace Project.Domain.Entities;

public sealed class SavingsProduct
{
    public long SavingsProductId { get; set; }
    public long TenantId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SavingsKind { get; set; } = string.Empty;
    public string Periodicity { get; set; } = string.Empty;
    public decimal? DefaultAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
