using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class LoanProductRepository : BaseRepository<LoanProduct>, ILoanProductRepository
{
    public LoanProductRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<(IEnumerable<LoanProduct> Items, int TotalCount)> GetAllAsync(long tenantId, LoanProductFilterParams filters)
    {
        var where = "tenant_id = @TenantId" + (string.IsNullOrWhiteSpace(filters.Search) ? string.Empty : " AND (product_code LIKE @Search OR product_name LIKE @Search)");
        using var connection = CreateConnection();
        var parameters = new { TenantId = tenantId, Search = $"%{filters.Search?.Trim()}%", Offset = filters.Offset, PageSize = filters.PageSize };
        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM coop.loan_products WHERE {where}", parameters);
        var items = await connection.QueryAsync<LoanProduct>($"""
            SELECT loan_product_id, tenant_id, product_code, product_name, default_flat_interest_rate_pct, min_flat_interest_rate_pct, max_flat_interest_rate_pct,
                   default_term_months, min_term_months, max_term_months, min_principal_amount, max_principal_amount,
                   default_admin_fee_amount, default_penalty_amount, is_active, created_at, updated_at
            FROM coop.loan_products
            WHERE {where}
            ORDER BY loan_product_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public Task<LoanProduct?> GetByIdAsync(long tenantId, long loanProductId) => QuerySingleOrDefaultAsync(
        """
        SELECT loan_product_id, tenant_id, product_code, product_name, default_flat_interest_rate_pct, min_flat_interest_rate_pct, max_flat_interest_rate_pct,
               default_term_months, min_term_months, max_term_months, min_principal_amount, max_principal_amount,
               default_admin_fee_amount, default_penalty_amount, is_active, created_at, updated_at
        FROM coop.loan_products
        WHERE tenant_id = @TenantId AND loan_product_id = @LoanProductId
        """, new { TenantId = tenantId, LoanProductId = loanProductId });

    public async Task<bool> ProductCodeExistsAsync(long tenantId, string productCode, long? excludeId = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM coop.loan_products WHERE tenant_id = @TenantId AND product_code = @ProductCode AND (@ExcludeId IS NULL OR loan_product_id <> @ExcludeId)",
            new { TenantId = tenantId, ProductCode = productCode, ExcludeId = excludeId }) > 0;
    }

    public Task<long> CreateAsync(LoanProduct loanProduct) => ExecuteScalarAsync<long>(
        """
        INSERT INTO coop.loan_products (tenant_id, product_code, product_name, default_flat_interest_rate_pct, min_flat_interest_rate_pct, max_flat_interest_rate_pct,
            default_term_months, min_term_months, max_term_months, min_principal_amount, max_principal_amount,
            default_admin_fee_amount, default_penalty_amount, is_active, created_at, updated_at)
        VALUES (@TenantId, @ProductCode, @ProductName, @DefaultFlatInterestRatePct, @MinFlatInterestRatePct, @MaxFlatInterestRatePct,
            @DefaultTermMonths, @MinTermMonths, @MaxTermMonths, @MinPrincipalAmount, @MaxPrincipalAmount,
            @DefaultAdminFeeAmount, @DefaultPenaltyAmount, @IsActive, @CreatedAt, @UpdatedAt);
        SELECT CAST(SCOPE_IDENTITY() AS bigint);
        """, loanProduct)!;

    public async Task<bool> UpdateAsync(LoanProduct loanProduct)
        => await ExecuteAsync(
            """
            UPDATE coop.loan_products
            SET product_code = @ProductCode, product_name = @ProductName, default_flat_interest_rate_pct = @DefaultFlatInterestRatePct,
                min_flat_interest_rate_pct = @MinFlatInterestRatePct, max_flat_interest_rate_pct = @MaxFlatInterestRatePct,
                default_term_months = @DefaultTermMonths, min_term_months = @MinTermMonths, max_term_months = @MaxTermMonths,
                min_principal_amount = @MinPrincipalAmount, max_principal_amount = @MaxPrincipalAmount,
                default_admin_fee_amount = @DefaultAdminFeeAmount, default_penalty_amount = @DefaultPenaltyAmount,
                is_active = @IsActive, updated_at = @UpdatedAt
            WHERE tenant_id = @TenantId AND loan_product_id = @LoanProductId
            """, loanProduct) > 0;
}
