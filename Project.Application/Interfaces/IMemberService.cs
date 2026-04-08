using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface IMemberService
{
    Task<ApiResponse<PagedResult<MemberDto>>> GetAllAsync(long tenantId, MemberFilterParams filters);
    Task<ApiResponse<MemberDto>> GetByIdAsync(long tenantId, long memberId);
    Task<ApiResponse<IEnumerable<MemberLookupDto>>> LookupAsync(long tenantId, string query, int limit = 10);
    Task<ApiResponse<MemberDto>> CreateAsync(CreateMemberRequest request);
    Task<ApiResponse<MemberDto>> UpdateAsync(long tenantId, long memberId, UpdateMemberRequest request);
}
