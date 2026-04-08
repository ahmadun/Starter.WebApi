using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class InventoryRepository : BaseRepository<Product>, IInventoryRepository
{
    public InventoryRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IEnumerable<ProductCategory>> GetCategoriesAsync(long tenantId)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<ProductCategory>(
            "SELECT product_category_id, tenant_id, category_code, category_name, is_active, created_at, updated_at FROM coop.product_categories WHERE tenant_id = @TenantId ORDER BY category_name",
            new { TenantId = tenantId });
    }

    public async Task<ProductCategory?> GetCategoryByIdAsync(long tenantId, long productCategoryId)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ProductCategory>(
            "SELECT product_category_id, tenant_id, category_code, category_name, is_active, created_at, updated_at FROM coop.product_categories WHERE tenant_id = @TenantId AND product_category_id = @ProductCategoryId",
            new { TenantId = tenantId, ProductCategoryId = productCategoryId });
    }

    public async Task<bool> ProductCategoryCodeExistsAsync(long tenantId, string categoryCode, long? excludeId = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM coop.product_categories WHERE tenant_id = @TenantId AND category_code = @CategoryCode AND (@ExcludeId IS NULL OR product_category_id <> @ExcludeId)", new { TenantId = tenantId, CategoryCode = categoryCode, ExcludeId = excludeId }) > 0;
    }

    public Task<long> CreateCategoryAsync(ProductCategory category) => ExecuteScalarAsync<long>(
        "INSERT INTO coop.product_categories (tenant_id, category_code, category_name, is_active, created_at, updated_at) VALUES (@TenantId, @CategoryCode, @CategoryName, @IsActive, @CreatedAt, @UpdatedAt); SELECT CAST(SCOPE_IDENTITY() AS bigint);",
        category)!;

    public async Task<bool> UpdateCategoryAsync(ProductCategory category)
        => await ExecuteAsync("UPDATE coop.product_categories SET category_code = @CategoryCode, category_name = @CategoryName, is_active = @IsActive, updated_at = @UpdatedAt WHERE tenant_id = @TenantId AND product_category_id = @ProductCategoryId", category) > 0;

    public async Task<IEnumerable<Supplier>> GetSuppliersAsync(long tenantId)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<Supplier>(
            "SELECT supplier_id, tenant_id, supplier_code, supplier_name, contact_name, phone_number, email, address_line, is_active, created_at, updated_at FROM coop.suppliers WHERE tenant_id = @TenantId ORDER BY supplier_name",
            new { TenantId = tenantId });
    }

    public async Task<Supplier?> GetSupplierByIdAsync(long tenantId, long supplierId)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Supplier>(
            "SELECT supplier_id, tenant_id, supplier_code, supplier_name, contact_name, phone_number, email, address_line, is_active, created_at, updated_at FROM coop.suppliers WHERE tenant_id = @TenantId AND supplier_id = @SupplierId",
            new { TenantId = tenantId, SupplierId = supplierId });
    }

    public async Task<bool> SupplierCodeExistsAsync(long tenantId, string supplierCode, long? excludeId = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM coop.suppliers WHERE tenant_id = @TenantId AND supplier_code = @SupplierCode AND (@ExcludeId IS NULL OR supplier_id <> @ExcludeId)", new { TenantId = tenantId, SupplierCode = supplierCode, ExcludeId = excludeId }) > 0;
    }

    public Task<long> CreateSupplierAsync(Supplier supplier) => ExecuteScalarAsync<long>(
        "INSERT INTO coop.suppliers (tenant_id, supplier_code, supplier_name, contact_name, phone_number, email, address_line, is_active, created_at, updated_at) VALUES (@TenantId, @SupplierCode, @SupplierName, @ContactName, @PhoneNumber, @Email, @AddressLine, @IsActive, @CreatedAt, @UpdatedAt); SELECT CAST(SCOPE_IDENTITY() AS bigint);",
        supplier)!;

    public async Task<bool> UpdateSupplierAsync(Supplier supplier)
        => await ExecuteAsync("UPDATE coop.suppliers SET supplier_code = @SupplierCode, supplier_name = @SupplierName, contact_name = @ContactName, phone_number = @PhoneNumber, email = @Email, address_line = @AddressLine, is_active = @IsActive, updated_at = @UpdatedAt WHERE tenant_id = @TenantId AND supplier_id = @SupplierId", supplier) > 0;

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetProductsAsync(long tenantId, ProductFilterParams filters)
    {
        var where = new List<string> { "p.tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, Offset = filters.Offset, PageSize = filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(p.sku LIKE @Search OR p.barcode LIKE @Search OR p.product_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (filters.ProductCategoryId.HasValue)
        {
            where.Add("p.product_category_id = @ProductCategoryId");
            parameters.Add("ProductCategoryId", filters.ProductCategoryId.Value);
        }
        if (filters.IsActive.HasValue)
        {
            where.Add("p.is_active = @IsActive");
            parameters.Add("IsActive", filters.IsActive.Value);
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM coop.products p WHERE {whereClause}", parameters);
        var items = await connection.QueryAsync<Product>($"""
            SELECT p.product_id, p.tenant_id, p.product_category_id, p.sku, p.barcode, p.product_name, p.unit_name, p.cost_price, p.sale_price,
                   p.is_active, p.created_at, p.updated_at,
                   COALESCE(ps.on_hand_qty, 0) AS on_hand_qty,
                   COALESCE(ps.min_stock_qty, 0) AS min_stock_qty
            FROM coop.products p
            LEFT JOIN coop.product_stocks ps ON ps.product_id = p.product_id AND ps.tenant_id = p.tenant_id
            WHERE {whereClause}
            ORDER BY p.product_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public Task<Product?> GetProductByIdAsync(long tenantId, long productId) => QuerySingleOrDefaultAsync(
        """
        SELECT p.product_id, p.tenant_id, p.product_category_id, p.sku, p.barcode, p.product_name, p.unit_name, p.cost_price, p.sale_price,
               p.is_active, p.created_at, p.updated_at,
               COALESCE(ps.on_hand_qty, 0) AS on_hand_qty,
               COALESCE(ps.min_stock_qty, 0) AS min_stock_qty
        FROM coop.products p
        LEFT JOIN coop.product_stocks ps ON ps.product_id = p.product_id AND ps.tenant_id = p.tenant_id
        WHERE p.tenant_id = @TenantId AND p.product_id = @ProductId
        """, new { TenantId = tenantId, ProductId = productId });

    public async Task<IEnumerable<Product>> LookupProductsAsync(long tenantId, string query, int limit)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 25);
        var exact = query.Trim();
        using var connection = CreateConnection();
        return await connection.QueryAsync<Product>(
            """
            SELECT TOP (@Limit)
                   p.product_id, p.tenant_id, p.product_category_id, p.sku, p.barcode, p.product_name, p.unit_name, p.cost_price, p.sale_price,
                   p.is_active, p.created_at, p.updated_at,
                   COALESCE(ps.on_hand_qty, 0) AS on_hand_qty,
                   COALESCE(ps.min_stock_qty, 0) AS min_stock_qty
            FROM coop.products p
            LEFT JOIN coop.product_stocks ps ON ps.product_id = p.product_id AND ps.tenant_id = p.tenant_id
            WHERE p.tenant_id = @TenantId
              AND p.is_active = 1
              AND (p.sku LIKE @Search OR p.barcode LIKE @Search OR p.product_name LIKE @Search)
            ORDER BY
              CASE
                WHEN p.barcode = @Exact THEN 0
                WHEN p.sku = @Exact THEN 1
                WHEN p.product_name = @Exact THEN 2
                WHEN p.barcode LIKE @StartsWith THEN 3
                WHEN p.sku LIKE @StartsWith THEN 4
                WHEN p.product_name LIKE @StartsWith THEN 5
                ELSE 6
              END,
              p.product_name,
              p.product_id DESC
            """,
            new
            {
                TenantId = tenantId,
                Limit = normalizedLimit,
                Exact = exact,
                Search = $"%{exact}%",
                StartsWith = $"{exact}%"
            });
    }

    public async Task<bool> ProductSkuExistsAsync(long tenantId, string sku, long? excludeId = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM coop.products WHERE tenant_id = @TenantId AND sku = @Sku AND (@ExcludeId IS NULL OR product_id <> @ExcludeId)", new { TenantId = tenantId, Sku = sku, ExcludeId = excludeId }) > 0;
    }

    public async Task<bool> ProductBarcodeExistsAsync(long tenantId, string? barcode, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return false;
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM coop.products WHERE tenant_id = @TenantId AND barcode = @Barcode AND (@ExcludeId IS NULL OR product_id <> @ExcludeId)", new { TenantId = tenantId, Barcode = barcode, ExcludeId = excludeId }) > 0;
    }

    public async Task<long> CreateProductAsync(Product product)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        var id = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.products (tenant_id, product_category_id, sku, barcode, product_name, unit_name, cost_price, sale_price, is_active, created_at, updated_at)
            VALUES (@TenantId, @ProductCategoryId, @Sku, @Barcode, @ProductName, @UnitName, @CostPrice, @SalePrice, @IsActive, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """, product, transaction);
        await connection.ExecuteAsync("INSERT INTO coop.product_stocks (tenant_id, product_id, on_hand_qty, min_stock_qty, updated_at) VALUES (@TenantId, @ProductId, 0, @MinStockQty, sysutcdatetime())", new { product.TenantId, ProductId = id, MinStockQty = product.MinStockQty ?? 0 }, transaction);
        transaction.Commit();
        return id;
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        var affected = await connection.ExecuteAsync(
            """
            UPDATE coop.products
            SET product_category_id = @ProductCategoryId, sku = @Sku, barcode = @Barcode, product_name = @ProductName,
                unit_name = @UnitName, cost_price = @CostPrice, sale_price = @SalePrice, is_active = @IsActive, updated_at = @UpdatedAt
            WHERE tenant_id = @TenantId AND product_id = @ProductId
            """, product, transaction);
        await connection.ExecuteAsync("UPDATE coop.product_stocks SET min_stock_qty = @MinStockQty, updated_at = sysutcdatetime() WHERE tenant_id = @TenantId AND product_id = @ProductId", new { product.TenantId, product.ProductId, MinStockQty = product.MinStockQty ?? 0 }, transaction);
        transaction.Commit();
        return affected > 0;
    }

    public async Task<(IEnumerable<PurchaseReceiptDto> Items, int TotalCount)> GetPurchaseReceiptsAsync(long tenantId, PurchaseReceiptFilterParams filters)
    {
        var where = new List<string> { "pr.tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, filters.Offset, filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(pr.receipt_no LIKE @Search OR s.supplier_code LIKE @Search OR s.supplier_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (filters.SupplierId.HasValue)
        {
            where.Add("pr.supplier_id = @SupplierId");
            parameters.Add("SupplierId", filters.SupplierId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filters.ReceiptStatus))
        {
            where.Add("pr.receipt_status = @ReceiptStatus");
            parameters.Add("ReceiptStatus", filters.ReceiptStatus.Trim());
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM coop.purchase_receipts pr
            LEFT JOIN coop.suppliers s ON s.supplier_id = pr.supplier_id
            WHERE {whereClause}
            """, parameters);
        var items = await connection.QueryAsync<PurchaseReceiptDto>($"""
            SELECT purchase_receipt_id AS PurchaseReceiptId, tenant_id AS TenantId, receipt_no AS ReceiptNo, supplier_id AS SupplierId, receipt_date AS ReceiptDate, total_amount AS TotalAmount, note AS Note
            FROM coop.purchase_receipts pr
            WHERE {whereClause}
            ORDER BY pr.receipt_date DESC, pr.purchase_receipt_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public async Task<PurchaseReceiptDetailDto?> GetPurchaseReceiptByIdAsync(long tenantId, long purchaseReceiptId)
    {
        using var connection = CreateConnection();
        var header = await connection.QuerySingleOrDefaultAsync<PurchaseReceiptHeaderRow>(
            """
            SELECT
                pr.purchase_receipt_id AS PurchaseReceiptId,
                pr.tenant_id AS TenantId,
                pr.receipt_no AS ReceiptNo,
                pr.supplier_id AS SupplierId,
                s.supplier_code AS SupplierCode,
                s.supplier_name AS SupplierName,
                pr.receipt_date AS ReceiptDate,
                pr.receipt_status AS ReceiptStatus,
                pr.total_amount AS TotalAmount,
                pr.note AS Note,
                u.display_name AS CreatedByDisplayName
            FROM coop.purchase_receipts pr
            LEFT JOIN coop.suppliers s ON s.supplier_id = pr.supplier_id
            LEFT JOIN coop.users u ON u.user_id = pr.created_by_user_id
            WHERE pr.tenant_id = @TenantId
              AND pr.purchase_receipt_id = @PurchaseReceiptId
            """,
            new { TenantId = tenantId, PurchaseReceiptId = purchaseReceiptId });

        if (header is null)
            return null;

        var items = await connection.QueryAsync<PurchaseReceiptItemDto>(
            """
            SELECT
                pri.product_id AS ProductId,
                p.product_name AS ProductName,
                pri.quantity AS Quantity,
                pri.unit_cost AS UnitCost,
                pri.line_total_amount AS LineTotalAmount
            FROM coop.purchase_receipt_items pri
            INNER JOIN coop.products p ON p.product_id = pri.product_id
            WHERE pri.purchase_receipt_id = @PurchaseReceiptId
            ORDER BY pri.purchase_receipt_item_id
            """,
            new { PurchaseReceiptId = purchaseReceiptId });

        return new PurchaseReceiptDetailDto(
            header.PurchaseReceiptId,
            header.TenantId,
            header.ReceiptNo,
            header.SupplierId,
            header.SupplierCode,
            header.SupplierName,
            header.ReceiptDate,
            header.ReceiptStatus,
            header.TotalAmount,
            header.Note,
            header.CreatedByDisplayName,
            items.ToList());
    }

    public async Task<long> CreatePurchaseReceiptAsync(long userId, CreatePurchaseReceiptRequest request)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        var totalAmount = request.Items.Sum(x => x.Quantity * x.UnitCost);
        var receiptId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.purchase_receipts (tenant_id, receipt_no, supplier_id, receipt_date, receipt_status, total_amount, note, created_by_user_id, created_at, updated_at)
            VALUES (@TenantId, @ReceiptNo, @SupplierId, @ReceiptDate, 'posted', @TotalAmount, @Note, @UserId, sysutcdatetime(), NULL);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new { request.TenantId, request.ReceiptNo, request.SupplierId, request.ReceiptDate, TotalAmount = totalAmount, request.Note, UserId = userId },
            transaction);

        foreach (var item in request.Items)
        {
            var lineTotal = item.Quantity * item.UnitCost;
            await connection.ExecuteAsync(
                "INSERT INTO coop.purchase_receipt_items (tenant_id, purchase_receipt_id, product_id, quantity, unit_cost, line_total_amount, created_at) VALUES (@TenantId, @PurchaseReceiptId, @ProductId, @Quantity, @UnitCost, @LineTotalAmount, sysutcdatetime())",
                new { request.TenantId, PurchaseReceiptId = receiptId, item.ProductId, item.Quantity, item.UnitCost, LineTotalAmount = lineTotal },
                transaction);
            await connection.ExecuteAsync("UPDATE coop.product_stocks SET on_hand_qty = on_hand_qty + @Quantity, updated_at = sysutcdatetime() WHERE tenant_id = @TenantId AND product_id = @ProductId", new { request.TenantId, item.ProductId, item.Quantity }, transaction);
            await connection.ExecuteAsync("INSERT INTO coop.stock_movements (tenant_id, product_id, movement_ts, movement_type, quantity, unit_cost, source_table, source_id, note, created_by_user_id, created_at) VALUES (@TenantId, @ProductId, @MovementTs, 'in', @Quantity, @UnitCost, 'purchase_receipts', @SourceId, @Note, @UserId, sysutcdatetime())",
                new { request.TenantId, item.ProductId, MovementTs = request.ReceiptDate, item.Quantity, item.UnitCost, SourceId = receiptId, request.Note, UserId = userId }, transaction);
        }

        transaction.Commit();
        return receiptId;
    }

    public async Task<(IEnumerable<StockAdjustmentDto> Items, int TotalCount)> GetStockAdjustmentsAsync(long tenantId, StockAdjustmentFilterParams filters)
    {
        var where = new List<string> { "sa.tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, filters.Offset, filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(sa.adjustment_no LIKE @Search OR sa.reason LIKE @Search OR sa.note LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filters.AdjustmentType))
        {
            where.Add("sa.adjustment_type = @AdjustmentType");
            parameters.Add("AdjustmentType", filters.AdjustmentType.Trim());
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM coop.stock_adjustments sa WHERE {whereClause}", parameters);
        var items = await connection.QueryAsync<StockAdjustmentDto>($"""
            SELECT stock_adjustment_id AS StockAdjustmentId, tenant_id AS TenantId, adjustment_no AS AdjustmentNo, adjustment_ts AS AdjustmentTs, adjustment_type AS AdjustmentType, reason AS Reason, note AS Note
            FROM coop.stock_adjustments sa
            WHERE {whereClause}
            ORDER BY sa.adjustment_ts DESC, sa.stock_adjustment_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public async Task<StockAdjustmentDetailDto?> GetStockAdjustmentByIdAsync(long tenantId, long stockAdjustmentId)
    {
        using var connection = CreateConnection();
        var header = await connection.QuerySingleOrDefaultAsync<StockAdjustmentHeaderRow>(
            """
            SELECT
                sa.stock_adjustment_id AS StockAdjustmentId,
                sa.tenant_id AS TenantId,
                sa.adjustment_no AS AdjustmentNo,
                sa.adjustment_ts AS AdjustmentTs,
                sa.adjustment_type AS AdjustmentType,
                sa.reason AS Reason,
                sa.note AS Note,
                u.display_name AS CreatedByDisplayName
            FROM coop.stock_adjustments sa
            LEFT JOIN coop.users u ON u.user_id = sa.created_by_user_id
            WHERE sa.tenant_id = @TenantId
              AND sa.stock_adjustment_id = @StockAdjustmentId
            """,
            new { TenantId = tenantId, StockAdjustmentId = stockAdjustmentId });

        if (header is null)
            return null;

        var items = await connection.QueryAsync<StockAdjustmentItemDto>(
            """
            SELECT
                sai.product_id AS ProductId,
                p.product_name AS ProductName,
                sai.system_qty AS SystemQty,
                sai.actual_qty AS ActualQty,
                sai.adjustment_qty AS AdjustmentQty,
                sai.unit_cost AS UnitCost,
                sai.note AS Note
            FROM coop.stock_adjustment_items sai
            INNER JOIN coop.products p ON p.product_id = sai.product_id
            WHERE sai.stock_adjustment_id = @StockAdjustmentId
            ORDER BY sai.stock_adjustment_item_id
            """,
            new { StockAdjustmentId = stockAdjustmentId });

        return new StockAdjustmentDetailDto(
            header.StockAdjustmentId,
            header.TenantId,
            header.AdjustmentNo,
            header.AdjustmentTs,
            header.AdjustmentType,
            header.Reason,
            header.Note,
            header.CreatedByDisplayName,
            items.ToList());
    }

    public async Task<long> CreateStockAdjustmentAsync(long userId, CreateStockAdjustmentRequest request)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        var adjustmentId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.stock_adjustments (tenant_id, adjustment_no, adjustment_ts, adjustment_type, reason, note, created_by_user_id, created_at)
            VALUES (@TenantId, @AdjustmentNo, @AdjustmentTs, @AdjustmentType, @Reason, @Note, @UserId, sysutcdatetime());
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new { request.TenantId, request.AdjustmentNo, request.AdjustmentTs, request.AdjustmentType, request.Reason, request.Note, UserId = userId },
            transaction);

        foreach (var item in request.Items)
        {
            var systemQty = await connection.ExecuteScalarAsync<decimal>("SELECT on_hand_qty FROM coop.product_stocks WHERE tenant_id = @TenantId AND product_id = @ProductId", new { request.TenantId, item.ProductId }, transaction);
            var adjustmentQty = item.ActualQty - systemQty;
            await connection.ExecuteAsync(
                "INSERT INTO coop.stock_adjustment_items (tenant_id, stock_adjustment_id, product_id, system_qty, actual_qty, adjustment_qty, unit_cost, note, created_at) VALUES (@TenantId, @StockAdjustmentId, @ProductId, @SystemQty, @ActualQty, @AdjustmentQty, @UnitCost, @Note, sysutcdatetime())",
                new { request.TenantId, StockAdjustmentId = adjustmentId, item.ProductId, SystemQty = systemQty, item.ActualQty, AdjustmentQty = adjustmentQty, item.UnitCost, item.Note }, transaction);
            await connection.ExecuteAsync("UPDATE coop.product_stocks SET on_hand_qty = @ActualQty, updated_at = sysutcdatetime() WHERE tenant_id = @TenantId AND product_id = @ProductId", new { request.TenantId, item.ProductId, item.ActualQty }, transaction);
            await connection.ExecuteAsync("INSERT INTO coop.stock_movements (tenant_id, product_id, movement_ts, movement_type, quantity, unit_cost, source_table, source_id, note, created_by_user_id, created_at) VALUES (@TenantId, @ProductId, @MovementTs, 'adjustment', @Quantity, @UnitCost, 'stock_adjustments', @SourceId, @Note, @UserId, sysutcdatetime())",
                new { request.TenantId, item.ProductId, MovementTs = request.AdjustmentTs, Quantity = Math.Abs(adjustmentQty), item.UnitCost, SourceId = adjustmentId, Note = item.Note ?? request.Note, UserId = userId }, transaction);
        }

        transaction.Commit();
        return adjustmentId;
    }

    public async Task<(IEnumerable<StockMovementDto> Items, int TotalCount)> GetStockMovementsAsync(long tenantId, StockMovementFilterParams filters)
    {
        var where = new List<string> { "sm.tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, filters.Offset, filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(p.sku LIKE @Search OR p.product_name LIKE @Search OR sm.note LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (filters.ProductId.HasValue)
        {
            where.Add("sm.product_id = @ProductId");
            parameters.Add("ProductId", filters.ProductId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filters.MovementType))
        {
            where.Add("sm.movement_type = @MovementType");
            parameters.Add("MovementType", filters.MovementType.Trim());
        }
        if (!string.IsNullOrWhiteSpace(filters.SourceTable))
        {
            where.Add("sm.source_table = @SourceTable");
            parameters.Add("SourceTable", filters.SourceTable.Trim());
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM coop.stock_movements sm
            INNER JOIN coop.products p ON p.product_id = sm.product_id
            WHERE {whereClause}
            """, parameters);
        var items = await connection.QueryAsync<StockMovementDto>($"""
            SELECT
                sm.stock_movement_id AS StockMovementId,
                sm.tenant_id AS TenantId,
                sm.product_id AS ProductId,
                p.product_name AS ProductName,
                p.sku AS Sku,
                sm.movement_ts AS MovementTs,
                sm.movement_type AS MovementType,
                sm.quantity AS Quantity,
                sm.unit_cost AS UnitCost,
                sm.source_table AS SourceTable,
                sm.source_id AS SourceId,
                sm.note AS Note,
                u.display_name AS CreatedByDisplayName
            FROM coop.stock_movements sm
            INNER JOIN coop.products p ON p.product_id = sm.product_id
            LEFT JOIN coop.users u ON u.user_id = sm.created_by_user_id
            WHERE {whereClause}
            ORDER BY sm.movement_ts DESC, sm.stock_movement_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    private sealed record PurchaseReceiptHeaderRow(
        long PurchaseReceiptId,
        long TenantId,
        string ReceiptNo,
        long? SupplierId,
        string? SupplierCode,
        string? SupplierName,
        DateTime ReceiptDate,
        string ReceiptStatus,
        decimal TotalAmount,
        string? Note,
        string? CreatedByDisplayName);

    private sealed record StockAdjustmentHeaderRow(
        long StockAdjustmentId,
        long TenantId,
        string AdjustmentNo,
        DateTime AdjustmentTs,
        string AdjustmentType,
        string Reason,
        string? Note,
        string? CreatedByDisplayName);
}
