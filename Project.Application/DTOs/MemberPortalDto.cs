using FluentValidation;
using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record MemberPortalProfileDto(
    long UserId,
    long TenantId,
    string TenantCode,
    string TenantName,
    long MemberId,
    string MemberNo,
    string? EmployeeCode,
    string FullName,
    string? IdentityNo,
    string? PhoneNumber,
    string? Email,
    string? AddressLine,
    DateOnly JoinDate,
    string MemberStatus,
    string Username,
    string DisplayName);

public sealed record MemberPortalDashboardDto(
    decimal TotalSavingsBalance,
    decimal OutstandingLoanAmount,
    int ActiveLoanCount,
    int PendingInstallmentCount,
    DateOnly? NextInstallmentDueDate,
    decimal? NextInstallmentAmount,
    decimal TotalPurchaseAmount);

public sealed record MemberPortalSavingsAccountDto(
    long MemberSavingsAccountId,
    long SavingsProductId,
    string ProductCode,
    string ProductName,
    string SavingsKind,
    string Periodicity,
    decimal? DefaultAmount,
    DateTime OpenedAt,
    string AccountStatus,
    decimal BalanceAmount,
    DateTime? LastTransactionAt);

public sealed record MemberPortalLoanInstallmentDto(
    int InstallmentNo,
    DateOnly DueDate,
    decimal PrincipalDueAmount,
    decimal InterestDueAmount,
    decimal InstallmentAmount,
    decimal PaidAmount,
    string InstallmentStatus,
    DateTime? SettledAt);

public sealed record MemberPortalLoanDto(
    long LoanId,
    long LoanProductId,
    string LoanProductName,
    string LoanNo,
    DateOnly LoanDate,
    decimal PrincipalAmount,
    decimal FlatInterestRatePct,
    int TermMonths,
    decimal AdminFeeAmount,
    decimal PenaltyAmount,
    decimal InstallmentAmount,
    decimal TotalInterestAmount,
    decimal TotalPayableAmount,
    decimal OutstandingPrincipalAmount,
    decimal OutstandingTotalAmount,
    string Status,
    DateOnly? NextDueDate,
    decimal? NextDueAmount,
    int PendingInstallmentCount,
    string? Note,
    IReadOnlyCollection<MemberPortalLoanInstallmentDto> Installments);

public sealed record MemberPortalPurchaseDto(
    long SaleId,
    string SaleNo,
    string? ReceiptNo,
    DateTime SaleTs,
    string SaleType,
    string SaleStatus,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal ChangeAmount,
    string? Note);

public sealed record MemberPortalTransactionDto(
    long MemberTransactionId,
    string TransactionNo,
    DateTime TransactionTs,
    string SourceModule,
    string SourceTable,
    long SourceId,
    string EntryType,
    decimal Amount,
    string? Description,
    string? ReferenceNo);

public sealed record MemberLoanRequestDto(
    long MemberLoanRequestId,
    long MemberId,
    string MemberNo,
    string FullName,
    string RequestNo,
    long LoanProductId,
    string LoanProductCode,
    string LoanProductName,
    decimal PrincipalAmount,
    int ProposedTermMonths,
    string Status,
    string? Note,
    string? ReviewerNote,
    long? ApprovedLoanId,
    DateTime RequestedAt,
    DateTime? ReviewedAt);

public sealed record CreateMemberLoanRequest(
    long LoanProductId,
    decimal PrincipalAmount,
    int? ProposedTermMonths,
    string? Note);

public sealed record SavingsWithdrawalRequestDto(
    long SavingsWithdrawalRequestId,
    long MemberId,
    string MemberNo,
    string FullName,
    string RequestNo,
    long SavingsProductId,
    string SavingsProductCode,
    string SavingsProductName,
    decimal Amount,
    string Status,
    string? Note,
    string? ReviewerNote,
    long? ApprovedSavingsTransactionId,
    DateTime RequestedAt,
    DateTime? ReviewedAt);

public sealed record CreateSavingsWithdrawalRequest(
    long SavingsProductId,
    decimal Amount,
    string? Note);

public sealed class MemberPortalPurchaseFilterParams : PaginationParams
{
    public string? SaleType { get; set; }
}

public sealed class MemberPortalTransactionFilterParams : PaginationParams
{
    public string? SourceModule { get; set; }
    public string? EntryType { get; set; }
}

public sealed class MemberLoanRequestFilterParams : PaginationParams
{
    public string? Status { get; set; }
    public long? MemberId { get; set; }
}

public sealed class SavingsWithdrawalRequestFilterParams : PaginationParams
{
    public string? Status { get; set; }
    public long? MemberId { get; set; }
}

public sealed record ApproveMemberLoanRequest(
    string LoanNo,
    DateOnly? LoanDate,
    decimal? FlatInterestRatePct,
    int? TermMonths,
    decimal? AdminFeeAmount,
    decimal? PenaltyAmount,
    string? ReviewerNote);

public sealed record RejectMemberLoanRequest(
    string ReviewerNote);

public sealed record ApproveSavingsWithdrawalRequest(
    string TransactionNo,
    DateTime? TransactionTs,
    string? ReviewerNote);

public sealed record RejectSavingsWithdrawalRequest(
    string ReviewerNote);

public sealed class CreateMemberLoanRequestValidator : AbstractValidator<CreateMemberLoanRequest>
{
    public CreateMemberLoanRequestValidator()
    {
        RuleFor(x => x.LoanProductId).GreaterThan(0);
        RuleFor(x => x.PrincipalAmount).GreaterThan(0);
        RuleFor(x => x.ProposedTermMonths)
            .GreaterThan(0)
            .When(x => x.ProposedTermMonths.HasValue);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class CreateSavingsWithdrawalRequestValidator : AbstractValidator<CreateSavingsWithdrawalRequest>
{
    public CreateSavingsWithdrawalRequestValidator()
    {
        RuleFor(x => x.SavingsProductId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class ApproveMemberLoanRequestValidator : AbstractValidator<ApproveMemberLoanRequest>
{
    public ApproveMemberLoanRequestValidator()
    {
        RuleFor(x => x.LoanNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TermMonths)
            .GreaterThan(0)
            .When(x => x.TermMonths.HasValue);
        RuleFor(x => x.FlatInterestRatePct)
            .GreaterThanOrEqualTo(0)
            .When(x => x.FlatInterestRatePct.HasValue);
        RuleFor(x => x.AdminFeeAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AdminFeeAmount.HasValue);
        RuleFor(x => x.PenaltyAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PenaltyAmount.HasValue);
        RuleFor(x => x.ReviewerNote).MaximumLength(500);
    }
}

public sealed class RejectMemberLoanRequestValidator : AbstractValidator<RejectMemberLoanRequest>
{
    public RejectMemberLoanRequestValidator()
    {
        RuleFor(x => x.ReviewerNote).NotEmpty().MaximumLength(500);
    }
}

public sealed class ApproveSavingsWithdrawalRequestValidator : AbstractValidator<ApproveSavingsWithdrawalRequest>
{
    public ApproveSavingsWithdrawalRequestValidator()
    {
        RuleFor(x => x.TransactionNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ReviewerNote).MaximumLength(500);
    }
}

public sealed class RejectSavingsWithdrawalRequestValidator : AbstractValidator<RejectSavingsWithdrawalRequest>
{
    public RejectSavingsWithdrawalRequestValidator()
    {
        RuleFor(x => x.ReviewerNote).NotEmpty().MaximumLength(500);
    }
}
