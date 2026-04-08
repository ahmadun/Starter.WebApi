using Dapper;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class MemberPortalRepository : BaseRepository<object>, IMemberPortalRepository
{
    public MemberPortalRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<MemberPortalProfileDto?> GetProfileAsync(long userId)
    {
        const string sql = """
            SELECT
                u.user_id AS UserId,
                u.tenant_id AS TenantId,
                t.tenant_code AS TenantCode,
                t.tenant_name AS TenantName,
                m.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.employee_code AS EmployeeCode,
                m.full_name AS FullName,
                m.identity_no AS IdentityNo,
                m.phone_number AS PhoneNumber,
                COALESCE(NULLIF(m.email, ''), u.email) AS Email,
                m.address_line AS AddressLine,
                m.join_date AS JoinDate,
                m.member_status AS MemberStatus,
                u.username AS Username,
                u.display_name AS DisplayName
            FROM coop.users u
            INNER JOIN coop.tenants t ON t.tenant_id = u.tenant_id
            INNER JOIN coop.members m ON m.member_id = u.member_id
            WHERE u.user_id = @UserId
              AND u.user_type = 'member'
              AND u.is_active = 1
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<MemberPortalProfileDto>(sql, new { UserId = userId });
    }

    public async Task<MemberPortalDashboardDto?> GetDashboardAsync(long userId)
    {
        var context = await GetContextAsync(userId);
        if (context is null)
            return null;

        const string sql = """
            SELECT
                COALESCE((
                    SELECT SUM(CASE mt.entry_type WHEN 'credit' THEN st.amount ELSE -st.amount END)
                    FROM coop.savings_transactions st
                    INNER JOIN coop.member_transactions mt ON mt.member_transaction_id = st.member_transaction_id
                    WHERE st.tenant_id = @TenantId
                      AND st.member_id = @MemberId
                ), 0) AS TotalSavingsBalance,
                COALESCE((
                    SELECT SUM(l.outstanding_total_amount)
                    FROM coop.loans l
                    WHERE l.tenant_id = @TenantId
                      AND l.member_id = @MemberId
                      AND l.status IN ('approved', 'active', 'defaulted')
                ), 0) AS OutstandingLoanAmount,
                COALESCE((
                    SELECT COUNT(1)
                    FROM coop.loans l
                    WHERE l.tenant_id = @TenantId
                      AND l.member_id = @MemberId
                      AND l.status IN ('approved', 'active', 'defaulted')
                ), 0) AS ActiveLoanCount,
                COALESCE((
                    SELECT COUNT(1)
                    FROM coop.loan_installment_schedules lis
                    INNER JOIN coop.loans l ON l.loan_id = lis.loan_id
                    WHERE l.tenant_id = @TenantId
                      AND l.member_id = @MemberId
                      AND l.status IN ('approved', 'active', 'defaulted')
                      AND lis.installment_status IN ('unpaid', 'partial', 'overdue')
                ), 0) AS PendingInstallmentCount,
                (
                    SELECT TOP (1) lis.due_date
                    FROM coop.loan_installment_schedules lis
                    INNER JOIN coop.loans l ON l.loan_id = lis.loan_id
                    WHERE l.tenant_id = @TenantId
                      AND l.member_id = @MemberId
                      AND l.status IN ('approved', 'active', 'defaulted')
                      AND lis.installment_status IN ('unpaid', 'partial', 'overdue')
                    ORDER BY lis.due_date, lis.installment_no
                ) AS NextInstallmentDueDate,
                (
                    SELECT TOP (1) lis.installment_amount - lis.paid_amount
                    FROM coop.loan_installment_schedules lis
                    INNER JOIN coop.loans l ON l.loan_id = lis.loan_id
                    WHERE l.tenant_id = @TenantId
                      AND l.member_id = @MemberId
                      AND l.status IN ('approved', 'active', 'defaulted')
                      AND lis.installment_status IN ('unpaid', 'partial', 'overdue')
                    ORDER BY lis.due_date, lis.installment_no
                ) AS NextInstallmentAmount,
                COALESCE((
                    SELECT SUM(s.total_amount)
                    FROM coop.sales s
                    WHERE s.tenant_id = @TenantId
                      AND s.member_id = @MemberId
                      AND s.sale_status = 'posted'
                ), 0) AS TotalPurchaseAmount
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleAsync<MemberPortalDashboardDto>(sql, context);
    }

    public async Task<IReadOnlyCollection<MemberPortalSavingsAccountDto>> GetSavingsAccountsAsync(long userId)
    {
        var context = await GetContextAsync(userId);
        if (context is null)
            return [];

        const string sql = """
            SELECT
                msa.member_savings_account_id AS MemberSavingsAccountId,
                sp.savings_product_id AS SavingsProductId,
                sp.product_code AS ProductCode,
                sp.product_name AS ProductName,
                sp.savings_kind AS SavingsKind,
                sp.periodicity AS Periodicity,
                sp.default_amount AS DefaultAmount,
                msa.opened_at AS OpenedAt,
                msa.account_status AS AccountStatus,
                COALESCE(tx.balance_amount, 0) AS BalanceAmount,
                tx.last_transaction_at AS LastTransactionAt
            FROM coop.member_savings_accounts msa
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = msa.savings_product_id
            OUTER APPLY (
                SELECT
                    SUM(CASE mt.entry_type WHEN 'credit' THEN st.amount ELSE -st.amount END) AS balance_amount,
                    MAX(st.transaction_ts) AS last_transaction_at
                FROM coop.savings_transactions st
                INNER JOIN coop.member_transactions mt ON mt.member_transaction_id = st.member_transaction_id
                WHERE st.member_savings_account_id = msa.member_savings_account_id
            ) tx
            WHERE msa.tenant_id = @TenantId
              AND msa.member_id = @MemberId
            ORDER BY sp.savings_kind, sp.product_name
            """;

        using var connection = CreateConnection();
        var items = await connection.QueryAsync<MemberPortalSavingsAccountDto>(sql, context);
        return items.ToList();
    }

    public async Task<IReadOnlyCollection<MemberPortalLoanDto>> GetLoansAsync(long userId)
    {
        var context = await GetContextAsync(userId);
        if (context is null)
            return [];

        const string loanSql = """
            SELECT
                l.loan_id AS LoanId,
                l.loan_product_id AS LoanProductId,
                lp.product_name AS LoanProductName,
                l.loan_no AS LoanNo,
                l.loan_date AS LoanDate,
                l.principal_amount AS PrincipalAmount,
                l.flat_interest_rate_pct AS FlatInterestRatePct,
                l.term_months AS TermMonths,
                l.admin_fee_amount AS AdminFeeAmount,
                l.penalty_amount AS PenaltyAmount,
                l.installment_amount AS InstallmentAmount,
                l.total_interest_amount AS TotalInterestAmount,
                l.total_payable_amount AS TotalPayableAmount,
                l.outstanding_principal_amount AS OutstandingPrincipalAmount,
                l.outstanding_total_amount AS OutstandingTotalAmount,
                l.status AS Status,
                next_due.next_due_date AS NextDueDate,
                next_due.next_due_amount AS NextDueAmount,
                COALESCE(stats.pending_installment_count, 0) AS PendingInstallmentCount,
                l.note AS Note
            FROM coop.loans l
            INNER JOIN coop.loan_products lp ON lp.loan_product_id = l.loan_product_id
            OUTER APPLY (
                SELECT TOP (1)
                    lis.due_date AS next_due_date,
                    lis.installment_amount - lis.paid_amount AS next_due_amount
                FROM coop.loan_installment_schedules lis
                WHERE lis.loan_id = l.loan_id
                  AND lis.installment_status IN ('unpaid', 'partial', 'overdue')
                ORDER BY lis.due_date, lis.installment_no
            ) next_due
            OUTER APPLY (
                SELECT COUNT(1) AS pending_installment_count
                FROM coop.loan_installment_schedules lis
                WHERE lis.loan_id = l.loan_id
                  AND lis.installment_status IN ('unpaid', 'partial', 'overdue')
            ) stats
            WHERE l.tenant_id = @TenantId
              AND l.member_id = @MemberId
            ORDER BY l.loan_date DESC, l.loan_id DESC
            """;

        const string installmentSql = """
            SELECT
                lis.loan_id AS LoanId,
                lis.installment_no AS InstallmentNo,
                lis.due_date AS DueDate,
                lis.principal_due_amount AS PrincipalDueAmount,
                lis.interest_due_amount AS InterestDueAmount,
                lis.installment_amount AS InstallmentAmount,
                lis.paid_amount AS PaidAmount,
                lis.installment_status AS InstallmentStatus,
                lis.settled_at AS SettledAt
            FROM coop.loan_installment_schedules lis
            WHERE lis.loan_id IN @LoanIds
            ORDER BY lis.loan_id, lis.installment_no
            """;

        using var connection = CreateConnection();
        var loans = (await connection.QueryAsync<MemberPortalLoanRow>(loanSql, context)).ToList();
        if (loans.Count == 0)
            return [];

        var installments = (await connection.QueryAsync<MemberPortalLoanInstallmentRow>(
            installmentSql,
            new { LoanIds = loans.Select(x => x.LoanId).ToArray() }))
            .ToList();

        var installmentLookup = installments
            .GroupBy(x => x.LoanId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyCollection<MemberPortalLoanInstallmentDto>)x
                    .Select(item => new MemberPortalLoanInstallmentDto(
                        item.InstallmentNo,
                        item.DueDate,
                        item.PrincipalDueAmount,
                        item.InterestDueAmount,
                        item.InstallmentAmount,
                        item.PaidAmount,
                        item.InstallmentStatus,
                        item.SettledAt))
                    .ToList());

        return loans
            .Select(loan => new MemberPortalLoanDto(
                loan.LoanId,
                loan.LoanProductId,
                loan.LoanProductName,
                loan.LoanNo,
                loan.LoanDate,
                loan.PrincipalAmount,
                loan.FlatInterestRatePct,
                loan.TermMonths,
                loan.AdminFeeAmount,
                loan.PenaltyAmount,
                loan.InstallmentAmount,
                loan.TotalInterestAmount,
                loan.TotalPayableAmount,
                loan.OutstandingPrincipalAmount,
                loan.OutstandingTotalAmount,
                loan.Status,
                loan.NextDueDate,
                loan.NextDueAmount,
                loan.PendingInstallmentCount,
                loan.Note,
                installmentLookup.TryGetValue(loan.LoanId, out var loanInstallments) ? loanInstallments : []))
            .ToList();
    }

    public async Task<(IEnumerable<LoanPaymentDto> Items, int TotalCount)> GetLoanPaymentsAsync(long userId, PaginationParams filters)
    {
        var context = await GetContextAsync(userId);
        if (context is null)
            return ([], 0);

        var parameters = new DynamicParameters(new
        {
            context.TenantId,
            context.MemberId,
            filters.Offset,
            filters.PageSize
        });

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM coop.loan_payments lp
            INNER JOIN coop.loans l ON l.loan_id = lp.loan_id
            INNER JOIN coop.members m ON m.member_id = lp.member_id
            WHERE lp.tenant_id = @TenantId
              AND lp.member_id = @MemberId
            """,
            parameters);

        var items = await connection.QueryAsync<LoanPaymentDto>(
            """
            SELECT
                lp.loan_payment_id AS LoanPaymentId,
                lp.tenant_id AS TenantId,
                lp.loan_id AS LoanId,
                l.loan_no AS LoanNo,
                lp.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                lp.loan_installment_schedule_id AS LoanInstallmentScheduleId,
                lp.payment_no AS PaymentNo,
                lp.payment_ts AS PaymentTs,
                lp.payment_amount AS PaymentAmount,
                lp.principal_paid_amount AS PrincipalPaidAmount,
                lp.interest_paid_amount AS InterestPaidAmount,
                lp.penalty_paid_amount AS PenaltyPaidAmount,
                lp.note AS Note
            FROM coop.loan_payments lp
            INNER JOIN coop.loans l ON l.loan_id = lp.loan_id
            INNER JOIN coop.members m ON m.member_id = lp.member_id
            WHERE lp.tenant_id = @TenantId
              AND lp.member_id = @MemberId
            ORDER BY lp.payment_ts DESC, lp.loan_payment_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """,
            parameters);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<MemberPortalPurchaseDto> Items, int TotalCount)> GetPurchasesAsync(long userId, MemberPortalPurchaseFilterParams filters)
    {
        var context = await GetContextAsync(userId);
        if (context is null)
            return ([], 0);

        var where = new List<string>
        {
            "tenant_id = @TenantId",
            "member_id = @MemberId"
        };

        var parameters = new DynamicParameters(new
        {
            context.TenantId,
            context.MemberId,
            filters.Offset,
            filters.PageSize
        });

        if (!string.IsNullOrWhiteSpace(filters.SaleType))
        {
            where.Add("sale_type = @SaleType");
            parameters.Add("SaleType", filters.SaleType.Trim());
        }

        var whereClause = string.Join(" AND ", where);

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM coop.sales WHERE {whereClause}", parameters);
        var items = await connection.QueryAsync<MemberPortalPurchaseDto>($"""
            SELECT
                sale_id AS SaleId,
                sale_no AS SaleNo,
                receipt_no AS ReceiptNo,
                sale_ts AS SaleTs,
                sale_type AS SaleType,
                sale_status AS SaleStatus,
                total_amount AS TotalAmount,
                paid_amount AS PaidAmount,
                change_amount AS ChangeAmount,
                note AS Note
            FROM coop.sales
            WHERE {whereClause}
            ORDER BY sale_ts DESC, sale_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<MemberPortalTransactionDto> Items, int TotalCount)> GetTransactionsAsync(long userId, MemberPortalTransactionFilterParams filters)
    {
        var context = await GetContextAsync(userId);
        if (context is null)
            return ([], 0);

        var where = new List<string>
        {
            "tenant_id = @TenantId",
            "member_id = @MemberId"
        };

        var parameters = new DynamicParameters(new
        {
            context.TenantId,
            context.MemberId,
            filters.Offset,
            filters.PageSize
        });

        if (!string.IsNullOrWhiteSpace(filters.SourceModule))
        {
            where.Add("source_module = @SourceModule");
            parameters.Add("SourceModule", filters.SourceModule.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filters.EntryType))
        {
            where.Add("entry_type = @EntryType");
            parameters.Add("EntryType", filters.EntryType.Trim());
        }

        var whereClause = string.Join(" AND ", where);

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM coop.member_transactions WHERE {whereClause}", parameters);
        var items = await connection.QueryAsync<MemberPortalTransactionDto>($"""
            SELECT
                member_transaction_id AS MemberTransactionId,
                transaction_no AS TransactionNo,
                transaction_ts AS TransactionTs,
                source_module AS SourceModule,
                source_table AS SourceTable,
                source_id AS SourceId,
                entry_type AS EntryType,
                amount AS Amount,
                description AS Description,
                reference_no AS ReferenceNo
            FROM coop.member_transactions
            WHERE {whereClause}
            ORDER BY transaction_ts DESC, member_transaction_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<MemberLoanRequestDto> Items, int TotalCount)> GetLoanRequestsAsync(long userId, MemberLoanRequestFilterParams filters)
    {
        var context = await GetContextAsync(userId);
        if (context is null)
            return ([], 0);

        var where = new List<string>
        {
            "mlr.tenant_id = @TenantId",
            "mlr.member_id = @MemberId"
        };

        var parameters = new DynamicParameters(new
        {
            context.TenantId,
            context.MemberId,
            filters.Offset,
            filters.PageSize
        });

        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            where.Add("mlr.status = @Status");
            parameters.Add("Status", filters.Status.Trim());
        }

        if (filters.MemberId.HasValue)
        {
            where.Add("mlr.member_id = @FilterMemberId");
            parameters.Add("FilterMemberId", filters.MemberId.Value);
        }

        var whereClause = string.Join(" AND ", where);

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM coop.member_loan_requests mlr
            INNER JOIN coop.members m ON m.member_id = mlr.member_id
            WHERE {whereClause}
            """, parameters);

        var items = await connection.QueryAsync<MemberLoanRequestDto>($"""
            SELECT
                mlr.member_loan_request_id AS MemberLoanRequestId,
                mlr.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                mlr.request_no AS RequestNo,
                mlr.loan_product_id AS LoanProductId,
                lp.product_code AS LoanProductCode,
                lp.product_name AS LoanProductName,
                mlr.principal_amount AS PrincipalAmount,
                mlr.proposed_term_months AS ProposedTermMonths,
                mlr.status AS Status,
                mlr.note AS Note,
                mlr.reviewer_note AS ReviewerNote,
                mlr.approved_loan_id AS ApprovedLoanId,
                mlr.requested_at AS RequestedAt,
                mlr.reviewed_at AS ReviewedAt
            FROM coop.member_loan_requests mlr
            INNER JOIN coop.loan_products lp ON lp.loan_product_id = mlr.loan_product_id
            INNER JOIN coop.members m ON m.member_id = mlr.member_id
            WHERE {whereClause}
            ORDER BY mlr.requested_at DESC, mlr.member_loan_request_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);

        return (items, totalCount);
    }

    public async Task<MemberLoanRequestDto?> GetLoanRequestByIdAsync(long userId, long memberLoanRequestId)
    {
        var context = await GetContextAsync(userId);
        if (context is null)
            return null;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<MemberLoanRequestDto>(
            """
            SELECT
                mlr.member_loan_request_id AS MemberLoanRequestId,
                mlr.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                mlr.request_no AS RequestNo,
                mlr.loan_product_id AS LoanProductId,
                lp.product_code AS LoanProductCode,
                lp.product_name AS LoanProductName,
                mlr.principal_amount AS PrincipalAmount,
                mlr.proposed_term_months AS ProposedTermMonths,
                mlr.status AS Status,
                mlr.note AS Note,
                mlr.reviewer_note AS ReviewerNote,
                mlr.approved_loan_id AS ApprovedLoanId,
                mlr.requested_at AS RequestedAt,
                mlr.reviewed_at AS ReviewedAt
            FROM coop.member_loan_requests mlr
            INNER JOIN coop.loan_products lp ON lp.loan_product_id = mlr.loan_product_id
            INNER JOIN coop.members m ON m.member_id = mlr.member_id
            WHERE mlr.tenant_id = @TenantId
              AND mlr.member_id = @MemberId
              AND mlr.member_loan_request_id = @MemberLoanRequestId
            """,
            new
            {
                context.TenantId,
                context.MemberId,
                MemberLoanRequestId = memberLoanRequestId
            });
    }

    public async Task<long> CreateLoanRequestAsync(long userId, CreateMemberLoanRequest request)
    {
        var context = await GetContextAsync(userId)
            ?? throw new InvalidOperationException("Member portal profile not found.");

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.member_loan_requests (
                tenant_id, member_id, loan_product_id, request_no, principal_amount, proposed_term_months,
                status, note, requested_at, created_by_user_id, created_at
            )
            VALUES (
                @TenantId, @MemberId, @LoanProductId,
                CONCAT('LR-', FORMAT(sysutcdatetime(), 'yyyyMMddHHmmss'), '-', RIGHT(CONCAT('000000', ABS(CHECKSUM(NEWID()))), 6)),
                @PrincipalAmount, @ProposedTermMonths, 'pending', @Note, sysutcdatetime(), @CreatedByUserId, sysutcdatetime()
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                context.TenantId,
                context.MemberId,
                request.LoanProductId,
                request.PrincipalAmount,
                ProposedTermMonths = request.ProposedTermMonths ?? 0,
                request.Note,
                CreatedByUserId = userId
            });
    }

    public async Task<(IEnumerable<MemberLoanRequestDto> Items, int TotalCount)> GetLoanRequestsForApprovalAsync(long tenantId, MemberLoanRequestFilterParams filters)
    {
        var where = new List<string> { "mlr.tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, filters.Offset, filters.PageSize });

        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            where.Add("mlr.status = @Status");
            parameters.Add("Status", filters.Status.Trim());
        }

        if (filters.MemberId.HasValue)
        {
            where.Add("mlr.member_id = @MemberId");
            parameters.Add("MemberId", filters.MemberId.Value);
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM coop.member_loan_requests mlr
            INNER JOIN coop.members m ON m.member_id = mlr.member_id
            WHERE {whereClause}
            """, parameters);

        var items = await connection.QueryAsync<MemberLoanRequestDto>($"""
            SELECT
                mlr.member_loan_request_id AS MemberLoanRequestId,
                mlr.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                mlr.request_no AS RequestNo,
                mlr.loan_product_id AS LoanProductId,
                lp.product_code AS LoanProductCode,
                lp.product_name AS LoanProductName,
                mlr.principal_amount AS PrincipalAmount,
                mlr.proposed_term_months AS ProposedTermMonths,
                mlr.status AS Status,
                mlr.note AS Note,
                mlr.reviewer_note AS ReviewerNote,
                mlr.approved_loan_id AS ApprovedLoanId,
                mlr.requested_at AS RequestedAt,
                mlr.reviewed_at AS ReviewedAt
            FROM coop.member_loan_requests mlr
            INNER JOIN coop.loan_products lp ON lp.loan_product_id = mlr.loan_product_id
            INNER JOIN coop.members m ON m.member_id = mlr.member_id
            WHERE {whereClause}
            ORDER BY CASE WHEN mlr.status = 'pending' THEN 0 ELSE 1 END, mlr.requested_at DESC, mlr.member_loan_request_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<SavingsWithdrawalRequestDto> Items, int TotalCount)> GetSavingsWithdrawalRequestsAsync(long userId, SavingsWithdrawalRequestFilterParams filters)
    {
        var context = await GetContextAsync(userId);
        if (context is null)
            return ([], 0);

        var where = new List<string>
        {
            "swr.tenant_id = @TenantId",
            "swr.member_id = @MemberId"
        };

        var parameters = new DynamicParameters(new
        {
            context.TenantId,
            context.MemberId,
            filters.Offset,
            filters.PageSize
        });

        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            where.Add("swr.status = @Status");
            parameters.Add("Status", filters.Status.Trim());
        }

        if (filters.MemberId.HasValue)
        {
            where.Add("swr.member_id = @FilterMemberId");
            parameters.Add("FilterMemberId", filters.MemberId.Value);
        }

        var whereClause = string.Join(" AND ", where);

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM coop.savings_withdrawal_requests swr
            INNER JOIN coop.members m ON m.member_id = swr.member_id
            WHERE {whereClause}
            """, parameters);

        var items = await connection.QueryAsync<SavingsWithdrawalRequestDto>($"""
            SELECT
                swr.savings_withdrawal_request_id AS SavingsWithdrawalRequestId,
                swr.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                swr.request_no AS RequestNo,
                swr.savings_product_id AS SavingsProductId,
                sp.product_code AS SavingsProductCode,
                sp.product_name AS SavingsProductName,
                swr.amount AS Amount,
                swr.status AS Status,
                swr.note AS Note,
                swr.reviewer_note AS ReviewerNote,
                swr.approved_savings_transaction_id AS ApprovedSavingsTransactionId,
                swr.requested_at AS RequestedAt,
                swr.reviewed_at AS ReviewedAt
            FROM coop.savings_withdrawal_requests swr
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = swr.savings_product_id
            INNER JOIN coop.members m ON m.member_id = swr.member_id
            WHERE {whereClause}
            ORDER BY swr.requested_at DESC, swr.savings_withdrawal_request_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);

        return (items, totalCount);
    }

    public async Task<SavingsWithdrawalRequestDto?> GetSavingsWithdrawalRequestByIdAsync(long userId, long savingsWithdrawalRequestId)
    {
        var context = await GetContextAsync(userId);
        if (context is null)
            return null;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SavingsWithdrawalRequestDto>(
            """
            SELECT
                swr.savings_withdrawal_request_id AS SavingsWithdrawalRequestId,
                swr.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                swr.request_no AS RequestNo,
                swr.savings_product_id AS SavingsProductId,
                sp.product_code AS SavingsProductCode,
                sp.product_name AS SavingsProductName,
                swr.amount AS Amount,
                swr.status AS Status,
                swr.note AS Note,
                swr.reviewer_note AS ReviewerNote,
                swr.approved_savings_transaction_id AS ApprovedSavingsTransactionId,
                swr.requested_at AS RequestedAt,
                swr.reviewed_at AS ReviewedAt
            FROM coop.savings_withdrawal_requests swr
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = swr.savings_product_id
            INNER JOIN coop.members m ON m.member_id = swr.member_id
            WHERE swr.tenant_id = @TenantId
              AND swr.member_id = @MemberId
              AND swr.savings_withdrawal_request_id = @SavingsWithdrawalRequestId
            """,
            new
            {
                context.TenantId,
                context.MemberId,
                SavingsWithdrawalRequestId = savingsWithdrawalRequestId
            });
    }

    public async Task<long> CreateSavingsWithdrawalRequestAsync(long userId, CreateSavingsWithdrawalRequest request)
    {
        var context = await GetContextAsync(userId)
            ?? throw new InvalidOperationException("Member portal profile not found.");

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.savings_withdrawal_requests (
                tenant_id, member_id, savings_product_id, request_no, amount,
                status, note, requested_at, created_by_user_id, created_at
            )
            VALUES (
                @TenantId, @MemberId, @SavingsProductId,
                CONCAT('SWR-', FORMAT(sysutcdatetime(), 'yyyyMMddHHmmss'), '-', RIGHT(CONCAT('000000', ABS(CHECKSUM(NEWID()))), 6)),
                @Amount, 'pending', @Note, sysutcdatetime(), @CreatedByUserId, sysutcdatetime()
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                context.TenantId,
                context.MemberId,
                request.SavingsProductId,
                request.Amount,
                request.Note,
                CreatedByUserId = userId
            });
    }

    public async Task<(IEnumerable<SavingsWithdrawalRequestDto> Items, int TotalCount)> GetSavingsWithdrawalRequestsForApprovalAsync(long tenantId, SavingsWithdrawalRequestFilterParams filters)
    {
        var where = new List<string> { "swr.tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, filters.Offset, filters.PageSize });

        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            where.Add("swr.status = @Status");
            parameters.Add("Status", filters.Status.Trim());
        }

        if (filters.MemberId.HasValue)
        {
            where.Add("swr.member_id = @MemberId");
            parameters.Add("MemberId", filters.MemberId.Value);
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM coop.savings_withdrawal_requests swr
            INNER JOIN coop.members m ON m.member_id = swr.member_id
            WHERE {whereClause}
            """, parameters);

        var items = await connection.QueryAsync<SavingsWithdrawalRequestDto>($"""
            SELECT
                swr.savings_withdrawal_request_id AS SavingsWithdrawalRequestId,
                swr.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                swr.request_no AS RequestNo,
                swr.savings_product_id AS SavingsProductId,
                sp.product_code AS SavingsProductCode,
                sp.product_name AS SavingsProductName,
                swr.amount AS Amount,
                swr.status AS Status,
                swr.note AS Note,
                swr.reviewer_note AS ReviewerNote,
                swr.approved_savings_transaction_id AS ApprovedSavingsTransactionId,
                swr.requested_at AS RequestedAt,
                swr.reviewed_at AS ReviewedAt
            FROM coop.savings_withdrawal_requests swr
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = swr.savings_product_id
            INNER JOIN coop.members m ON m.member_id = swr.member_id
            WHERE {whereClause}
            ORDER BY CASE WHEN swr.status = 'pending' THEN 0 ELSE 1 END, swr.requested_at DESC, swr.savings_withdrawal_request_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);

        return (items, totalCount);
    }

    public async Task<MemberLoanRequestDto?> ApproveLoanRequestAsync(long approverUserId, long tenantId, long memberLoanRequestId, ApproveMemberLoanRequest request)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var loanRequest = await connection.QuerySingleOrDefaultAsync<LoanRequestApprovalRow>(
            """
            SELECT
                mlr.member_loan_request_id AS MemberLoanRequestId,
                mlr.tenant_id AS TenantId,
                mlr.member_id AS MemberId,
                mlr.loan_product_id AS LoanProductId,
                mlr.request_no AS RequestNo,
                mlr.principal_amount AS PrincipalAmount,
                mlr.proposed_term_months AS ProposedTermMonths,
                mlr.status AS Status,
                mlr.note AS Note,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                m.member_status AS MemberStatus,
                lp.product_code AS LoanProductCode,
                lp.product_name AS LoanProductName,
                lp.default_flat_interest_rate_pct AS DefaultFlatInterestRatePct,
                lp.min_flat_interest_rate_pct AS MinFlatInterestRatePct,
                lp.max_flat_interest_rate_pct AS MaxFlatInterestRatePct,
                lp.default_term_months AS DefaultTermMonths,
                lp.min_term_months AS MinTermMonths,
                lp.max_term_months AS MaxTermMonths,
                lp.min_principal_amount AS MinPrincipalAmount,
                lp.max_principal_amount AS MaxPrincipalAmount,
                lp.default_admin_fee_amount AS DefaultAdminFeeAmount,
                lp.default_penalty_amount AS DefaultPenaltyAmount,
                lp.is_active AS LoanProductIsActive
            FROM coop.member_loan_requests mlr WITH (UPDLOCK, ROWLOCK)
            INNER JOIN coop.members m ON m.member_id = mlr.member_id
            INNER JOIN coop.loan_products lp ON lp.loan_product_id = mlr.loan_product_id
            WHERE mlr.tenant_id = @TenantId
              AND mlr.member_loan_request_id = @MemberLoanRequestId
            """,
            new { TenantId = tenantId, MemberLoanRequestId = memberLoanRequestId },
            transaction);

        if (loanRequest is null)
        {
            transaction.Rollback();
            return null;
        }

        if (!string.Equals(loanRequest.Status, "pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only pending loan requests can be approved.");

        if (!loanRequest.LoanProductIsActive)
            throw new InvalidOperationException("Loan product is inactive.");

        if (!string.Equals(loanRequest.MemberStatus, "active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Member is not active.");

        var termMonths = request.TermMonths ?? loanRequest.ProposedTermMonths;
        var interestRate = request.FlatInterestRatePct ?? loanRequest.DefaultFlatInterestRatePct;
        var adminFee = request.AdminFeeAmount ?? loanRequest.DefaultAdminFeeAmount;
        var penalty = request.PenaltyAmount ?? loanRequest.DefaultPenaltyAmount;
        var loanDate = request.LoanDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (loanRequest.MinPrincipalAmount.HasValue && loanRequest.PrincipalAmount < loanRequest.MinPrincipalAmount.Value)
            throw new InvalidOperationException("Loan principal is below the product minimum.");

        if (loanRequest.MaxPrincipalAmount.HasValue && loanRequest.PrincipalAmount > loanRequest.MaxPrincipalAmount.Value)
            throw new InvalidOperationException("Loan principal exceeds the product maximum.");

        if (loanRequest.MinTermMonths.HasValue && termMonths < loanRequest.MinTermMonths.Value)
            throw new InvalidOperationException("Loan term is below the product minimum.");

        if (loanRequest.MaxTermMonths.HasValue && termMonths > loanRequest.MaxTermMonths.Value)
            throw new InvalidOperationException("Loan term exceeds the product maximum.");

        if (loanRequest.MinFlatInterestRatePct.HasValue && interestRate < loanRequest.MinFlatInterestRatePct.Value)
            throw new InvalidOperationException("Interest rate is below the product minimum.");

        if (loanRequest.MaxFlatInterestRatePct.HasValue && interestRate > loanRequest.MaxFlatInterestRatePct.Value)
            throw new InvalidOperationException("Interest rate exceeds the product maximum.");

        var loanNoExists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM coop.loans WHERE tenant_id = @TenantId AND loan_no = @LoanNo",
            new { TenantId = tenantId, request.LoanNo },
            transaction);

        if (loanNoExists > 0)
            throw new InvalidOperationException($"Loan number '{request.LoanNo}' is already in use.");

        var totalInterest = Math.Round(loanRequest.PrincipalAmount * (interestRate / 100m) * termMonths, 2);
        var totalPayable = loanRequest.PrincipalAmount + totalInterest + adminFee;
        var installment = Math.Round(totalPayable / termMonths, 2);

        var loanId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.loans (
                tenant_id, member_id, loan_product_id, loan_no, loan_date, principal_amount, flat_interest_rate_pct, term_months,
                admin_fee_amount, penalty_amount, installment_amount, total_interest_amount, total_payable_amount,
                outstanding_principal_amount, outstanding_total_amount, status, disbursed_at, approved_by_user_id, note, created_at, updated_at
            )
            VALUES (
                @TenantId, @MemberId, @LoanProductId, @LoanNo, @LoanDate, @PrincipalAmount, @FlatInterestRatePct, @TermMonths,
                @AdminFeeAmount, @PenaltyAmount, @InstallmentAmount, @TotalInterestAmount, @TotalPayableAmount,
                @OutstandingPrincipalAmount, @OutstandingTotalAmount, 'active', sysutcdatetime(), @ApprovedByUserId, @Note, sysutcdatetime(), NULL
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                TenantId = tenantId,
                loanRequest.MemberId,
                loanRequest.LoanProductId,
                LoanNo = request.LoanNo.Trim(),
                LoanDate = loanDate,
                PrincipalAmount = loanRequest.PrincipalAmount,
                FlatInterestRatePct = interestRate,
                TermMonths = termMonths,
                AdminFeeAmount = adminFee,
                PenaltyAmount = penalty,
                InstallmentAmount = installment,
                TotalInterestAmount = totalInterest,
                TotalPayableAmount = totalPayable,
                OutstandingPrincipalAmount = loanRequest.PrincipalAmount,
                OutstandingTotalAmount = totalPayable,
                ApprovedByUserId = approverUserId,
                Note = string.IsNullOrWhiteSpace(request.ReviewerNote) ? loanRequest.Note : request.ReviewerNote.Trim()
            },
            transaction);

        var principalPerInstallment = Math.Round(loanRequest.PrincipalAmount / termMonths, 2);
        var interestPerInstallment = Math.Round(totalInterest / termMonths, 2);
        for (var i = 1; i <= termMonths; i++)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO coop.loan_installment_schedules (loan_id, installment_no, due_date, principal_due_amount, interest_due_amount, installment_amount, paid_amount, installment_status, created_at)
                VALUES (@LoanId, @InstallmentNo, @DueDate, @PrincipalDueAmount, @InterestDueAmount, @InstallmentAmount, 0, 'unpaid', sysutcdatetime())
                """,
                new
                {
                    LoanId = loanId,
                    InstallmentNo = i,
                    DueDate = loanDate.AddMonths(i),
                    PrincipalDueAmount = principalPerInstallment,
                    InterestDueAmount = interestPerInstallment,
                    InstallmentAmount = installment
                },
                transaction);
        }

        await connection.ExecuteAsync(
            """
            INSERT INTO coop.member_transactions (
                tenant_id, member_id, transaction_no, transaction_ts, source_module, source_table, source_id,
                entry_type, amount, description, reference_no, created_by_user_id, created_at
            )
            VALUES (
                @TenantId, @MemberId, @TransactionNo, sysutcdatetime(), 'loan', 'loans', @SourceId,
                'credit', @Amount, @Description, @ReferenceNo, @CreatedByUserId, sysutcdatetime()
            )
            """,
            new
            {
                TenantId = tenantId,
                loanRequest.MemberId,
                TransactionNo = $"LN-{request.LoanNo.Trim()}",
                SourceId = loanId,
                Amount = loanRequest.PrincipalAmount,
                Description = $"Loan disbursement {request.LoanNo.Trim()}",
                ReferenceNo = loanRequest.RequestNo,
                CreatedByUserId = approverUserId
            },
            transaction);

        await connection.ExecuteAsync(
            """
            UPDATE coop.member_loan_requests
            SET status = 'approved',
                reviewer_note = @ReviewerNote,
                approved_loan_id = @ApprovedLoanId,
                reviewed_at = sysutcdatetime(),
                reviewed_by_user_id = @ReviewedByUserId,
                updated_at = sysutcdatetime()
            WHERE member_loan_request_id = @MemberLoanRequestId
            """,
            new
            {
                ReviewerNote = request.ReviewerNote?.Trim(),
                ApprovedLoanId = loanId,
                ReviewedByUserId = approverUserId,
                MemberLoanRequestId = memberLoanRequestId
            },
            transaction);

        var result = await connection.QuerySingleAsync<MemberLoanRequestDto>(
            """
            SELECT
                mlr.member_loan_request_id AS MemberLoanRequestId,
                mlr.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                mlr.request_no AS RequestNo,
                mlr.loan_product_id AS LoanProductId,
                lp.product_code AS LoanProductCode,
                lp.product_name AS LoanProductName,
                mlr.principal_amount AS PrincipalAmount,
                mlr.proposed_term_months AS ProposedTermMonths,
                mlr.status AS Status,
                mlr.note AS Note,
                mlr.reviewer_note AS ReviewerNote,
                mlr.approved_loan_id AS ApprovedLoanId,
                mlr.requested_at AS RequestedAt,
                mlr.reviewed_at AS ReviewedAt
            FROM coop.member_loan_requests mlr
            INNER JOIN coop.members m ON m.member_id = mlr.member_id
            INNER JOIN coop.loan_products lp ON lp.loan_product_id = mlr.loan_product_id
            WHERE mlr.member_loan_request_id = @MemberLoanRequestId
            """,
            new { MemberLoanRequestId = memberLoanRequestId },
            transaction);

        transaction.Commit();
        return result;
    }

    public async Task<MemberLoanRequestDto?> RejectLoanRequestAsync(long approverUserId, long tenantId, long memberLoanRequestId, RejectMemberLoanRequest request)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var exists = await connection.QuerySingleOrDefaultAsync<string>(
            """
            SELECT status
            FROM coop.member_loan_requests WITH (UPDLOCK, ROWLOCK)
            WHERE tenant_id = @TenantId
              AND member_loan_request_id = @MemberLoanRequestId
            """,
            new { TenantId = tenantId, MemberLoanRequestId = memberLoanRequestId },
            transaction);

        if (exists is null)
        {
            transaction.Rollback();
            return null;
        }

        if (!string.Equals(exists, "pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only pending loan requests can be rejected.");

        await connection.ExecuteAsync(
            """
            UPDATE coop.member_loan_requests
            SET status = 'rejected',
                reviewer_note = @ReviewerNote,
                reviewed_at = sysutcdatetime(),
                reviewed_by_user_id = @ReviewedByUserId,
                updated_at = sysutcdatetime()
            WHERE member_loan_request_id = @MemberLoanRequestId
            """,
            new
            {
                ReviewerNote = request.ReviewerNote.Trim(),
                ReviewedByUserId = approverUserId,
                MemberLoanRequestId = memberLoanRequestId
            },
            transaction);

        var result = await connection.QuerySingleAsync<MemberLoanRequestDto>(
            """
            SELECT
                mlr.member_loan_request_id AS MemberLoanRequestId,
                mlr.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                mlr.request_no AS RequestNo,
                mlr.loan_product_id AS LoanProductId,
                lp.product_code AS LoanProductCode,
                lp.product_name AS LoanProductName,
                mlr.principal_amount AS PrincipalAmount,
                mlr.proposed_term_months AS ProposedTermMonths,
                mlr.status AS Status,
                mlr.note AS Note,
                mlr.reviewer_note AS ReviewerNote,
                mlr.approved_loan_id AS ApprovedLoanId,
                mlr.requested_at AS RequestedAt,
                mlr.reviewed_at AS ReviewedAt
            FROM coop.member_loan_requests mlr
            INNER JOIN coop.members m ON m.member_id = mlr.member_id
            INNER JOIN coop.loan_products lp ON lp.loan_product_id = mlr.loan_product_id
            WHERE mlr.member_loan_request_id = @MemberLoanRequestId
            """,
            new { MemberLoanRequestId = memberLoanRequestId },
            transaction);

        transaction.Commit();
        return result;
    }

    public async Task<SavingsWithdrawalRequestDto?> ApproveSavingsWithdrawalRequestAsync(long approverUserId, long tenantId, long savingsWithdrawalRequestId, ApproveSavingsWithdrawalRequest request)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var withdrawalRequest = await connection.QuerySingleOrDefaultAsync<WithdrawalRequestApprovalRow>(
            """
            SELECT
                swr.savings_withdrawal_request_id AS SavingsWithdrawalRequestId,
                swr.tenant_id AS TenantId,
                swr.member_id AS MemberId,
                swr.savings_product_id AS SavingsProductId,
                swr.request_no AS RequestNo,
                swr.amount AS Amount,
                swr.status AS Status,
                swr.note AS Note,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                m.member_status AS MemberStatus,
                sp.product_code AS SavingsProductCode,
                sp.product_name AS SavingsProductName,
                sp.savings_kind AS SavingsKind,
                sp.periodicity AS Periodicity,
                sp.is_active AS SavingsProductIsActive,
                msa.member_savings_account_id AS MemberSavingsAccountId
            FROM coop.savings_withdrawal_requests swr WITH (UPDLOCK, ROWLOCK)
            INNER JOIN coop.members m ON m.member_id = swr.member_id
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = swr.savings_product_id
            LEFT JOIN coop.member_savings_accounts msa
                ON msa.member_id = swr.member_id
               AND msa.savings_product_id = swr.savings_product_id
            WHERE swr.tenant_id = @TenantId
              AND swr.savings_withdrawal_request_id = @SavingsWithdrawalRequestId
            """,
            new { TenantId = tenantId, SavingsWithdrawalRequestId = savingsWithdrawalRequestId },
            transaction);

        if (withdrawalRequest is null)
        {
            transaction.Rollback();
            return null;
        }

        if (!string.Equals(withdrawalRequest.Status, "pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only pending withdrawal requests can be approved.");

        if (!withdrawalRequest.SavingsProductIsActive)
            throw new InvalidOperationException("Savings product is inactive.");

        if (!string.Equals(withdrawalRequest.MemberStatus, "active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Member is not active.");

        if (!withdrawalRequest.MemberSavingsAccountId.HasValue)
            throw new InvalidOperationException("Savings account for this product was not found.");

        var balance = await connection.ExecuteScalarAsync<decimal>(
            """
            SELECT COALESCE(SUM(CASE mt.entry_type WHEN 'credit' THEN st.amount ELSE -st.amount END), 0)
            FROM coop.savings_transactions st
            INNER JOIN coop.member_transactions mt ON mt.member_transaction_id = st.member_transaction_id
            WHERE st.member_savings_account_id = @MemberSavingsAccountId
            """,
            new { MemberSavingsAccountId = withdrawalRequest.MemberSavingsAccountId.Value },
            transaction);

        if (balance < withdrawalRequest.Amount)
            throw new InvalidOperationException("Insufficient savings balance for this withdrawal request.");

        var txNoExists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM coop.savings_transactions WHERE tenant_id = @TenantId AND transaction_no = @TransactionNo",
            new { TenantId = tenantId, request.TransactionNo },
            transaction);

        if (txNoExists > 0)
            throw new InvalidOperationException($"Savings transaction number '{request.TransactionNo}' is already in use.");

        var transactionTs = request.TransactionTs ?? DateTime.UtcNow;

        var memberTransactionId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.member_transactions (
                tenant_id, member_id, transaction_no, transaction_ts, source_module, source_table, source_id,
                entry_type, amount, description, reference_no, created_by_user_id, created_at
            )
            VALUES (
                @TenantId, @MemberId, @TransactionNo, @TransactionTs, 'saving', 'savings_transactions', 0,
                'debit', @Amount, @Description, @ReferenceNo, @CreatedByUserId, sysutcdatetime()
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                TenantId = tenantId,
                withdrawalRequest.MemberId,
                TransactionNo = request.TransactionNo.Trim(),
                TransactionTs = transactionTs,
                Amount = withdrawalRequest.Amount,
                Description = $"{withdrawalRequest.SavingsProductName} withdrawal",
                ReferenceNo = withdrawalRequest.RequestNo,
                CreatedByUserId = approverUserId
            },
            transaction);

        var savingsTransactionId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.savings_transactions (
                tenant_id, member_savings_account_id, member_id, transaction_no, transaction_ts, transaction_type,
                amount, period_year, period_month, note, member_transaction_id, created_by_user_id, created_at
            )
            VALUES (
                @TenantId, @MemberSavingsAccountId, @MemberId, @TransactionNo, @TransactionTs, 'withdrawal',
                @Amount, NULL, NULL, @Note, @MemberTransactionId, @CreatedByUserId, sysutcdatetime()
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                TenantId = tenantId,
                MemberSavingsAccountId = withdrawalRequest.MemberSavingsAccountId.Value,
                withdrawalRequest.MemberId,
                TransactionNo = request.TransactionNo.Trim(),
                TransactionTs = transactionTs,
                Amount = withdrawalRequest.Amount,
                Note = string.IsNullOrWhiteSpace(request.ReviewerNote) ? withdrawalRequest.Note : request.ReviewerNote.Trim(),
                MemberTransactionId = memberTransactionId,
                CreatedByUserId = approverUserId
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

        await connection.ExecuteAsync(
            """
            UPDATE coop.savings_withdrawal_requests
            SET status = 'approved',
                reviewer_note = @ReviewerNote,
                approved_savings_transaction_id = @ApprovedSavingsTransactionId,
                reviewed_at = sysutcdatetime(),
                reviewed_by_user_id = @ReviewedByUserId,
                updated_at = sysutcdatetime()
            WHERE savings_withdrawal_request_id = @SavingsWithdrawalRequestId
            """,
            new
            {
                ReviewerNote = request.ReviewerNote?.Trim(),
                ApprovedSavingsTransactionId = savingsTransactionId,
                ReviewedByUserId = approverUserId,
                SavingsWithdrawalRequestId = savingsWithdrawalRequestId
            },
            transaction);

        var result = await connection.QuerySingleAsync<SavingsWithdrawalRequestDto>(
            """
            SELECT
                swr.savings_withdrawal_request_id AS SavingsWithdrawalRequestId,
                swr.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                swr.request_no AS RequestNo,
                swr.savings_product_id AS SavingsProductId,
                sp.product_code AS SavingsProductCode,
                sp.product_name AS SavingsProductName,
                swr.amount AS Amount,
                swr.status AS Status,
                swr.note AS Note,
                swr.reviewer_note AS ReviewerNote,
                swr.approved_savings_transaction_id AS ApprovedSavingsTransactionId,
                swr.requested_at AS RequestedAt,
                swr.reviewed_at AS ReviewedAt
            FROM coop.savings_withdrawal_requests swr
            INNER JOIN coop.members m ON m.member_id = swr.member_id
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = swr.savings_product_id
            WHERE swr.savings_withdrawal_request_id = @SavingsWithdrawalRequestId
            """,
            new { SavingsWithdrawalRequestId = savingsWithdrawalRequestId },
            transaction);

        transaction.Commit();
        return result;
    }

    public async Task<SavingsWithdrawalRequestDto?> RejectSavingsWithdrawalRequestAsync(long approverUserId, long tenantId, long savingsWithdrawalRequestId, RejectSavingsWithdrawalRequest request)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var exists = await connection.QuerySingleOrDefaultAsync<string>(
            """
            SELECT status
            FROM coop.savings_withdrawal_requests WITH (UPDLOCK, ROWLOCK)
            WHERE tenant_id = @TenantId
              AND savings_withdrawal_request_id = @SavingsWithdrawalRequestId
            """,
            new { TenantId = tenantId, SavingsWithdrawalRequestId = savingsWithdrawalRequestId },
            transaction);

        if (exists is null)
        {
            transaction.Rollback();
            return null;
        }

        if (!string.Equals(exists, "pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only pending withdrawal requests can be rejected.");

        await connection.ExecuteAsync(
            """
            UPDATE coop.savings_withdrawal_requests
            SET status = 'rejected',
                reviewer_note = @ReviewerNote,
                reviewed_at = sysutcdatetime(),
                reviewed_by_user_id = @ReviewedByUserId,
                updated_at = sysutcdatetime()
            WHERE savings_withdrawal_request_id = @SavingsWithdrawalRequestId
            """,
            new
            {
                ReviewerNote = request.ReviewerNote.Trim(),
                ReviewedByUserId = approverUserId,
                SavingsWithdrawalRequestId = savingsWithdrawalRequestId
            },
            transaction);

        var result = await connection.QuerySingleAsync<SavingsWithdrawalRequestDto>(
            """
            SELECT
                swr.savings_withdrawal_request_id AS SavingsWithdrawalRequestId,
                swr.member_id AS MemberId,
                m.member_no AS MemberNo,
                m.full_name AS FullName,
                swr.request_no AS RequestNo,
                swr.savings_product_id AS SavingsProductId,
                sp.product_code AS SavingsProductCode,
                sp.product_name AS SavingsProductName,
                swr.amount AS Amount,
                swr.status AS Status,
                swr.note AS Note,
                swr.reviewer_note AS ReviewerNote,
                swr.approved_savings_transaction_id AS ApprovedSavingsTransactionId,
                swr.requested_at AS RequestedAt,
                swr.reviewed_at AS ReviewedAt
            FROM coop.savings_withdrawal_requests swr
            INNER JOIN coop.members m ON m.member_id = swr.member_id
            INNER JOIN coop.savings_products sp ON sp.savings_product_id = swr.savings_product_id
            WHERE swr.savings_withdrawal_request_id = @SavingsWithdrawalRequestId
            """,
            new { SavingsWithdrawalRequestId = savingsWithdrawalRequestId },
            transaction);

        transaction.Commit();
        return result;
    }

    private async Task<MemberPortalContext?> GetContextAsync(long userId)
    {
        const string sql = """
            SELECT TOP (1)
                u.tenant_id AS TenantId,
                u.member_id AS MemberId
            FROM coop.users u
            WHERE u.user_id = @UserId
              AND u.user_type = 'member'
              AND u.is_active = 1
              AND u.member_id IS NOT NULL
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<MemberPortalContext>(sql, new { UserId = userId });
    }

    private sealed record MemberPortalContext(long TenantId, long MemberId);

    private sealed record MemberPortalLoanRow(
        long LoanId,
        long LoanProductId,
        string LoanProductName,
        string LoanNo,
        DateOnly LoanDate,
        decimal PrincipalAmount,
        decimal FlatInterestRatePct,
        int TermMonths,
        decimal AdminFeeAmount,
        decimal PenaltyAmount,
        decimal InstallmentAmount,
        decimal TotalInterestAmount,
        decimal TotalPayableAmount,
        decimal OutstandingPrincipalAmount,
        decimal OutstandingTotalAmount,
        string Status,
        DateOnly? NextDueDate,
        decimal? NextDueAmount,
        int PendingInstallmentCount,
        string? Note);

    private sealed record MemberPortalLoanInstallmentRow(
        long LoanId,
        int InstallmentNo,
        DateOnly DueDate,
        decimal PrincipalDueAmount,
        decimal InterestDueAmount,
        decimal InstallmentAmount,
        decimal PaidAmount,
        string InstallmentStatus,
        DateTime? SettledAt);

    private sealed record LoanRequestApprovalRow(
        long MemberLoanRequestId,
        long TenantId,
        long MemberId,
        long LoanProductId,
        string RequestNo,
        decimal PrincipalAmount,
        int ProposedTermMonths,
        string Status,
        string? Note,
        string MemberNo,
        string FullName,
        string MemberStatus,
        string LoanProductCode,
        string LoanProductName,
        decimal DefaultFlatInterestRatePct,
        decimal? MinFlatInterestRatePct,
        decimal? MaxFlatInterestRatePct,
        int DefaultTermMonths,
        int? MinTermMonths,
        int? MaxTermMonths,
        decimal? MinPrincipalAmount,
        decimal? MaxPrincipalAmount,
        decimal DefaultAdminFeeAmount,
        decimal DefaultPenaltyAmount,
        bool LoanProductIsActive);

    private sealed record WithdrawalRequestApprovalRow(
        long SavingsWithdrawalRequestId,
        long TenantId,
        long MemberId,
        long SavingsProductId,
        string RequestNo,
        decimal Amount,
        string Status,
        string? Note,
        string MemberNo,
        string FullName,
        string MemberStatus,
        string SavingsProductCode,
        string SavingsProductName,
        string SavingsKind,
        string Periodicity,
        bool SavingsProductIsActive,
        long? MemberSavingsAccountId);
}
