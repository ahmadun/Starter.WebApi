using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;

public interface ILoanRepository
{
    Task<(IEnumerable<Loan> Items, int TotalCount)> GetAllAsync(long tenantId, LoanFilterParams filters);
    Task<Loan?> GetByIdAsync(long tenantId, long loanId);
    Task<IReadOnlyCollection<LoanInstallmentScheduleDto>> GetInstallmentsAsync(long loanId);
    Task<(IEnumerable<LoanPaymentDto> Items, int TotalCount)> GetPaymentsAsync(long tenantId, LoanPaymentFilterParams filters);
    Task<LoanPaymentDto?> GetPaymentByIdAsync(long tenantId, long loanPaymentId);
    Task<bool> LoanNoExistsAsync(long tenantId, string loanNo);
    Task<bool> PaymentNoExistsAsync(long tenantId, string paymentNo);
    Task<long> CreateAsync(long userId, CreateLoanRequest request, LoanProduct loanProduct);
    Task<long> CreatePaymentAsync(long userId, long tenantId, long loanId, CreateLoanPaymentRequest request);
    Task<bool> DeleteAsync(long tenantId, long loanId);
    Task<bool> DeletePaymentAsync(long tenantId, long loanPaymentId);
    Task<MemberCreditConversionDto> ConvertMemberCreditSaleToLoanAsync(long userId, Sale sale, LoanProduct loanProduct, ConvertMemberCreditSaleRequest request);
}
