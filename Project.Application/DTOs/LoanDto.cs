using FluentValidation;
using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record LoanInstallmentScheduleDto(int InstallmentNo, DateOnly DueDate, decimal PrincipalDueAmount, decimal InterestDueAmount, decimal InstallmentAmount, decimal PaidAmount, string InstallmentStatus);
public sealed record LoanDto(long LoanId, long TenantId, long MemberId, long LoanProductId, string LoanNo, DateOnly LoanDate, decimal PrincipalAmount, decimal FlatInterestRatePct, int TermMonths, decimal AdminFeeAmount, decimal PenaltyAmount, decimal InstallmentAmount, decimal TotalInterestAmount, decimal TotalPayableAmount, decimal OutstandingPrincipalAmount, decimal OutstandingTotalAmount, string Status, string? Note, IReadOnlyCollection<LoanInstallmentScheduleDto> Installments);
public sealed record CreateLoanRequest(long TenantId, long MemberId, long LoanProductId, string LoanNo, DateOnly LoanDate, decimal PrincipalAmount, decimal? FlatInterestRatePct, int? TermMonths, decimal? AdminFeeAmount, decimal? PenaltyAmount, string? Note);
public sealed record LoanPaymentDto(long LoanPaymentId, long TenantId, long LoanId, string LoanNo, long MemberId, string MemberNo, string FullName, long? LoanInstallmentScheduleId, string PaymentNo, DateTime PaymentTs, decimal PaymentAmount, decimal PrincipalPaidAmount, decimal InterestPaidAmount, decimal PenaltyPaidAmount, string? Note);
public sealed record CreateLoanPaymentRequest(long TenantId, string PaymentNo, DateTime PaymentTs, decimal PaymentAmount, long? LoanInstallmentScheduleId, string? Note);

public sealed class LoanFilterParams : PaginationParams
{
    public long? MemberId { get; set; }
    public string? Status { get; set; }
}

public sealed class LoanPaymentFilterParams : PaginationParams
{
    public long? LoanId { get; set; }
    public long? MemberId { get; set; }
}

public sealed class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
{
    public CreateLoanRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.MemberId).GreaterThan(0);
        RuleFor(x => x.LoanProductId).GreaterThan(0);
        RuleFor(x => x.LoanNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PrincipalAmount).GreaterThan(0);
    }
}

public sealed class CreateLoanPaymentRequestValidator : AbstractValidator<CreateLoanPaymentRequest>
{
    public CreateLoanPaymentRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.PaymentNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PaymentAmount).GreaterThan(0);
        RuleFor(x => x.LoanInstallmentScheduleId)
            .GreaterThan(0)
            .When(x => x.LoanInstallmentScheduleId.HasValue);
    }
}
