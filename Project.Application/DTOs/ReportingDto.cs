using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed class ReportingPeriodFilter
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

public sealed class LowStockFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public bool OnlyBelowMinimum { get; set; } = true;
}

public sealed class MemberBalanceFilterParams : PaginationParams
{
}

public sealed record CooperativeDashboardDto(
    int TotalMembers,
    int ActiveMembers,
    decimal TotalSavingsBalance,
    decimal OutstandingLoanAmount,
    int ActiveLoanCount,
    decimal SalesTodayAmount,
    int SalesTodayCount,
    decimal MemberCreditOutstandingAmount,
    int LowStockProductCount);

public sealed record SalesSummaryDto(
    DateOnly DateFrom,
    DateOnly DateTo,
    int TotalSalesCount,
    decimal TotalSalesAmount,
    decimal TotalCashSalesAmount,
    decimal TotalMemberCreditSalesAmount,
    decimal TotalDiscountAmount,
    decimal AverageSaleAmount,
    IReadOnlyCollection<DailySalesSummaryDto> DailySummaries);

public sealed record DailySalesSummaryDto(
    DateOnly SaleDate,
    int TotalSalesCount,
    decimal TotalSalesAmount,
    decimal CashSalesAmount,
    decimal MemberCreditSalesAmount);

public sealed record MemberBalanceSummaryDto(
    int MemberCountWithTransactions,
    decimal TotalSavingsBalance,
    decimal TotalOutstandingLoanAmount,
    decimal TotalMemberCreditOutstandingAmount,
    PagedResult<MemberBalanceItemDto> Members);

public sealed record MemberBalanceItemDto(
    long MemberId,
    string MemberNo,
    string? EmployeeCode,
    string FullName,
    decimal SavingsBalance,
    decimal OutstandingLoanAmount,
    decimal MemberCreditOutstandingAmount,
    decimal NetPositionAmount);

public sealed record LowStockProductDto(
    long ProductId,
    string Sku,
    string? Barcode,
    string ProductName,
    string UnitName,
    decimal OnHandQty,
    decimal MinStockQty,
    bool IsBelowMinimum);
