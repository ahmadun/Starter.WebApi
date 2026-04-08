using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface IReportingRepository
{
    Task<CooperativeDashboardDto> GetDashboardAsync(long tenantId);
    Task<SalesSummaryDto> GetSalesSummaryAsync(long tenantId, DateOnly dateFrom, DateOnly dateTo);
    Task<(IEnumerable<MemberBalanceItemDto> Items, int TotalCount, decimal TotalSavingsBalance, decimal TotalOutstandingLoanAmount, decimal TotalMemberCreditOutstandingAmount)> GetMemberBalanceSummaryAsync(long tenantId, MemberBalanceFilterParams filters);
    Task<(IEnumerable<LowStockProductDto> Items, int TotalCount)> GetLowStockProductsAsync(long tenantId, LowStockFilterParams filters);
}

