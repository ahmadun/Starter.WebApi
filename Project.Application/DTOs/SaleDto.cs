using FluentValidation;
using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record SaleItemRequest(long ProductId, decimal Quantity, decimal UnitPrice, decimal LineDiscountAmount);
public sealed record CreateSaleRequest(long TenantId, string SaleNo, string? ReceiptNo, DateTime SaleTs, long? MemberId, string SaleType, decimal DiscountAmount, decimal PaidAmount, string? Note, IReadOnlyCollection<SaleItemRequest> Items);
public sealed record SaleDto(long SaleId, long TenantId, string SaleNo, string? ReceiptNo, DateTime SaleTs, long? MemberId, string SaleType, decimal TotalAmount, decimal PaidAmount, decimal ChangeAmount, string? Note);
public sealed record SaleReceiptItemDto(long ProductId, string ProductName, string UnitName, decimal Quantity, decimal UnitPrice, decimal LineDiscountAmount, decimal LineTotalAmount);
public sealed record SaleReceiptDto(long SaleId, long TenantId, string SaleNo, string? ReceiptNo, DateTime SaleTs, long? MemberId, string? MemberNo, string? MemberName, string CashierDisplayName, string SaleType, string SaleStatus, decimal SubtotalAmount, decimal DiscountAmount, decimal TotalAmount, decimal PaidAmount, decimal ChangeAmount, string? Note, IReadOnlyCollection<SaleReceiptItemDto> Items);
public sealed record ConvertMemberCreditSaleRequest(long TenantId, long LoanProductId, string LoanNo, DateOnly? LoanDate, decimal? FlatInterestRatePct, int? TermMonths, decimal? AdminFeeAmount, decimal? PenaltyAmount, string? Note);
public sealed record MemberCreditConversionDto(long MemberCreditConversionId, long SaleId, string SaleNo, long LoanId, string LoanNo, DateOnly LoanDate, decimal PrincipalAmount, decimal TotalPayableAmount, string? Note);

public sealed class SaleFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public long? MemberId { get; set; }
    public string? SaleType { get; set; }
}

public sealed class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.SaleNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SaleType).Must(v => new[] { "cash", "member_credit" }.Contains(v));
        RuleFor(x => x.Items).NotEmpty();
    }
}

public sealed class ConvertMemberCreditSaleRequestValidator : AbstractValidator<ConvertMemberCreditSaleRequest>
{
    public ConvertMemberCreditSaleRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.LoanProductId).GreaterThan(0);
        RuleFor(x => x.LoanNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FlatInterestRatePct).GreaterThanOrEqualTo(0).When(x => x.FlatInterestRatePct.HasValue);
        RuleFor(x => x.TermMonths).GreaterThan(0).When(x => x.TermMonths.HasValue);
        RuleFor(x => x.AdminFeeAmount).GreaterThanOrEqualTo(0).When(x => x.AdminFeeAmount.HasValue);
        RuleFor(x => x.PenaltyAmount).GreaterThanOrEqualTo(0).When(x => x.PenaltyAmount.HasValue);
    }
}
