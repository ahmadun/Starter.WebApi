using FluentValidation;
using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record LoanProductDto(
    long LoanProductId,
    long TenantId,
    string ProductCode,
    string ProductName,
    decimal DefaultFlatInterestRatePct,
    decimal? MinFlatInterestRatePct,
    decimal? MaxFlatInterestRatePct,
    int DefaultTermMonths,
    int? MinTermMonths,
    int? MaxTermMonths,
    decimal? MinPrincipalAmount,
    decimal? MaxPrincipalAmount,
    decimal DefaultAdminFeeAmount,
    decimal DefaultPenaltyAmount,
    bool IsActive);

public sealed record SaveLoanProductRequest(
    long TenantId,
    string ProductCode,
    string ProductName,
    decimal DefaultFlatInterestRatePct,
    decimal? MinFlatInterestRatePct,
    decimal? MaxFlatInterestRatePct,
    int DefaultTermMonths,
    int? MinTermMonths,
    int? MaxTermMonths,
    decimal? MinPrincipalAmount,
    decimal? MaxPrincipalAmount,
    decimal DefaultAdminFeeAmount,
    decimal DefaultPenaltyAmount,
    bool IsActive);

public sealed class LoanProductFilterParams : PaginationParams
{
    public string? Search { get; set; }
}

public sealed class SaveLoanProductRequestValidator : AbstractValidator<SaveLoanProductRequest>
{
    public SaveLoanProductRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DefaultFlatInterestRatePct).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DefaultTermMonths).GreaterThan(0);
    }
}
