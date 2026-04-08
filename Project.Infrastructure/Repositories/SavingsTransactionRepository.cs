using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class SavingsTransactionRepository : BaseRepository<object>, ISavingsTransactionRepository
{
    public SavingsTransactionRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<(IEnumerable<SavingsAccountDto> Items, int TotalCount)> GetAccountsAsync(long tenantId, SavingsAccountFilterParams filters)
    {
        var where = new List<string> { "msa.tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, filters.Offset, filters.PageSize });

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(m.member_no LIKE @Search OR m.employee_code LIKE @Search OR m.full_name LIKE @Search OR sp.product_code LIKE @Search OR sp.product_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }

        if (filters.MemberId.HasValue)
        {
            where.Add("msa.member_id = @MemberId");
            parameters.Add("MemberId", filters.MemberId.Value);
        }

        if (filters.SavingsProductId.HasValue)
        {
            where.Add("msa.savings_product_id = @SavingsProductId");
            parameters.Add("SavingsProductId", filters.SavingsProductId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.AccountStatus))
        {
            where.Add("msa.account_status = @AccountStatus");
            parameters.Add("AccountStatus", filters.AccountStatus.Trim());
        }

        var whereClause = string.Join(" AND ", where);

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM coop.member_savings_accounts msa
            INNER JOIN coop.members m ON m.member_id = msa.member_id
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = msa.savings_product_id
            WHERE {whereClause}
            """, parameters);

        var items = await connection.QueryAsync<SavingsAccountDto>($"""
            SELECT
                msa.member_savings_account_id AS MemberSavingsAccountId,
                msa.tenant_id AS TenantId,
                m.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                sp.savings_product_id AS SavingsProductId,
                sp.product_code AS ProductCode,
                sp.product_name AS ProductName,
                sp.savings_kind AS SavingsKind,
                sp.periodicity AS Periodicity,
                msa.opened_at AS OpenedAt,
                msa.account_status AS AccountStatus,
                COALESCE(balance.balance_amount, 0) AS BalanceAmount,
                balance.last_transaction_at AS LastTransactionAt
            FROM coop.member_savings_accounts msa
            INNER JOIN coop.members m ON m.member_id = msa.member_id
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = msa.savings_product_id
            OUTER APPLY (
                SELECT
                    SUM(CASE mt.entry_type WHEN 'credit' THEN st.amount ELSE -st.amount END) AS balance_amount,
                    MAX(st.transaction_ts) AS last_transaction_at
                FROM coop.savings_transactions st
                INNER JOIN coop.member_transactions mt ON mt.member_transaction_id = st.member_transaction_id
                WHERE st.member_savings_account_id = msa.member_savings_account_id
            ) balance
            WHERE {whereClause}
            ORDER BY msa.member_savings_account_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<SavingsTransactionDto> Items, int TotalCount)> GetTransactionsAsync(long tenantId, SavingsTransactionFilterParams filters)
    {
        var where = new List<string> { "st.tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, filters.Offset, filters.PageSize });

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(st.transaction_no LIKE @Search OR m.member_no LIKE @Search OR m.full_name LIKE @Search OR sp.product_code LIKE @Search OR sp.product_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }

        if (filters.MemberId.HasValue)
        {
            where.Add("st.member_id = @MemberId");
            parameters.Add("MemberId", filters.MemberId.Value);
        }

        if (filters.SavingsProductId.HasValue)
        {
            where.Add("msa.savings_product_id = @SavingsProductId");
            parameters.Add("SavingsProductId", filters.SavingsProductId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.TransactionType))
        {
            where.Add("st.transaction_type = @TransactionType");
            parameters.Add("TransactionType", filters.TransactionType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filters.EntryType))
        {
            where.Add("mt.entry_type = @EntryType");
            parameters.Add("EntryType", filters.EntryType.Trim());
        }

        var whereClause = string.Join(" AND ", where);

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM coop.savings_transactions st
            INNER JOIN coop.member_savings_accounts msa ON msa.member_savings_account_id = st.member_savings_account_id
            INNER JOIN coop.members m ON m.member_id = st.member_id
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = msa.savings_product_id
            INNER JOIN coop.member_transactions mt ON mt.member_transaction_id = st.member_transaction_id
            WHERE {whereClause}
            """, parameters);

        var items = await connection.QueryAsync<SavingsTransactionDto>($"""
            SELECT
                st.savings_transaction_id AS SavingsTransactionId,
                st.tenant_id AS TenantId,
                st.member_savings_account_id AS MemberSavingsAccountId,
                m.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                sp.savings_product_id AS SavingsProductId,
                sp.product_code AS ProductCode,
                sp.product_name AS ProductName,
                sp.savings_kind AS SavingsKind,
                st.transaction_no AS TransactionNo,
                st.transaction_ts AS TransactionTs,
                st.transaction_type AS TransactionType,
                mt.entry_type AS EntryType,
                st.amount AS Amount,
                st.period_year AS PeriodYear,
                st.period_month AS PeriodMonth,
                st.note AS Note
            FROM coop.savings_transactions st
            INNER JOIN coop.member_savings_accounts msa ON msa.member_savings_account_id = st.member_savings_account_id
            INNER JOIN coop.members m ON m.member_id = st.member_id
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = msa.savings_product_id
            INNER JOIN coop.member_transactions mt ON mt.member_transaction_id = st.member_transaction_id
            WHERE {whereClause}
            ORDER BY st.transaction_ts DESC, st.savings_transaction_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);

        return (items, totalCount);
    }

    public async Task<SavingsTransactionDto?> GetByIdAsync(long tenantId, long savingsTransactionId)
    {
        const string sql = """
            SELECT
                st.savings_transaction_id AS SavingsTransactionId,
                st.tenant_id AS TenantId,
                st.member_savings_account_id AS MemberSavingsAccountId,
                m.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                sp.savings_product_id AS SavingsProductId,
                sp.product_code AS ProductCode,
                sp.product_name AS ProductName,
                sp.savings_kind AS SavingsKind,
                st.transaction_no AS TransactionNo,
                st.transaction_ts AS TransactionTs,
                st.transaction_type AS TransactionType,
                mt.entry_type AS EntryType,
                st.amount AS Amount,
                st.period_year AS PeriodYear,
                st.period_month AS PeriodMonth,
                st.note AS Note
            FROM coop.savings_transactions st
            INNER JOIN coop.member_savings_accounts msa ON msa.member_savings_account_id = st.member_savings_account_id
            INNER JOIN coop.members m ON m.member_id = st.member_id
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = msa.savings_product_id
            INNER JOIN coop.member_transactions mt ON mt.member_transaction_id = st.member_transaction_id
            WHERE st.tenant_id = @TenantId
              AND st.savings_transaction_id = @SavingsTransactionId
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SavingsTransactionDto>(sql, new { TenantId = tenantId, SavingsTransactionId = savingsTransactionId });
    }

    public async Task<SavingsTransactionContextDto?> GetContextAsync(long tenantId, long memberId, long savingsProductId)
    {
        const string sql = """
            SELECT
                m.tenant_id AS TenantId,
                m.member_id AS MemberId,
                m.member_status AS MemberStatus,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                sp.savings_product_id AS SavingsProductId,
                sp.product_code AS ProductCode,
                sp.product_name AS ProductName,
                sp.savings_kind AS SavingsKind,
                sp.periodicity AS Periodicity,
                sp.is_active AS ProductIsActive,
                msa.member_savings_account_id AS MemberSavingsAccountId
            FROM coop.members m
            INNER JOIN coop.savings_products sp ON sp.tenant_id = m.tenant_id AND sp.savings_product_id = @SavingsProductId
            LEFT JOIN coop.member_savings_accounts msa ON msa.member_id = m.member_id AND msa.savings_product_id = sp.savings_product_id
            WHERE m.tenant_id = @TenantId
              AND m.member_id = @MemberId
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SavingsTransactionContextDto>(sql, new { TenantId = tenantId, MemberId = memberId, SavingsProductId = savingsProductId });
    }

    public async Task<bool> TransactionNoExistsAsync(long tenantId, string transactionNo)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM coop.savings_transactions WHERE tenant_id = @TenantId AND transaction_no = @TransactionNo",
            new { TenantId = tenantId, TransactionNo = transactionNo }) > 0;
    }

    public async Task<decimal> GetBalanceAsync(long memberSavingsAccountId)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<decimal>(
            """
            SELECT COALESCE(SUM(CASE mt.entry_type WHEN 'credit' THEN st.amount ELSE -st.amount END), 0)
            FROM coop.savings_transactions st
            INNER JOIN coop.member_transactions mt ON mt.member_transaction_id = st.member_transaction_id
            WHERE st.member_savings_account_id = @MemberSavingsAccountId
            """,
            new { MemberSavingsAccountId = memberSavingsAccountId });
    }

    public async Task<bool> HasAnyDepositAsync(long memberSavingsAccountId)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1)
            FROM coop.savings_transactions
            WHERE member_savings_account_id = @MemberSavingsAccountId
              AND transaction_type = 'deposit'
            """,
            new { MemberSavingsAccountId = memberSavingsAccountId }) > 0;
    }

    public async Task<long> CreateAsync(long userId, CreateSavingsTransactionRequest request, string entryType)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var context = await connection.QuerySingleAsync<SavingsTransactionContextDto>(
            """
            SELECT
                m.tenant_id AS TenantId,
                m.member_id AS MemberId,
                m.member_status AS MemberStatus,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                sp.savings_product_id AS SavingsProductId,
                sp.product_code AS ProductCode,
                sp.product_name AS ProductName,
                sp.savings_kind AS SavingsKind,
                sp.periodicity AS Periodicity,
                sp.is_active AS ProductIsActive,
                msa.member_savings_account_id AS MemberSavingsAccountId
            FROM coop.members m
            INNER JOIN coop.savings_products sp ON sp.tenant_id = m.tenant_id AND sp.savings_product_id = @SavingsProductId
            LEFT JOIN coop.member_savings_accounts msa ON msa.member_id = m.member_id AND msa.savings_product_id = sp.savings_product_id
            WHERE m.tenant_id = @TenantId
              AND m.member_id = @MemberId
            """,
            new { request.TenantId, request.MemberId, request.SavingsProductId },
            transaction);

        var memberSavingsAccountId = context.MemberSavingsAccountId;
        if (!memberSavingsAccountId.HasValue)
        {
            memberSavingsAccountId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO coop.member_savings_accounts (tenant_id, member_id, savings_product_id, opened_at, account_status, created_at, updated_at)
                VALUES (@TenantId, @MemberId, @SavingsProductId, @OpenedAt, 'active', sysutcdatetime(), NULL);
                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """,
                new
                {
                    request.TenantId,
                    request.MemberId,
                    request.SavingsProductId,
                    OpenedAt = request.TransactionTs
                },
                transaction);
        }

        var memberTransactionId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.member_transactions (
                tenant_id, member_id, transaction_no, transaction_ts, source_module, source_table, source_id,
                entry_type, amount, description, reference_no, created_by_user_id, created_at
            )
            VALUES (
                @TenantId, @MemberId, @TransactionNo, @TransactionTs, 'saving', 'savings_transactions', 0,
                @EntryType, @Amount, @Description, @ReferenceNo, @CreatedByUserId, sysutcdatetime()
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                request.TenantId,
                request.MemberId,
                TransactionNo = request.TransactionNo,
                request.TransactionTs,
                EntryType = entryType,
                request.Amount,
                Description = $"{context.ProductName} {request.TransactionType}",
                ReferenceNo = request.TransactionNo,
                CreatedByUserId = userId
            },
            transaction);

        var savingsTransactionId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.savings_transactions (
                tenant_id, member_savings_account_id, member_id, transaction_no, transaction_ts, transaction_type,
                amount, period_year, period_month, note, member_transaction_id, created_by_user_id, created_at
            )
            VALUES (
                @TenantId, @MemberSavingsAccountId, @MemberId, @TransactionNo, @TransactionTs, @TransactionType,
                @Amount, @PeriodYear, @PeriodMonth, @Note, @MemberTransactionId, @CreatedByUserId, sysutcdatetime()
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                request.TenantId,
                MemberSavingsAccountId = memberSavingsAccountId.Value,
                request.MemberId,
                TransactionNo = request.TransactionNo,
                request.TransactionTs,
                request.TransactionType,
                request.Amount,
                request.PeriodYear,
                request.PeriodMonth,
                request.Note,
                MemberTransactionId = memberTransactionId,
                CreatedByUserId = userId
            },
            transaction);

        await connection.ExecuteAsync(
            """
            UPDATE coop.member_transactions
            SET source_id = @SourceId
            WHERE member_transaction_id = @MemberTransactionId
            """,
            new { SourceId = savingsTransactionId, MemberTransactionId = memberTransactionId },
            transaction);

        transaction.Commit();
        return savingsTransactionId;
    }

    public async Task<bool> DeleteAsync(long tenantId, long savingsTransactionId)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var memberTransactionId = await connection.ExecuteScalarAsync<long?>(
            """
            SELECT member_transaction_id
            FROM coop.savings_transactions
            WHERE tenant_id = @TenantId AND savings_transaction_id = @SavingsTransactionId
            """,
            new { TenantId = tenantId, SavingsTransactionId = savingsTransactionId },
            transaction);

        if (!memberTransactionId.HasValue)
        {
            transaction.Rollback();
            return false;
        }

        await connection.ExecuteAsync(
            "DELETE FROM coop.savings_transactions WHERE savings_transaction_id = @SavingsTransactionId",
            new { SavingsTransactionId = savingsTransactionId },
            transaction);

        await connection.ExecuteAsync(
            "DELETE FROM coop.member_transactions WHERE member_transaction_id = @MemberTransactionId",
            new { MemberTransactionId = memberTransactionId.Value },
            transaction);

        transaction.Commit();
        return true;
    }
}
