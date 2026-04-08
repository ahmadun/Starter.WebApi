using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class LoanRepository : BaseRepository<Loan>, ILoanRepository
{
    public LoanRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<(IEnumerable<Loan> Items, int TotalCount)> GetAllAsync(long tenantId, LoanFilterParams filters)
    {
        var where = new List<string> { "tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, Offset = filters.Offset, PageSize = filters.PageSize });
        if (filters.MemberId.HasValue)
        {
            where.Add("member_id = @MemberId");
            parameters.Add("MemberId", filters.MemberId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            where.Add("status = @Status");
            parameters.Add("Status", filters.Status);
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM coop.loans WHERE {whereClause}", parameters);
        var items = await connection.QueryAsync<Loan>($"""
            SELECT loan_id, tenant_id, member_id, loan_product_id, loan_no, loan_date, principal_amount, flat_interest_rate_pct, term_months,
                   admin_fee_amount, penalty_amount, installment_amount, total_interest_amount, total_payable_amount,
                   outstanding_principal_amount, outstanding_total_amount, status, disbursed_at, approved_by_user_id, note, created_at, updated_at
            FROM coop.loans
            WHERE {whereClause}
            ORDER BY loan_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public Task<Loan?> GetByIdAsync(long tenantId, long loanId) => QuerySingleOrDefaultAsync(
        """
        SELECT loan_id, tenant_id, member_id, loan_product_id, loan_no, loan_date, principal_amount, flat_interest_rate_pct, term_months,
               admin_fee_amount, penalty_amount, installment_amount, total_interest_amount, total_payable_amount,
               outstanding_principal_amount, outstanding_total_amount, status, disbursed_at, approved_by_user_id, note, created_at, updated_at
        FROM coop.loans
        WHERE tenant_id = @TenantId AND loan_id = @LoanId
        """, new { TenantId = tenantId, LoanId = loanId });

    public async Task<IReadOnlyCollection<LoanInstallmentScheduleDto>> GetInstallmentsAsync(long loanId)
    {
        using var connection = CreateConnection();
        var items = await connection.QueryAsync<LoanInstallmentScheduleDto>(
            """
            SELECT
                installment_no AS InstallmentNo,
                due_date AS DueDate,
                principal_due_amount AS PrincipalDueAmount,
                interest_due_amount AS InterestDueAmount,
                installment_amount AS InstallmentAmount,
                paid_amount AS PaidAmount,
                installment_status AS InstallmentStatus
            FROM coop.loan_installment_schedules
            WHERE loan_id = @LoanId
            ORDER BY installment_no
            """, new { LoanId = loanId });
        return items.ToList();
    }

    public async Task<(IEnumerable<LoanPaymentDto> Items, int TotalCount)> GetPaymentsAsync(long tenantId, LoanPaymentFilterParams filters)
    {
        var where = new List<string> { "lp.tenant_id = @TenantId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, filters.Offset, filters.PageSize });

        if (filters.LoanId.HasValue)
        {
            where.Add("lp.loan_id = @LoanId");
            parameters.Add("LoanId", filters.LoanId.Value);
        }

        if (filters.MemberId.HasValue)
        {
            where.Add("lp.member_id = @MemberId");
            parameters.Add("MemberId", filters.MemberId.Value);
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM coop.loan_payments lp
            INNER JOIN coop.loans l ON l.loan_id = lp.loan_id
            INNER JOIN coop.members m ON m.member_id = lp.member_id
            WHERE {whereClause}
            """, parameters);

        var items = await connection.QueryAsync<LoanPaymentDto>($"""
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
            WHERE {whereClause}
            ORDER BY lp.payment_ts DESC, lp.loan_payment_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);

        return (items, totalCount);
    }

    public async Task<LoanPaymentDto?> GetPaymentByIdAsync(long tenantId, long loanPaymentId)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<LoanPaymentDto>(
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
              AND lp.loan_payment_id = @LoanPaymentId
            """,
            new { TenantId = tenantId, LoanPaymentId = loanPaymentId });
    }

    public async Task<bool> LoanNoExistsAsync(long tenantId, string loanNo)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM coop.loans WHERE tenant_id = @TenantId AND loan_no = @LoanNo", new { TenantId = tenantId, LoanNo = loanNo }) > 0;
    }

    public async Task<bool> PaymentNoExistsAsync(long tenantId, string paymentNo)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM coop.loan_payments WHERE tenant_id = @TenantId AND payment_no = @PaymentNo", new { TenantId = tenantId, PaymentNo = paymentNo }) > 0;
    }

    public async Task<long> CreateAsync(long userId, CreateLoanRequest request, LoanProduct loanProduct)
    {
        var interestRate = request.FlatInterestRatePct ?? loanProduct.DefaultFlatInterestRatePct;
        var termMonths = request.TermMonths ?? loanProduct.DefaultTermMonths;
        var adminFee = request.AdminFeeAmount ?? loanProduct.DefaultAdminFeeAmount;
        var penalty = request.PenaltyAmount ?? loanProduct.DefaultPenaltyAmount;
        var totalInterest = Math.Round(request.PrincipalAmount * (interestRate / 100m) * termMonths, 2);
        var totalPayable = request.PrincipalAmount + totalInterest + adminFee;
        var installment = Math.Round(totalPayable / termMonths, 2);

        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        var loanId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.loans (tenant_id, member_id, loan_product_id, loan_no, loan_date, principal_amount, flat_interest_rate_pct, term_months, admin_fee_amount, penalty_amount,
                installment_amount, total_interest_amount, total_payable_amount, outstanding_principal_amount, outstanding_total_amount, status, disbursed_at, approved_by_user_id, note, created_at, updated_at)
            VALUES (@TenantId, @MemberId, @LoanProductId, @LoanNo, @LoanDate, @PrincipalAmount, @FlatInterestRatePct, @TermMonths, @AdminFeeAmount, @PenaltyAmount,
                @InstallmentAmount, @TotalInterestAmount, @TotalPayableAmount, @OutstandingPrincipalAmount, @OutstandingTotalAmount, 'active', sysutcdatetime(), @ApprovedByUserId, @Note, sysutcdatetime(), NULL);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                request.TenantId,
                request.MemberId,
                request.LoanProductId,
                LoanNo = request.LoanNo.Trim(),
                request.LoanDate,
                request.PrincipalAmount,
                FlatInterestRatePct = interestRate,
                TermMonths = termMonths,
                AdminFeeAmount = adminFee,
                PenaltyAmount = penalty,
                InstallmentAmount = installment,
                TotalInterestAmount = totalInterest,
                TotalPayableAmount = totalPayable,
                OutstandingPrincipalAmount = request.PrincipalAmount,
                OutstandingTotalAmount = totalPayable,
                ApprovedByUserId = userId,
                request.Note
            },
            transaction);

        var principalPerInstallment = Math.Round(request.PrincipalAmount / termMonths, 2);
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
                    DueDate = request.LoanDate.AddMonths(i),
                    PrincipalDueAmount = principalPerInstallment,
                    InterestDueAmount = interestPerInstallment,
                    InstallmentAmount = installment
                },
                transaction);
        }

        await connection.ExecuteAsync(
            """
            INSERT INTO coop.member_transactions (tenant_id, member_id, transaction_no, transaction_ts, source_module, source_table, source_id, entry_type, amount, description, created_by_user_id, created_at)
            VALUES (@TenantId, @MemberId, @TransactionNo, sysutcdatetime(), 'loan', 'loans', @SourceId, 'credit', @Amount, @Description, @CreatedByUserId, sysutcdatetime())
            """,
            new
            {
                request.TenantId,
                request.MemberId,
                TransactionNo = $"LN-{request.LoanNo.Trim()}",
                SourceId = loanId,
                Amount = request.PrincipalAmount,
                Description = $"Loan disbursement {request.LoanNo.Trim()}",
                CreatedByUserId = userId
            },
            transaction);

        transaction.Commit();
        return loanId;
    }

    public async Task<long> CreatePaymentAsync(long userId, long tenantId, long loanId, CreateLoanPaymentRequest request)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var loan = await connection.QuerySingleOrDefaultAsync<Loan>(
            """
            SELECT loan_id, tenant_id, member_id, loan_product_id, loan_no, loan_date, principal_amount, flat_interest_rate_pct, term_months,
                   admin_fee_amount, penalty_amount, installment_amount, total_interest_amount, total_payable_amount,
                   outstanding_principal_amount, outstanding_total_amount, status, disbursed_at, approved_by_user_id, note, created_at, updated_at
            FROM coop.loans WITH (UPDLOCK, ROWLOCK)
            WHERE tenant_id = @TenantId AND loan_id = @LoanId
            """,
            new { TenantId = tenantId, LoanId = loanId },
            transaction);

        if (loan is null)
            throw new InvalidOperationException("Loan was not found.");

        if (!new[] { "approved", "active", "defaulted" }.Contains(loan.Status))
            throw new InvalidOperationException("Loan is not in a payable status.");

        if (loan.OutstandingTotalAmount <= 0)
            throw new InvalidOperationException("Loan is already fully paid.");

        if (request.PaymentAmount > loan.OutstandingTotalAmount)
            throw new InvalidOperationException("Payment amount exceeds the outstanding loan balance.");

        var schedules = (await connection.QueryAsync<LoanInstallmentSchedule>(
            """
            SELECT loan_installment_schedule_id, installment_no, due_date, principal_due_amount, interest_due_amount, installment_amount, paid_amount, installment_status, settled_at
            FROM coop.loan_installment_schedules WITH (UPDLOCK, ROWLOCK)
            WHERE loan_id = @LoanId
            ORDER BY due_date, installment_no
            """,
            new { LoanId = loanId },
            transaction)).ToList();

        if (request.LoanInstallmentScheduleId.HasValue && schedules.All(x => x.LoanInstallmentScheduleId != request.LoanInstallmentScheduleId.Value))
            throw new InvalidOperationException("Loan installment schedule was not found for this loan.");

        var paymentRemaining = request.PaymentAmount;
        var totalPrincipalPaid = 0m;
        var totalInterestPaid = 0m;
        var touchedScheduleIds = new List<long>();
        var targetSchedules = request.LoanInstallmentScheduleId.HasValue
            ? schedules.Where(x => x.LoanInstallmentScheduleId == request.LoanInstallmentScheduleId.Value)
            : schedules.Where(x => x.PaidAmount < x.InstallmentAmount);

        foreach (var schedule in targetSchedules)
        {
            if (paymentRemaining <= 0)
                break;

            var remainingDue = schedule.InstallmentAmount - schedule.PaidAmount;
            if (remainingDue <= 0)
                continue;

            var allocated = Math.Min(paymentRemaining, remainingDue);
            var interestAlreadyPaid = Math.Min(schedule.PaidAmount, schedule.InterestDueAmount);
            var remainingInterest = Math.Max(schedule.InterestDueAmount - interestAlreadyPaid, 0);
            var interestPaid = Math.Min(allocated, remainingInterest);
            var principalPaid = allocated - interestPaid;
            var newPaidAmount = schedule.PaidAmount + allocated;

            await connection.ExecuteAsync(
                """
                UPDATE coop.loan_installment_schedules
                SET paid_amount = @PaidAmount
                WHERE loan_installment_schedule_id = @LoanInstallmentScheduleId
                """,
                new
                {
                    PaidAmount = newPaidAmount,
                    schedule.LoanInstallmentScheduleId
                },
                transaction);

            paymentRemaining -= allocated;
            totalInterestPaid += interestPaid;
            totalPrincipalPaid += principalPaid;
            touchedScheduleIds.Add(schedule.LoanInstallmentScheduleId);
        }

        if (paymentRemaining > 0)
            throw new InvalidOperationException("Payment amount exceeds the remaining unpaid installments.");

        await connection.ExecuteAsync(
            """
            UPDATE coop.loan_installment_schedules
            SET installment_status = CASE
                    WHEN paid_amount >= installment_amount THEN 'paid'
                    WHEN due_date < @AsOfDate THEN 'overdue'
                    WHEN paid_amount > 0 THEN 'partial'
                    ELSE 'unpaid'
                END,
                settled_at = CASE
                    WHEN paid_amount >= installment_amount THEN COALESCE(settled_at, @SettledAt)
                    ELSE NULL
                END
            WHERE loan_id = @LoanId
            """,
            new
            {
                LoanId = loanId,
                AsOfDate = DateOnly.FromDateTime(request.PaymentTs),
                SettledAt = request.PaymentTs
            },
            transaction);

        var newOutstandingPrincipal = Math.Max(loan.OutstandingPrincipalAmount - totalPrincipalPaid, 0);
        var newOutstandingTotal = Math.Max(loan.OutstandingTotalAmount - request.PaymentAmount, 0);
        var newStatus = newOutstandingTotal == 0 ? "paid_off" : loan.Status == "approved" ? "active" : loan.Status;

        await connection.ExecuteAsync(
            """
            UPDATE coop.loans
            SET outstanding_principal_amount = @OutstandingPrincipalAmount,
                outstanding_total_amount = @OutstandingTotalAmount,
                status = @Status,
                updated_at = sysutcdatetime()
            WHERE loan_id = @LoanId
            """,
            new
            {
                LoanId = loanId,
                OutstandingPrincipalAmount = newOutstandingPrincipal,
                OutstandingTotalAmount = newOutstandingTotal,
                Status = newStatus
            },
            transaction);

        var memberTransactionId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.member_transactions (
                tenant_id, member_id, transaction_no, transaction_ts, source_module, source_table, source_id,
                entry_type, amount, description, reference_no, created_by_user_id, created_at
            )
            VALUES (
                @TenantId, @MemberId, @TransactionNo, @TransactionTs, 'loan', 'loan_payments', 0,
                'debit', @Amount, @Description, @ReferenceNo, @CreatedByUserId, sysutcdatetime()
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                TenantId = tenantId,
                loan.MemberId,
                TransactionNo = request.PaymentNo,
                TransactionTs = request.PaymentTs,
                Amount = request.PaymentAmount,
                Description = $"Loan payment {loan.LoanNo}",
                ReferenceNo = request.PaymentNo,
                CreatedByUserId = userId
            },
            transaction);

        var loanPaymentId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.loan_payments (
                tenant_id, loan_id, loan_installment_schedule_id, member_id, payment_no, payment_ts,
                payment_amount, principal_paid_amount, interest_paid_amount, penalty_paid_amount, note,
                member_transaction_id, created_by_user_id, created_at
            )
            VALUES (
                @TenantId, @LoanId, @LoanInstallmentScheduleId, @MemberId, @PaymentNo, @PaymentTs,
                @PaymentAmount, @PrincipalPaidAmount, @InterestPaidAmount, 0, @Note,
                @MemberTransactionId, @CreatedByUserId, sysutcdatetime()
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                TenantId = tenantId,
                LoanId = loanId,
                LoanInstallmentScheduleId = touchedScheduleIds.Count == 1 ? (long?)touchedScheduleIds[0] : null,
                loan.MemberId,
                PaymentNo = request.PaymentNo,
                request.PaymentTs,
                request.PaymentAmount,
                PrincipalPaidAmount = totalPrincipalPaid,
                InterestPaidAmount = totalInterestPaid,
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
            new { SourceId = loanPaymentId, MemberTransactionId = memberTransactionId },
            transaction);

        transaction.Commit();
        return loanPaymentId;
    }

    public async Task<bool> DeleteAsync(long tenantId, long loanId)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var loanContext = await connection.QuerySingleOrDefaultAsync(
            """
            SELECT
                l.loan_id AS LoanId,
                l.loan_no AS LoanNo,
                mt.member_transaction_id AS MemberTransactionId
            FROM coop.loans l
            LEFT JOIN coop.member_transactions mt
                ON mt.source_module = 'loan'
               AND mt.source_table = 'loans'
               AND mt.source_id = l.loan_id
               AND mt.tenant_id = l.tenant_id
            WHERE l.tenant_id = @TenantId
              AND l.loan_id = @LoanId
            """,
            new { TenantId = tenantId, LoanId = loanId },
            transaction);

        if (loanContext is null)
        {
            transaction.Rollback();
            return false;
        }

        var hasPayments = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1)
            FROM coop.loan_payments
            WHERE tenant_id = @TenantId
              AND loan_id = @LoanId
            """,
            new { TenantId = tenantId, LoanId = loanId },
            transaction);

        if (hasPayments > 0)
            throw new InvalidOperationException("Loan cannot be deleted because it already has payment records.");

        var hasConversion = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1)
            FROM coop.member_credit_conversions
            WHERE tenant_id = @TenantId
              AND loan_id = @LoanId
            """,
            new { TenantId = tenantId, LoanId = loanId },
            transaction);

        if (hasConversion > 0)
            throw new InvalidOperationException("Loan cannot be deleted because it is linked to a member credit conversion.");

        await connection.ExecuteAsync(
            "DELETE FROM coop.loan_installment_schedules WHERE loan_id = @LoanId",
            new { LoanId = loanId },
            transaction);

        if (loanContext.MemberTransactionId is not null)
        {
            await connection.ExecuteAsync(
                "DELETE FROM coop.member_transactions WHERE member_transaction_id = @MemberTransactionId",
                new { MemberTransactionId = (long)loanContext.MemberTransactionId },
                transaction);
        }

        var affected = await connection.ExecuteAsync(
            "DELETE FROM coop.loans WHERE tenant_id = @TenantId AND loan_id = @LoanId",
            new { TenantId = tenantId, LoanId = loanId },
            transaction);

        transaction.Commit();
        return affected > 0;
    }

    public async Task<bool> DeletePaymentAsync(long tenantId, long loanPaymentId)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var payment = await connection.QuerySingleOrDefaultAsync<LoanPaymentDto>(
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
              AND lp.loan_payment_id = @LoanPaymentId
            """,
            new { TenantId = tenantId, LoanPaymentId = loanPaymentId },
            transaction);

        if (payment is null)
        {
            transaction.Rollback();
            return false;
        }

        var memberTransactionId = await connection.ExecuteScalarAsync<long?>(
            """
            SELECT member_transaction_id
            FROM coop.loan_payments
            WHERE tenant_id = @TenantId
              AND loan_payment_id = @LoanPaymentId
            """,
            new { TenantId = tenantId, LoanPaymentId = loanPaymentId },
            transaction);

        var latestPaymentId = await connection.ExecuteScalarAsync<long?>(
            """
            SELECT TOP 1 loan_payment_id
            FROM coop.loan_payments
            WHERE tenant_id = @TenantId
              AND loan_id = @LoanId
            ORDER BY payment_ts DESC, loan_payment_id DESC
            """,
            new { TenantId = tenantId, payment.LoanId },
            transaction);

        if (latestPaymentId != payment.LoanPaymentId)
            throw new InvalidOperationException("Only the latest loan payment can be cancelled.");

        var loan = await connection.QuerySingleOrDefaultAsync<Loan>(
            """
            SELECT loan_id, tenant_id, member_id, loan_product_id, loan_no, loan_date, principal_amount, flat_interest_rate_pct, term_months,
                   admin_fee_amount, penalty_amount, installment_amount, total_interest_amount, total_payable_amount,
                   outstanding_principal_amount, outstanding_total_amount, status, disbursed_at, approved_by_user_id, note, created_at, updated_at
            FROM coop.loans WITH (UPDLOCK, ROWLOCK)
            WHERE tenant_id = @TenantId AND loan_id = @LoanId
            """,
            new { TenantId = tenantId, LoanId = payment.LoanId },
            transaction);

        if (loan is null)
            throw new InvalidOperationException("Loan was not found.");

        var schedules = (await connection.QueryAsync<LoanInstallmentSchedule>(
            """
            SELECT loan_installment_schedule_id, installment_no, due_date, principal_due_amount, interest_due_amount, installment_amount, paid_amount, installment_status, settled_at
            FROM coop.loan_installment_schedules WITH (UPDLOCK, ROWLOCK)
            WHERE loan_id = @LoanId
            ORDER BY due_date, installment_no
            """,
            new { LoanId = payment.LoanId },
            transaction)).ToList();

        var paymentRemaining = payment.PaymentAmount;

        foreach (var schedule in schedules.Where(x => x.PaidAmount > 0).OrderByDescending(x => x.DueDate).ThenByDescending(x => x.InstallmentNo))
        {
            if (paymentRemaining <= 0)
                break;

            var reversibleAmount = Math.Min(schedule.PaidAmount, paymentRemaining);
            if (reversibleAmount <= 0)
                continue;

            await connection.ExecuteAsync(
                """
                UPDATE coop.loan_installment_schedules
                SET paid_amount = paid_amount - @ReversibleAmount
                WHERE loan_installment_schedule_id = @LoanInstallmentScheduleId
                """,
                new
                {
                    ReversibleAmount = reversibleAmount,
                    schedule.LoanInstallmentScheduleId
                },
                transaction);

            paymentRemaining -= reversibleAmount;
        }

        if (paymentRemaining > 0)
            throw new InvalidOperationException("Loan payment cancellation could not reconcile installment balances.");

        await connection.ExecuteAsync(
            """
            UPDATE coop.loan_installment_schedules
            SET installment_status = CASE
                    WHEN paid_amount >= installment_amount THEN 'paid'
                    WHEN due_date < @AsOfDate THEN 'overdue'
                    WHEN paid_amount > 0 THEN 'partial'
                    ELSE 'unpaid'
                END,
                settled_at = CASE
                    WHEN paid_amount >= installment_amount THEN settled_at
                    ELSE NULL
                END
            WHERE loan_id = @LoanId
            """,
            new
            {
                LoanId = payment.LoanId,
                AsOfDate = DateOnly.FromDateTime(DateTime.UtcNow)
            },
            transaction);

        await connection.ExecuteAsync(
            """
            UPDATE coop.loans
            SET outstanding_principal_amount = @OutstandingPrincipalAmount,
                outstanding_total_amount = @OutstandingTotalAmount,
                status = @Status,
                updated_at = sysutcdatetime()
            WHERE loan_id = @LoanId
            """,
            new
            {
                LoanId = payment.LoanId,
                OutstandingPrincipalAmount = loan.OutstandingPrincipalAmount + payment.PrincipalPaidAmount,
                OutstandingTotalAmount = loan.OutstandingTotalAmount + payment.PaymentAmount,
                Status = schedules.Any(x => x.DueDate < DateOnly.FromDateTime(DateTime.UtcNow) && x.PaidAmount < x.InstallmentAmount)
                    ? "defaulted"
                    : "active"
            },
            transaction);

        await connection.ExecuteAsync(
            "DELETE FROM coop.loan_payments WHERE loan_payment_id = @LoanPaymentId",
            new { LoanPaymentId = loanPaymentId },
            transaction);

        if (memberTransactionId.HasValue)
        {
            await connection.ExecuteAsync(
                "DELETE FROM coop.member_transactions WHERE member_transaction_id = @MemberTransactionId",
                new { MemberTransactionId = memberTransactionId.Value },
                transaction);
        }

        transaction.Commit();
        return true;
    }

    public async Task<MemberCreditConversionDto> ConvertMemberCreditSaleToLoanAsync(long userId, Sale sale, LoanProduct loanProduct, ConvertMemberCreditSaleRequest request)
    {
        if (!sale.MemberId.HasValue)
            throw new InvalidOperationException("Sale is not linked to a member.");

        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        var lockedSale = await connection.QuerySingleOrDefaultAsync<Sale>(
            """
            SELECT sale_id, tenant_id, sale_no, receipt_no, sale_ts, member_id, cashier_user_id, member_transaction_id, sale_type, sale_status,
                   subtotal_amount, discount_amount, total_amount, paid_amount, change_amount, note, created_at
            FROM coop.sales WITH (UPDLOCK, ROWLOCK)
            WHERE tenant_id = @TenantId
              AND sale_id = @SaleId
            """,
            new { sale.TenantId, sale.SaleId },
            transaction);

        if (lockedSale is null)
            throw new InvalidOperationException("Sale was not found.");

        if (!string.Equals(lockedSale.SaleType, "member_credit", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only member credit sales can be converted.");

        var memberId = lockedSale.MemberId
            ?? throw new InvalidOperationException("Sale is not linked to a member.");

        var alreadyConverted = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM coop.member_credit_conversions WHERE sale_id = @SaleId",
            new { sale.SaleId },
            transaction);

        if (alreadyConverted > 0)
            throw new InvalidOperationException("This sale has already been converted to a loan.");

        var principalAmount = lockedSale.TotalAmount;
        var interestRate = request.FlatInterestRatePct ?? loanProduct.DefaultFlatInterestRatePct;
        var termMonths = request.TermMonths ?? loanProduct.DefaultTermMonths;
        var adminFee = request.AdminFeeAmount ?? loanProduct.DefaultAdminFeeAmount;
        var penalty = request.PenaltyAmount ?? loanProduct.DefaultPenaltyAmount;
        var loanDate = request.LoanDate ?? DateOnly.FromDateTime(lockedSale.SaleTs);

        if (loanProduct.MinTermMonths.HasValue && termMonths < loanProduct.MinTermMonths.Value)
            throw new InvalidOperationException("Term months is below the loan product minimum.");

        if (loanProduct.MaxTermMonths.HasValue && termMonths > loanProduct.MaxTermMonths.Value)
            throw new InvalidOperationException("Term months exceeds the loan product maximum.");

        if (loanProduct.MinFlatInterestRatePct.HasValue && interestRate < loanProduct.MinFlatInterestRatePct.Value)
            throw new InvalidOperationException("Interest rate is below the loan product minimum.");

        if (loanProduct.MaxFlatInterestRatePct.HasValue && interestRate > loanProduct.MaxFlatInterestRatePct.Value)
            throw new InvalidOperationException("Interest rate exceeds the loan product maximum.");

        var totalInterest = Math.Round(principalAmount * (interestRate / 100m) * termMonths, 2);
        var totalPayable = principalAmount + totalInterest + adminFee;
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
                TenantId = lockedSale.TenantId,
                MemberId = memberId,
                LoanProductId = loanProduct.LoanProductId,
                LoanNo = request.LoanNo,
                LoanDate = loanDate,
                PrincipalAmount = principalAmount,
                FlatInterestRatePct = interestRate,
                TermMonths = termMonths,
                AdminFeeAmount = adminFee,
                PenaltyAmount = penalty,
                InstallmentAmount = installment,
                TotalInterestAmount = totalInterest,
                TotalPayableAmount = totalPayable,
                OutstandingPrincipalAmount = principalAmount,
                OutstandingTotalAmount = totalPayable,
                ApprovedByUserId = userId,
                Note = request.Note ?? $"Converted from POS sale {lockedSale.SaleNo}"
            },
            transaction);

        var principalPerInstallment = Math.Round(principalAmount / termMonths, 2);
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

        var conversionId = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO coop.member_credit_conversions (tenant_id, sale_id, loan_id, converted_at, converted_by_user_id, note)
            VALUES (@TenantId, @SaleId, @LoanId, sysutcdatetime(), @ConvertedByUserId, @Note);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """,
            new
            {
                TenantId = lockedSale.TenantId,
                SaleId = lockedSale.SaleId,
                LoanId = loanId,
                ConvertedByUserId = userId,
                Note = request.Note ?? $"Converted sale {lockedSale.SaleNo} into loan {request.LoanNo}"
            },
            transaction);

        await connection.ExecuteAsync(
            """
            INSERT INTO coop.member_transactions (
                tenant_id, member_id, transaction_no, transaction_ts, source_module, source_table, source_id,
                entry_type, amount, description, reference_no, created_by_user_id, created_at
            )
            VALUES (
                @TenantId, @MemberId, @TransactionNo, sysutcdatetime(), 'pos', 'member_credit_conversions', @SourceId,
                'credit', @Amount, @Description, @ReferenceNo, @CreatedByUserId, sysutcdatetime()
            )
            """,
            new
            {
                TenantId = lockedSale.TenantId,
                MemberId = memberId,
                TransactionNo = $"POS-CNV-{lockedSale.SaleNo}",
                SourceId = conversionId,
                Amount = lockedSale.TotalAmount,
                Description = $"Convert POS member credit sale {lockedSale.SaleNo} to loan {request.LoanNo}",
                ReferenceNo = lockedSale.SaleNo,
                CreatedByUserId = userId
            },
            transaction);

        transaction.Commit();

        return new MemberCreditConversionDto(
            conversionId,
            lockedSale.SaleId,
            lockedSale.SaleNo,
            loanId,
            request.LoanNo,
            loanDate,
            principalAmount,
            totalPayable,
            request.Note ?? $"Converted sale {lockedSale.SaleNo} into loan {request.LoanNo}");
    }
}
