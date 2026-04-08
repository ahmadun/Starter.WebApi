using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class ReportingRepository : BaseRepository<object>, IReportingRepository
{
    public ReportingRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<CooperativeDashboardDto> GetDashboardAsync(long tenantId)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM coop.members WHERE tenant_id = @TenantId) AS TotalMembers,
                (SELECT COUNT(*) FROM coop.members WHERE tenant_id = @TenantId AND member_status = 'active') AS ActiveMembers,
                COALESCE((
                    SELECT SUM(CASE mt.entry_type WHEN 'credit' THEN st.amount ELSE -st.amount END)
                    FROM coop.savings_transactions st
                    INNER JOIN coop.member_transactions mt ON mt.member_transaction_id = st.member_transaction_id
                    WHERE st.tenant_id = @TenantId
                ), 0) AS TotalSavingsBalance,
                COALESCE((
                    SELECT SUM(outstanding_total_amount)
                    FROM coop.loans
                    WHERE tenant_id = @TenantId
                      AND status IN ('approved', 'active', 'defaulted')
                ), 0) AS OutstandingLoanAmount,
                (SELECT COUNT(*) FROM coop.loans WHERE tenant_id = @TenantId AND status IN ('approved', 'active', 'defaulted')) AS ActiveLoanCount,
                COALESCE((
                    SELECT SUM(total_amount)
                    FROM coop.sales
                    WHERE tenant_id = @TenantId
                      AND sale_status = 'posted'
                      AND CAST(sale_ts AS date) = CAST(SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time' AS date)
                ), 0) AS SalesTodayAmount,
                (SELECT COUNT(*) FROM coop.sales WHERE tenant_id = @TenantId AND sale_status = 'posted' AND CAST(sale_ts AS date) = CAST(SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time' AS date)) AS SalesTodayCount,
                COALESCE((
                    SELECT SUM(CASE entry_type WHEN 'debit' THEN amount ELSE -amount END)
                    FROM coop.member_transactions
                    WHERE tenant_id = @TenantId
                      AND source_module = 'pos'
                ), 0) AS MemberCreditOutstandingAmount,
                (SELECT COUNT(*) FROM coop.product_stocks ps WHERE ps.tenant_id = @TenantId AND ps.on_hand_qty <= ps.min_stock_qty) AS LowStockProductCount
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleAsync<CooperativeDashboardDto>(sql, new { TenantId = tenantId });
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(long tenantId, DateOnly dateFrom, DateOnly dateTo)
    {
        using var connection = CreateConnection();

        var summary = await connection.QuerySingleAsync<SalesSummaryRow>(
            """
            SELECT
                @DateFrom AS DateFrom,
                @DateTo AS DateTo,
                COUNT(*) AS TotalSalesCount,
                COALESCE(SUM(total_amount), 0) AS TotalSalesAmount,
                COALESCE(SUM(CASE WHEN sale_type = 'cash' THEN total_amount ELSE 0 END), 0) AS TotalCashSalesAmount,
                COALESCE(SUM(CASE WHEN sale_type = 'member_credit' THEN total_amount ELSE 0 END), 0) AS TotalMemberCreditSalesAmount,
                COALESCE(SUM(discount_amount), 0) AS TotalDiscountAmount,
                COALESCE(AVG(CAST(total_amount AS decimal(18,2))), 0) AS AverageSaleAmount
            FROM coop.sales
            WHERE tenant_id = @TenantId
              AND sale_status = 'posted'
              AND CAST(sale_ts AS date) BETWEEN @DateFrom AND @DateTo
            """,
            new { TenantId = tenantId, DateFrom = dateFrom, DateTo = dateTo });

        var dailyRows = await connection.QueryAsync<DailySalesSummaryDto>(
            """
            SELECT
                CAST(sale_ts AS date) AS SaleDate,
                COUNT(*) AS TotalSalesCount,
                COALESCE(SUM(total_amount), 0) AS TotalSalesAmount,
                COALESCE(SUM(CASE WHEN sale_type = 'cash' THEN total_amount ELSE 0 END), 0) AS CashSalesAmount,
                COALESCE(SUM(CASE WHEN sale_type = 'member_credit' THEN total_amount ELSE 0 END), 0) AS MemberCreditSalesAmount
            FROM coop.sales
            WHERE tenant_id = @TenantId
              AND sale_status = 'posted'
              AND CAST(sale_ts AS date) BETWEEN @DateFrom AND @DateTo
            GROUP BY CAST(sale_ts AS date)
            ORDER BY SaleDate
            """,
            new { TenantId = tenantId, DateFrom = dateFrom, DateTo = dateTo });

        return new SalesSummaryDto(
            summary.DateFrom,
            summary.DateTo,
            summary.TotalSalesCount,
            summary.TotalSalesAmount,
            summary.TotalCashSalesAmount,
            summary.TotalMemberCreditSalesAmount,
            summary.TotalDiscountAmount,
            summary.AverageSaleAmount,
            dailyRows.ToList());
    }

    public async Task<(IEnumerable<MemberBalanceItemDto> Items, int TotalCount, decimal TotalSavingsBalance, decimal TotalOutstandingLoanAmount, decimal TotalMemberCreditOutstandingAmount)> GetMemberBalanceSummaryAsync(long tenantId, MemberBalanceFilterParams filters)
    {
        const string baseSql = """
            SELECT
                m.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.employee_code AS EmployeeCode,
                m.full_name AS FullName,
                COALESCE(savings.savings_balance, 0) AS SavingsBalance,
                COALESCE(loans.outstanding_loan_amount, 0) AS OutstandingLoanAmount,
                COALESCE(pos.member_credit_outstanding_amount, 0) AS MemberCreditOutstandingAmount,
                COALESCE(savings.savings_balance, 0) - COALESCE(loans.outstanding_loan_amount, 0) - COALESCE(pos.member_credit_outstanding_amount, 0) AS NetPositionAmount
            FROM coop.members m
            OUTER APPLY (
                SELECT SUM(CASE mt.entry_type WHEN 'credit' THEN st.amount ELSE -st.amount END) AS savings_balance
                FROM coop.savings_transactions st
                INNER JOIN coop.member_transactions mt ON mt.member_transaction_id = st.member_transaction_id
                WHERE st.tenant_id = m.tenant_id
                  AND st.member_id = m.member_id
            ) savings
            OUTER APPLY (
                SELECT SUM(l.outstanding_total_amount) AS outstanding_loan_amount
                FROM coop.loans l
                WHERE l.tenant_id = m.tenant_id
                  AND l.member_id = m.member_id
                  AND l.status IN ('approved', 'active', 'defaulted')
            ) loans
            OUTER APPLY (
                SELECT SUM(CASE mt.entry_type WHEN 'debit' THEN mt.amount ELSE -mt.amount END) AS member_credit_outstanding_amount
                FROM coop.member_transactions mt
                WHERE mt.tenant_id = m.tenant_id
                  AND mt.member_id = m.member_id
                  AND mt.source_module = 'pos'
            ) pos
            WHERE m.tenant_id = @TenantId
              AND (
                    COALESCE(savings.savings_balance, 0) <> 0
                 OR COALESCE(loans.outstanding_loan_amount, 0) <> 0
                 OR COALESCE(pos.member_credit_outstanding_amount, 0) <> 0
              )
            """;

        var parameters = new DynamicParameters(new { TenantId = tenantId, Offset = filters.Offset, PageSize = filters.PageSize });

        using var connection = CreateConnection();
        var summary = await connection.QuerySingleAsync<MemberBalanceSummaryRow>($"""
            SELECT
                COUNT(*) AS TotalCount,
                COALESCE(SUM(SavingsBalance), 0) AS TotalSavingsBalance,
                COALESCE(SUM(OutstandingLoanAmount), 0) AS TotalOutstandingLoanAmount,
                COALESCE(SUM(MemberCreditOutstandingAmount), 0) AS TotalMemberCreditOutstandingAmount
            FROM (
                {baseSql}
            ) balances
            """, parameters);

        var items = await connection.QueryAsync<MemberBalanceItemDto>($"""
            SELECT *
            FROM (
                {baseSql}
            ) balances
            ORDER BY FullName, MemberNo
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);

        return (items, summary.TotalCount, summary.TotalSavingsBalance, summary.TotalOutstandingLoanAmount, summary.TotalMemberCreditOutstandingAmount);
    }

    public async Task<(IEnumerable<LowStockProductDto> Items, int TotalCount)> GetLowStockProductsAsync(long tenantId, LowStockFilterParams filters)
    {
        var where = new List<string> { "p.tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, filters.Offset, filters.PageSize });

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(p.sku LIKE @Search OR p.barcode LIKE @Search OR p.product_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }

        if (filters.OnlyBelowMinimum)
        {
            where.Add("ps.on_hand_qty <= ps.min_stock_qty");
        }

        var whereClause = string.Join(" AND ", where);

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM coop.products p
            INNER JOIN coop.product_stocks ps ON ps.product_id = p.product_id AND ps.tenant_id = p.tenant_id
            WHERE {whereClause}
            """, parameters);

        var items = await connection.QueryAsync<LowStockProductDto>($"""
            SELECT
                p.product_id AS ProductId,
                p.sku AS Sku,
                p.barcode AS Barcode,
                p.product_name AS ProductName,
                p.unit_name AS UnitName,
                ps.on_hand_qty AS OnHandQty,
                ps.min_stock_qty AS MinStockQty,
                CASE WHEN ps.on_hand_qty <= ps.min_stock_qty THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsBelowMinimum
            FROM coop.products p
            INNER JOIN coop.product_stocks ps ON ps.product_id = p.product_id AND ps.tenant_id = p.tenant_id
            WHERE {whereClause}
            ORDER BY IsBelowMinimum DESC, ps.on_hand_qty ASC, p.product_name
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);

        return (items, totalCount);
    }

    private sealed record MemberBalanceSummaryRow(
        int TotalCount,
        decimal TotalSavingsBalance,
        decimal TotalOutstandingLoanAmount,
        decimal TotalMemberCreditOutstandingAmount);

    private sealed record SalesSummaryRow(
        DateOnly DateFrom,
        DateOnly DateTo,
        int TotalSalesCount,
        decimal TotalSalesAmount,
        decimal TotalCashSalesAmount,
        decimal TotalMemberCreditSalesAmount,
        decimal TotalDiscountAmount,
        decimal AverageSaleAmount);
}

