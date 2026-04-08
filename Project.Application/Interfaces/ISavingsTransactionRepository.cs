using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface ISavingsTransactionRepository
{
    Task<(IEnumerable<SavingsAccountDto> Items, int TotalCount)> GetAccountsAsync(long tenantId, SavingsAccountFilterParams filters);
    Task<(IEnumerable<SavingsTransactionDto> Items, int TotalCount)> GetTransactionsAsync(long tenantId, SavingsTransactionFilterParams filters);
    Task<SavingsTransactionDto?> GetByIdAsync(long tenantId, long savingsTransactionId);
    Task<SavingsTransactionContextDto?> GetContextAsync(long tenantId, long memberId, long savingsProductId);
    Task<bool> TransactionNoExistsAsync(long tenantId, string transactionNo);
    Task<decimal> GetBalanceAsync(long memberSavingsAccountId);
    Task<bool> HasAnyDepositAsync(long memberSavingsAccountId);
    Task<long> CreateAsync(long userId, CreateSavingsTransactionRequest request, string entryType);
    Task<bool> DeleteAsync(long tenantId, long savingsTransactionId);
}
