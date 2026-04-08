namespace Project.Domain.Entities;

public sealed class PurchaseReceipt
{
    public long PurchaseReceiptId { get; set; }
    public long TenantId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public long? SupplierId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public string ReceiptStatus { get; set; } = "posted";
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
