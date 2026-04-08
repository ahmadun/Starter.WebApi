using FluentValidation;
using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record SavingsProductDto(
    long SavingsProductId,
    long TenantId,
    string ProductCode,
    string ProductName,
    string SavingsKind,
    string Periodicity,
    decimal? DefaultAmount,
    bool IsActive);

public sealed record SaveSavingsProductRequest(
    long TenantId,
    string ProductCode,
    string ProductName,
    string SavingsKind,
    string Periodicity,
    decimal? DefaultAmount,
    bool IsActive);

public sealed class SavingsProductFilterParams : PaginationParams
{
    public string? Search { get; set; }
}

public sealed class SaveSavingsProductRequestValidator : AbstractValidator<SaveSavingsProductRequest>
{
    public SaveSavingsProductRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SavingsKind).Must(v => new[] { "pokok", "wajib", "sukarela" }.Contains(v));
        RuleFor(x => x.Periodicity).Must(v => new[] { "once", "monthly", "flexible" }.Contains(v));
    }
}
