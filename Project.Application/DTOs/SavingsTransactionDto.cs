using FluentValidation;
using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record SavingsAccountDto(
    long MemberSavingsAccountId,
    long TenantId,
    long MemberId,
    string MemberNo,
    string FullName,
    long SavingsProductId,
    string ProductCode,
    string ProductName,
    string SavingsKind,
    string Periodicity,
    DateTime OpenedAt,
    string AccountStatus,
    decimal BalanceAmount,
    DateTime? LastTransactionAt);

public sealed record SavingsTransactionDto(
    long SavingsTransactionId,
    long TenantId,
    long MemberSavingsAccountId,
    long MemberId,
    string MemberNo,
    string FullName,
    long SavingsProductId,
    string ProductCode,
    string ProductName,
    string SavingsKind,
    string TransactionNo,
    DateTime TransactionTs,
    string TransactionType,
    string EntryType,
    decimal Amount,
    short? PeriodYear,
    byte? PeriodMonth,
    string? Note);

public sealed record CreateSavingsTransactionRequest(
    long TenantId,
    long MemberId,
    long SavingsProductId,
    string TransactionNo,
    DateTime TransactionTs,
    string TransactionType,
    string? EntryType,
    decimal Amount,
    short? PeriodYear,
    byte? PeriodMonth,
    string? Note);

public sealed record SavingsTransactionContextDto(
    long TenantId,
    long MemberId,
    string MemberStatus,
    string MemberNo,
    string FullName,
    long SavingsProductId,
    string ProductCode,
    string ProductName,
    string SavingsKind,
    string Periodicity,
    bool ProductIsActive,
    long? MemberSavingsAccountId);

public sealed class SavingsAccountFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public long? MemberId { get; set; }
    public long? SavingsProductId { get; set; }
    public string? AccountStatus { get; set; }
}

public sealed class SavingsTransactionFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public long? MemberId { get; set; }
    public long? SavingsProductId { get; set; }
    public string? TransactionType { get; set; }
    public string? EntryType { get; set; }
}

public sealed class CreateSavingsTransactionRequestValidator : AbstractValidator<CreateSavingsTransactionRequest>
{
    private static readonly string[] AllowedTransactionTypes = ["deposit", "withdrawal", "adjustment"];
    private static readonly string[] AllowedEntryTypes = ["credit", "debit"];

    public CreateSavingsTransactionRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.MemberId).GreaterThan(0);
        RuleFor(x => x.SavingsProductId).GreaterThan(0);
        RuleFor(x => x.TransactionNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TransactionType).Must(AllowedTransactionTypes.Contains);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PeriodMonth)
            .InclusiveBetween((byte)1, (byte)12)
            .When(x => x.PeriodMonth.HasValue);
        RuleFor(x => x.EntryType)
            .Must(value => string.IsNullOrWhiteSpace(value) || AllowedEntryTypes.Contains(value))
            .WithMessage("Entry type must be credit or debit.");
        RuleFor(x => x.EntryType)
            .NotEmpty()
            .When(x => x.TransactionType == "adjustment")
            .WithMessage("Entry type is required for adjustment transactions.");
    }
}
