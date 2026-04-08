using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;

public interface ISaleRepository
{
    Task<(IEnumerable<Sale> Items, int TotalCount)> GetAllAsync(long tenantId, SaleFilterParams filters);
    Task<Sale?> GetByIdAsync(long tenantId, long saleId);
    Task<SaleReceiptDto?> GetReceiptAsync(long tenantId, long saleId);
    Task<bool> SaleNoExistsAsync(long tenantId, string saleNo);
    Task<bool> HasMemberCreditConversionAsync(long tenantId, long saleId);
    Task<long> CreateAsync(long userId, CreateSaleRequest request);
}
