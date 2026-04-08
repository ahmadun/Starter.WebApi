namespace Project.Domain.Entities;

public sealed class StockAdjustment
{
    public long StockAdjustmentId { get; set; }
    public long TenantId { get; set; }
    public string AdjustmentNo { get; set; } = string.Empty;
    public DateTime AdjustmentTs { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
