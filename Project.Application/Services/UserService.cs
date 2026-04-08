using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;

namespace Project.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<CreateInternalUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IValidator<UpdateMyProfileRequest> _profileValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;

    public UserService(
        IUserRepository userRepository,
        IValidator<CreateInternalUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IValidator<UpdateMyProfileRequest> profileValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator)
    {
        _userRepository = userRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _profileValidator = profileValidator;
        _resetPasswordValidator = resetPasswordValidator;
    }

    public async Task<ApiResponse<PagedResult<UserSummaryDto>>> GetAllAsync(long tenantId, UserFilterParams filters)
    {
        var (items, totalCount) = await _userRepository.GetAllAsync(tenantId, filters);
        return ApiResponse<PagedResult<UserSummaryDto>>.Ok(
            PagedResult<UserSummaryDto>.Create(items.Select(MapSummary), totalCount, filters));
    }

    public async Task<ApiResponse<IEnumerable<UserOptionDto>>> GetOptionsAsync(long tenantId, UserOptionFilterParams filters)
    {
        var items = await _userRepository.GetOptionsAsync(tenantId, filters);
        return ApiResponse<IEnumerable<UserOptionDto>>.Ok(items.Select(u => new UserOptionDto(u.UserId, u.Username, u.DisplayName, u.Email)));
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(long tenantId, long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user is null || user.TenantId != tenantId
            ? ApiResponse<UserDto>.NotFound("User not found.")
            : ApiResponse<UserDto>.Ok(MapDto(user));
    }

    public async Task<ApiResponse<UserDto>> CreateAsync(CreateInternalUserRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<UserDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _userRepository.UsernameExistsAsync(request.TenantId, request.Username.Trim()))
            return ApiResponse<UserDto>.Fail($"Username '{request.Username}' is already in use.");

        if (await _userRepository.EmailExistsAsync(request.TenantId, request.Email.Trim().ToLowerInvariant()))
            return ApiResponse<UserDto>.Fail($"Email '{request.Email}' is already registered.");

        var user = new User
        {
            TenantId = request.TenantId,
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName.Trim(),
            UserType = request.UserType,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        user.UserId = await _userRepository.CreateAsync(user, request.Roles);
        var created = await _userRepository.GetByIdAsync(user.UserId) ?? user;
        return ApiResponse<UserDto>.Created(MapDto(created));
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(long tenantId, long userId, UpdateUserRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<UserDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null || user.TenantId != tenantId)
            return ApiResponse<UserDto>.NotFound("User not found.");

        if (await _userRepository.UsernameExistsAsync(tenantId, request.Username.Trim(), userId))
            return ApiResponse<UserDto>.Fail($"Username '{request.Username}' is already in use.");

        if (await _userRepository.EmailExistsAsync(tenantId, request.Email.Trim().ToLowerInvariant(), userId))
            return ApiResponse<UserDto>.Fail($"Email '{request.Email}' is already registered.");

        user.Username = request.Username.Trim();
        user.Email = request.Email.Trim().ToLowerInvariant();
        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, request.Roles);
        var updated = await _userRepository.GetByIdAsync(userId) ?? user;
        return ApiResponse<UserDto>.Ok(MapDto(updated), "Updated successfully.");
    }

    public async Task<ApiResponse<MyProfileDto>> GetMyProfileAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user is null
            ? ApiResponse<MyProfileDto>.NotFound("User not found.")
            : ApiResponse<MyProfileDto>.Ok(MapProfile(user));
    }

    public async Task<ApiResponse<MyProfileDto>> UpdateMyProfileAsync(long userId, UpdateMyProfileRequest request)
    {
        var validation = await _profileValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<MyProfileDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse<MyProfileDto>.NotFound("User not found.");

        if (await _userRepository.UsernameExistsAsync(user.TenantId, request.Username.Trim(), userId))
            return ApiResponse<MyProfileDto>.Fail($"Username '{request.Username}' is already in use.");

        if (await _userRepository.EmailExistsAsync(user.TenantId, request.Email.Trim().ToLowerInvariant(), userId))
            return ApiResponse<MyProfileDto>.Fail($"Email '{request.Email}' is already registered.");

        user.Username = request.Username.Trim();
        user.Email = request.Email.Trim().ToLowerInvariant();
        user.DisplayName = request.DisplayName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, user.Roles);
        var updated = await _userRepository.GetByIdAsync(userId) ?? user;
        return ApiResponse<MyProfileDto>.Ok(MapProfile(updated), "Profile updated successfully.");
    }

    public async Task<ApiResponse> ResetPasswordAsync(long tenantId, long userId, ResetPasswordRequest request)
    {
        var validation = await _resetPasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null || user.TenantId != tenantId)
            return ApiResponse.NotFound("User not found.");

        await _userRepository.UpdatePasswordAsync(userId, BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
        await _userRepository.ResetFailedLoginAttemptsAsync(userId);
        return ApiResponse.Ok("Password reset successfully.");
    }

    public async Task<ApiResponse> DeleteAsync(long tenantId, long userId)
    {
        var deleted = await _userRepository.SoftDeleteAsync(tenantId, userId);
        return deleted ? ApiResponse.Ok("User deleted successfully.") : ApiResponse.NotFound("User not found.");
    }

    private static UserDto MapDto(User user) => new(
        user.UserId, user.TenantId, user.MemberId, user.Username, user.Email, user.DisplayName, user.UserType,
        user.IsActive, user.LastLoginAt, user.Roles, user.CreatedAt, user.UpdatedAt);

    private static UserSummaryDto MapSummary(User user) => new(
        user.UserId, user.Username, user.Email, user.DisplayName, user.UserType, user.IsActive, user.Roles, user.LastLoginAt);

    private static MyProfileDto MapProfile(User user) => new(
        user.UserId, user.TenantId, user.MemberId, user.Username, user.Email, user.DisplayName, user.UserType,
        user.Roles, user.Member?.MemberNo, user.Member?.EmployeeCode);
}
