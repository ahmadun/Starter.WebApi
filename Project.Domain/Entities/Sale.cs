namespace Project.Domain.Entities;

public sealed class Sale
{
    public long SaleId { get; set; }
    public long TenantId { get; set; }
    public string SaleNo { get; set; } = string.Empty;
    public string? ReceiptNo { get; set; }
    public DateTime SaleTs { get; set; }
    public long? MemberId { get; set; }
    public long CashierUserId { get; set; }
    public long? MemberTransactionId { get; set; }
    public string SaleType { get; set; } = "cash";
    public string SaleStatus { get; set; } = "posted";
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
