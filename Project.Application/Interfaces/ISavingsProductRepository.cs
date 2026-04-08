using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;

public interface ISavingsProductRepository
{
    Task<(IEnumerable<SavingsProduct> Items, int TotalCount)> GetAllAsync(long tenantId, SavingsProductFilterParams filters);
    Task<SavingsProduct?> GetByIdAsync(long tenantId, long savingsProductId);
    Task<bool> ProductCodeExistsAsync(long tenantId, string productCode, long? excludeId = null);
    Task<long> CreateAsync(SavingsProduct savingsProduct);
    Task<bool> UpdateAsync(SavingsProduct savingsProduct);
}
