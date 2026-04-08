using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class SavingsProductRepository : BaseRepository<SavingsProduct>, ISavingsProductRepository
{
    public SavingsProductRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<(IEnumerable<SavingsProduct> Items, int TotalCount)> GetAllAsync(long tenantId, SavingsProductFilterParams filters)
    {
        var where = "tenant_id = @TenantId" + (string.IsNullOrWhiteSpace(filters.Search) ? string.Empty : " AND (product_code LIKE @Search OR product_name LIKE @Search)");
        using var connection = CreateConnection();
        var parameters = new { TenantId = tenantId, Search = $"%{filters.Search?.Trim()}%", Offset = filters.Offset, PageSize = filters.PageSize };
        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM coop.savings_products WHERE {where}", parameters);
        var items = await connection.QueryAsync<SavingsProduct>($"""
            SELECT savings_product_id, tenant_id, product_code, product_name, savings_kind, periodicity, default_amount, is_active, created_at, updated_at
            FROM coop.savings_products
            WHERE {where}
            ORDER BY savings_product_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public Task<SavingsProduct?> GetByIdAsync(long tenantId, long savingsProductId) => QuerySingleOrDefaultAsync(
        """
        SELECT savings_product_id, tenant_id, product_code, product_name, savings_kind, periodicity, default_amount, is_active, created_at, updated_at
        FROM coop.savings_products
        WHERE tenant_id = @TenantId AND savings_product_id = @SavingsProductId
        """, new { TenantId = tenantId, SavingsProductId = savingsProductId });

    public async Task<bool> ProductCodeExistsAsync(long tenantId, string productCode, long? excludeId = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM coop.savings_products WHERE tenant_id = @TenantId AND product_code = @ProductCode AND (@ExcludeId IS NULL OR savings_product_id <> @ExcludeId)",
            new { TenantId = tenantId, ProductCode = productCode, ExcludeId = excludeId }) > 0;
    }

    public Task<long> CreateAsync(SavingsProduct savingsProduct) => ExecuteScalarAsync<long>(
        """
        INSERT INTO coop.savings_products (tenant_id, product_code, product_name, savings_kind, periodicity, default_amount, is_active, created_at, updated_at)
        VALUES (@TenantId, @ProductCode, @ProductName, @SavingsKind, @Periodicity, @DefaultAmount, @IsActive, @CreatedAt, @UpdatedAt);
        SELECT CAST(SCOPE_IDENTITY() AS bigint);
        """, savingsProduct)!;

    public async Task<bool> UpdateAsync(SavingsProduct savingsProduct)
        => await ExecuteAsync(
            """
            UPDATE coop.savings_products
            SET product_code = @ProductCode, product_name = @ProductName, savings_kind = @SavingsKind,
                periodicity = @Periodicity, default_amount = @DefaultAmount, is_active = @IsActive, updated_at = @UpdatedAt
            WHERE tenant_id = @TenantId AND savings_product_id = @SavingsProductId
            """, savingsProduct) > 0;
}
