namespace Project.Domain.Entities;

public sealed class LoanProduct
{
    public long LoanProductId { get; set; }
    public long TenantId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal DefaultFlatInterestRatePct { get; set; }
    public decimal? MinFlatInterestRatePct { get; set; }
    public decimal? MaxFlatInterestRatePct { get; set; }
    public int DefaultTermMonths { get; set; }
    public int? MinTermMonths { get; set; }
    public int? MaxTermMonths { get; set; }
    public decimal? MinPrincipalAmount { get; set; }
    public decimal? MaxPrincipalAmount { get; set; }
    public decimal DefaultAdminFeeAmount { get; set; }
    public decimal DefaultPenaltyAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
