using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;

public interface ILoanProductRepository
{
    Task<(IEnumerable<LoanProduct> Items, int TotalCount)> GetAllAsync(long tenantId, LoanProductFilterParams filters);
    Task<LoanProduct?> GetByIdAsync(long tenantId, long loanProductId);
    Task<bool> ProductCodeExistsAsync(long tenantId, string productCode, long? excludeId = null);
    Task<long> CreateAsync(LoanProduct loanProduct);
    Task<bool> UpdateAsync(LoanProduct loanProduct);
}
