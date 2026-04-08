using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface IUserService
{
    Task<ApiResponse<PagedResult<UserSummaryDto>>> GetAllAsync(long tenantId, UserFilterParams filters);
    Task<ApiResponse<IEnumerable<UserOptionDto>>> GetOptionsAsync(long tenantId, UserOptionFilterParams filters);
    Task<ApiResponse<UserDto>> GetByIdAsync(long tenantId, long userId);
    Task<ApiResponse<UserDto>> CreateAsync(CreateInternalUserRequest request);
    Task<ApiResponse<UserDto>> UpdateAsync(long tenantId, long userId, UpdateUserRequest request);
    Task<ApiResponse<MyProfileDto>> GetMyProfileAsync(long userId);
    Task<ApiResponse<MyProfileDto>> UpdateMyProfileAsync(long userId, UpdateMyProfileRequest request);
    Task<ApiResponse> ResetPasswordAsync(long tenantId, long userId, ResetPasswordRequest request);
    Task<ApiResponse> DeleteAsync(long tenantId, long userId);
}
