using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class SaleRepository : BaseRepository<Sale>, ISaleRepository
{
    public SaleRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<(IEnumerable<Sale> Items, int TotalCount)> GetAllAsync(long tenantId, SaleFilterParams filters)
    {
        var where = new List<string> { "tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, Offset = filters.Offset, PageSize = filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(sale_no LIKE @Search OR receipt_no LIKE @Search OR note LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (filters.MemberId.HasValue)
        {
            where.Add("member_id = @MemberId");
            parameters.Add("MemberId", filters.MemberId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filters.SaleType))
        {
            where.Add("sale_type = @SaleType");
            parameters.Add("SaleType", filters.SaleType);
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM coop.sales WHERE {whereClause}", parameters);
        var items = await connection.QueryAsync<Sale>($"""
            SELECT sale_id, tenant_id, sale_no, receipt_no, sale_ts, member_id, cashier_user_id, member_transaction_id, sale_type, sale_status,
                   subtotal_amount, discount_amount, total_amount, paid_amount, change_amount, note, created_at
            FROM coop.sales
            WHERE {whereClause}
            ORDER BY sale_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public Task<Sale?> GetByIdAsync(long tenantId, long saleId) => QuerySingleOrDefaultAsync(
        """
        SELECT sale_id, tenant_id, sale_no, receipt_no, sale_ts, member_id, cashier_user_id, member_transaction_id, sale_type, sale_status,
               subtotal_amount, discount_amount, total_amount, paid_amount, change_amount, note, created_at
        FROM coop.sales
        WHERE tenant_id = @TenantId AND sale_id = @SaleId
        """, new { TenantId = tenantId, SaleId = saleId });

    public async Task<SaleReceiptDto?> GetReceiptAsync(long tenantId, long saleId)
    {
        using var connection = CreateConnection();
        var header = await connection.QuerySingleOrDefaultAsync<SaleReceiptHeaderRow>(
            """
            SELECT
                s.sale_id AS SaleId,
                s.tenant_id AS TenantId,
                s.sale_no AS SaleNo,
                s.receipt_no AS ReceiptNo,
                s.sale_ts AS SaleTs,
                s.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS MemberName,
                u.display_name AS CashierDisplayName,
                s.sale_type AS SaleType,
                s.sale_status AS SaleStatus,
                s.subtotal_amount AS SubtotalAmount,
                s.discount_amount AS DiscountAmount,
                s.total_amount AS TotalAmount,
                s.paid_amount AS PaidAmount,
                s.change_amount AS ChangeAmount,
                s.note AS Note
            FROM coop.sales s
            INNER JOIN coop.users u ON u.user_id = s.cashier_user_id
            LEFT JOIN coop.members m ON m.member_id = s.member_id
            WHERE s.tenant_id = @TenantId
              AND s.sale_id = @SaleId
            """,
            new { TenantId = tenantId, SaleId = saleId });

        if (header is null)
            return null;

        var items = await connection.QueryAsync<SaleReceiptItemDto>(
            """
            SELECT
                si.product_id AS ProductId,
                p.product_name AS ProductName,
                p.unit_name AS UnitName,
                si.quantity AS Quantity,
                si.unit_price AS UnitPrice,
                si.line_discount_amount AS LineDiscountAmount,
                si.line_total_amount AS LineTotalAmount
            FROM coop.sale_items si
            INNER JOIN coop.products p ON p.product_id = si.product_id
            WHERE si.sale_id = @SaleId
            ORDER BY si.sale_item_id
            """,
            new { SaleId = saleId });

        return new SaleReceiptDto(
            header.SaleId,
            header.TenantId,
            header.SaleNo,
            header.ReceiptNo,
            header.SaleTs,
            header.MemberId,
            header.MemberNo,
            header.MemberName,
            header.CashierDisplayName,
            header.SaleType,
            header.SaleStatus,
            header.SubtotalAmount,
            header.DiscountAmount,
            header.TotalAmount,
            header.PaidAmount,
            header.ChangeAmount,
            header.Note,
            items.ToList());
    }

    public async Task<bool> SaleNoExistsAsync(long tenantId, string saleNo)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM coop.sales WHERE tenant_id = @TenantId AND sale_no = @SaleNo", new { TenantId = tenantId, SaleNo = saleNo }) > 0;
    }

    public async Task<bool> HasMemberCreditConversionAsync(long tenantId, long saleId)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1)
            FROM coop.member_credit_conversions mcc
            INNER JOIN coop.sales s ON s.sale_id = mcc.sale_id
            WHERE s.tenant_id = @TenantId
              AND mcc.sale_id = @SaleId
            """,
            new { TenantId = tenantId, SaleId = saleId }) > 0;
    }

    public async Task<long> CreateAsync(long userId, CreateSaleRequest request)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        var subtotal = request.Items.Sum(x => x.Quantity * x.UnitPrice - x.LineDiscountAmount);
        var total = subtotal - request.DiscountAmount;
        var change = request.SaleType == "cash" ? Math.Max(request.PaidAmount - total, 0) : 0;
        long? memberTransactionId = null;

        if (request.SaleType == "member_credit" && request.MemberId.HasValue)
        {
            memberTransactionId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO coop.member_transactions (tenant_id, member_id, transaction_no, transaction_ts, source_module, source_table, source_id, entry_type, amount, description, created_by_user_id, created_at)
                VALUES (@TenantId, @MemberId, @TransactionNo, @TransactionTs, 'pos', 'sales', 0, 'debit', @Amount, @Description, @CreatedByUserId, sysutcdatetime());
                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """,
                new
                {
                    request.TenantId,
                    MemberId = request.MemberId.Value,
                    TransactionNo = $"POS-{request.SaleNo.Trim()}",
                    TransactionTs = request.SaleTs,
                    Amount = total,
                    Description = $"Member credit sale {request.SaleNo.Trim()}",
                    CreatedByUserId = userId
                },
                transaction);
        }

        var saleId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.sales (tenant_id, sale_no, receipt_no, sale_ts, member_id, cashier_user_id, member_transaction_id, sale_type, sale_status, subtotal_amount, discount_amount, total_amount, paid_amount, change_amount, note, created_at)
            VALUES (@TenantId, @SaleNo, @ReceiptNo, @SaleTs, @MemberId, @CashierUserId, @MemberTransactionId, @SaleType, 'posted', @SubtotalAmount, @DiscountAmount, @TotalAmount, @PaidAmount, @ChangeAmount, @Note, sysutcdatetime());
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                request.TenantId,
                SaleNo = request.SaleNo.Trim(),
                request.ReceiptNo,
                request.SaleTs,
                request.MemberId,
                CashierUserId = userId,
                MemberTransactionId = memberTransactionId,
                request.SaleType,
                SubtotalAmount = subtotal,
                request.DiscountAmount,
                TotalAmount = total,
                request.PaidAmount,
                ChangeAmount = change,
                request.Note
            },
            transaction);

        if (memberTransactionId.HasValue)
        {
            await connection.ExecuteAsync("UPDATE coop.member_transactions SET source_id = @SaleId WHERE member_transaction_id = @MemberTransactionId", new { SaleId = saleId, MemberTransactionId = memberTransactionId.Value }, transaction);
        }

        foreach (var item in request.Items)
        {
            var lineTotal = item.Quantity * item.UnitPrice - item.LineDiscountAmount;
            await connection.ExecuteAsync(
                "INSERT INTO coop.sale_items (tenant_id, sale_id, product_id, quantity, unit_price, line_discount_amount, line_total_amount, created_at) VALUES (@TenantId, @SaleId, @ProductId, @Quantity, @UnitPrice, @LineDiscountAmount, @LineTotalAmount, sysutcdatetime())",
                new { request.TenantId, SaleId = saleId, item.ProductId, item.Quantity, item.UnitPrice, item.LineDiscountAmount, LineTotalAmount = lineTotal },
                transaction);
            await connection.ExecuteAsync("UPDATE coop.product_stocks SET on_hand_qty = on_hand_qty - @Quantity, updated_at = sysutcdatetime() WHERE tenant_id = @TenantId AND product_id = @ProductId", new { request.TenantId, item.ProductId, item.Quantity }, transaction);
            await connection.ExecuteAsync("INSERT INTO coop.stock_movements (tenant_id, product_id, movement_ts, movement_type, quantity, unit_cost, source_table, source_id, note, created_by_user_id, created_at) VALUES (@TenantId, @ProductId, @MovementTs, 'out', @Quantity, @UnitCost, 'sales', @SourceId, @Note, @UserId, sysutcdatetime())",
                new { request.TenantId, item.ProductId, MovementTs = request.SaleTs, item.Quantity, UnitCost = item.UnitPrice, SourceId = saleId, request.Note, UserId = userId }, transaction);
        }

        transaction.Commit();
        return saleId;
    }

    private sealed record SaleReceiptHeaderRow(
        long SaleId,
        long TenantId,
        string SaleNo,
        string? ReceiptNo,
        DateTime SaleTs,
        long? MemberId,
        string? MemberNo,
        string? MemberName,
        string CashierDisplayName,
        string SaleType,
        string SaleStatus,
        decimal SubtotalAmount,
        decimal DiscountAmount,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal ChangeAmount,
        string? Note);
}
