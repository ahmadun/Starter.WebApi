using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface ISaleService
{
    Task<ApiResponse<PagedResult<SaleDto>>> GetAllAsync(long tenantId, SaleFilterParams filters);
    Task<ApiResponse<SaleDto>> GetByIdAsync(long tenantId, long saleId);
    Task<ApiResponse<SaleReceiptDto>> GetReceiptAsync(long tenantId, long saleId);
    Task<ApiResponse<SaleDto>> CreateAsync(long userId, CreateSaleRequest request);
    Task<ApiResponse<MemberCreditConversionDto>> ConvertMemberCreditToLoanAsync(long userId, long tenantId, long saleId, ConvertMemberCreditSaleRequest request);
}
