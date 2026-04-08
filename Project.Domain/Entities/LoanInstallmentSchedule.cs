namespace Project.Domain.Entities;

public sealed class LoanInstallmentSchedule
{
    public long LoanInstallmentScheduleId { get; set; }
    public long LoanId { get; set; }
    public int InstallmentNo { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal PrincipalDueAmount { get; set; }
    public decimal InterestDueAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string InstallmentStatus { get; set; } = string.Empty;
    public DateTime? SettledAt { get; set; }
}
