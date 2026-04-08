using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Application.Services;

public sealed class SavingsTransactionService : ISavingsTransactionService
{
    private readonly ISavingsTransactionRepository _repository;
    private readonly IValidator<CreateSavingsTransactionRequest> _validator;

    public SavingsTransactionService(
        ISavingsTransactionRepository repository,
        IValidator<CreateSavingsTransactionRequest> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<ApiResponse<PagedResult<SavingsAccountDto>>> GetAccountsAsync(long tenantId, SavingsAccountFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetAccountsAsync(tenantId, filters);
        return ApiResponse<PagedResult<SavingsAccountDto>>.Ok(PagedResult<SavingsAccountDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<PagedResult<SavingsTransactionDto>>> GetTransactionsAsync(long tenantId, SavingsTransactionFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetTransactionsAsync(tenantId, filters);
        return ApiResponse<PagedResult<SavingsTransactionDto>>.Ok(PagedResult<SavingsTransactionDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<SavingsTransactionDto>> GetByIdAsync(long tenantId, long savingsTransactionId)
    {
        var item = await _repository.GetByIdAsync(tenantId, savingsTransactionId);
        return item is null
            ? ApiResponse<SavingsTransactionDto>.NotFound("Savings transaction not found.")
            : ApiResponse<SavingsTransactionDto>.Ok(item);
    }

    public async Task<ApiResponse<SavingsTransactionDto>> CreateAsync(long userId, CreateSavingsTransactionRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<SavingsTransactionDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.TransactionNoExistsAsync(request.TenantId, request.TransactionNo.Trim()))
            return ApiResponse<SavingsTransactionDto>.Fail($"Savings transaction number '{request.TransactionNo}' is already in use.");

        var context = await _repository.GetContextAsync(request.TenantId, request.MemberId, request.SavingsProductId);
        if (context is null)
            return ApiResponse<SavingsTransactionDto>.Fail("Member or savings product was not found.");

        if (!context.ProductIsActive)
            return ApiResponse<SavingsTransactionDto>.Fail("Savings product is inactive.");

        if (!string.Equals(context.MemberStatus, "active", StringComparison.OrdinalIgnoreCase))
            return ApiResponse<SavingsTransactionDto>.Fail("Member is not active.");

        if (context.Periodicity == "monthly" && request.TransactionType == "deposit" && (!request.PeriodYear.HasValue || !request.PeriodMonth.HasValue))
            return ApiResponse<SavingsTransactionDto>.Fail("Period year and month are required for monthly savings deposits.");

        if (context.SavingsKind == "pokok" && request.TransactionType == "deposit" && context.MemberSavingsAccountId.HasValue)
        {
            var hasAnyDeposit = await _repository.HasAnyDepositAsync(context.MemberSavingsAccountId.Value);
            if (hasAnyDeposit)
                return ApiResponse<SavingsTransactionDto>.Fail("Simpanan pokok can only be deposited once.");
        }

        var entryType = ResolveEntryType(request);
        if ((request.TransactionType == "withdrawal" || entryType == "debit") && context.MemberSavingsAccountId.HasValue)
        {
            var balance = await _repository.GetBalanceAsync(context.MemberSavingsAccountId.Value);
            if (balance < request.Amount)
                return ApiResponse<SavingsTransactionDto>.Fail("Insufficient savings balance for this transaction.");
        }

        var transactionId = await _repository.CreateAsync(userId, request with { TransactionNo = request.TransactionNo.Trim() }, entryType);
        var created = await _repository.GetByIdAsync(request.TenantId, transactionId);
        return created is null
            ? ApiResponse<SavingsTransactionDto>.Fail("Savings transaction was created but could not be reloaded.")
            : ApiResponse<SavingsTransactionDto>.Created(created);
    }

    public async Task<ApiResponse<object>> DeleteAsync(long tenantId, long savingsTransactionId)
    {
        var existing = await _repository.GetByIdAsync(tenantId, savingsTransactionId);
        if (existing is null)
            return ApiResponse<object>.NotFound("Savings transaction not found.");

        var deleted = await _repository.DeleteAsync(tenantId, savingsTransactionId);
        return deleted
            ? ApiResponse<object>.Ok(null, "Savings transaction deleted successfully.")
            : ApiResponse<object>.Fail("Failed to delete savings transaction.");
    }

    private static string ResolveEntryType(CreateSavingsTransactionRequest request)
        => request.TransactionType switch
        {
            "deposit" => "credit",
            "withdrawal" => "debit",
            _ => request.EntryType!.Trim()
        };
}
