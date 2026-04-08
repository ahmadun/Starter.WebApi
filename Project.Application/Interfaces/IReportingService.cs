using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface IReportingService
{
    Task<ApiResponse<CooperativeDashboardDto>> GetDashboardAsync(long tenantId);
    Task<ApiResponse<SalesSummaryDto>> GetSalesSummaryAsync(long tenantId, ReportingPeriodFilter filter);
    Task<ApiResponse<MemberBalanceSummaryDto>> GetMemberBalanceSummaryAsync(long tenantId, MemberBalanceFilterParams filters);
    Task<ApiResponse<PagedResult<LowStockProductDto>>> GetLowStockProductsAsync(long tenantId, LowStockFilterParams filters);
}

