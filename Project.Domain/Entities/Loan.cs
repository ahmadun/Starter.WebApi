namespace Project.Domain.Entities;

public sealed class Loan
{
    public long LoanId { get; set; }
    public long TenantId { get; set; }
    public long MemberId { get; set; }
    public long LoanProductId { get; set; }
    public string LoanNo { get; set; } = string.Empty;
    public DateOnly LoanDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal FlatInterestRatePct { get; set; }
    public int TermMonths { get; set; }
    public decimal AdminFeeAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal TotalInterestAmount { get; set; }
    public decimal TotalPayableAmount { get; set; }
    public decimal OutstandingPrincipalAmount { get; set; }
    public decimal OutstandingTotalAmount { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime? DisbursedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
