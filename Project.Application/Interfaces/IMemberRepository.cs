using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;

public interface IMemberRepository
{
    Task<(IEnumerable<Member> Items, int TotalCount)> GetAllAsync(long tenantId, MemberFilterParams filters);
    Task<Member?> GetByIdAsync(long tenantId, long memberId);
    Task<IEnumerable<Member>> LookupAsync(long tenantId, string query, int limit);
    Task<bool> MemberNoExistsAsync(long tenantId, string memberNo, long? excludeMemberId = null);
    Task<bool> EmployeeCodeExistsAsync(long tenantId, string? employeeCode, long? excludeMemberId = null);
    Task<long> CreateAsync(Member member);
    Task<bool> UpdateAsync(Member member);
}
