using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Application.Services;

public sealed class ReportingService : IReportingService
{
    private readonly IReportingRepository _repository;

    public ReportingService(IReportingRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<CooperativeDashboardDto>> GetDashboardAsync(long tenantId)
        => ApiResponse<CooperativeDashboardDto>.Ok(await _repository.GetDashboardAsync(tenantId));

    public async Task<ApiResponse<SalesSummaryDto>> GetSalesSummaryAsync(long tenantId, ReportingPeriodFilter filter)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dateTo = filter.DateTo ?? today;
        var dateFrom = filter.DateFrom ?? dateTo.AddDays(-29);

        if (dateFrom > dateTo)
            return ApiResponse<SalesSummaryDto>.Fail("DateFrom must be earlier than or equal to DateTo.");

        return ApiResponse<SalesSummaryDto>.Ok(await _repository.GetSalesSummaryAsync(tenantId, dateFrom, dateTo));
    }

    public async Task<ApiResponse<MemberBalanceSummaryDto>> GetMemberBalanceSummaryAsync(long tenantId, MemberBalanceFilterParams filters)
    {
        var (items, totalCount, totalSavingsBalance, totalOutstandingLoanAmount, totalMemberCreditOutstandingAmount) =
            await _repository.GetMemberBalanceSummaryAsync(tenantId, filters);

        return ApiResponse<MemberBalanceSummaryDto>.Ok(new MemberBalanceSummaryDto(
            totalCount,
            totalSavingsBalance,
            totalOutstandingLoanAmount,
            totalMemberCreditOutstandingAmount,
            PagedResult<MemberBalanceItemDto>.Create(items, totalCount, filters)));
    }

    public async Task<ApiResponse<PagedResult<LowStockProductDto>>> GetLowStockProductsAsync(long tenantId, LowStockFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetLowStockProductsAsync(tenantId, filters);
        return ApiResponse<PagedResult<LowStockProductDto>>.Ok(PagedResult<LowStockProductDto>.Create(items, totalCount, filters));
    }
}
