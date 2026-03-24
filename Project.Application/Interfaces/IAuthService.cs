using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;


public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse> ResetPasswordAsync(ResetPasswordWithTokenRequest request);

    Task<ApiResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);

    Task<ApiResponse<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);
}

public interface IUserService
{
    Task<ApiResponse<PagedResult<UserSummaryDto>>> GetAllAsync(UserFilterParams filters);
    Task<ApiResponse<IEnumerable<UserOptionDto>>> GetOptionsAsync(UserOptionFilterParams filters);
    Task<ApiResponse<UserDto>> GetByIdAsync(int userId);
    Task<ApiResponse<UserDto>> CreateAsync(CreateUserRequest request);
    Task<ApiResponse<UserDto>> UpdateAsync(int userId, UpdateUserRequest request);
    Task<ApiResponse<MyProfileDto>> GetMyProfileAsync(int userId);
    Task<ApiResponse<MyProfileDto>> UpdateMyProfileAsync(int userId, UpdateMyProfileRequest request);
    Task<ApiResponse> ResetPasswordAsync(int userId, ResetPasswordRequest request);
    Task<ApiResponse> DeleteAsync(int userId);
}
