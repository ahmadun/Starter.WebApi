using BCrypt.Net;
using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Project.Domain.Entities;

namespace Project.Application.Services;

/// <inheritdoc cref="IUserService"/>
public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IValidator<UpdateMyProfileRequest> _updateMyProfileValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepo,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IValidator<UpdateMyProfileRequest> updateMyProfileValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator,
        ILogger<UserService> logger)
    {
        _userRepo = userRepo;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _updateMyProfileValidator = updateMyProfileValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _logger = logger;
    }


    public async Task<ApiResponse<PagedResult<UserSummaryDto>>> GetAllAsync(UserFilterParams filters)
    {
        var (users, totalCount) = await _userRepo.GetAllAsync(filters);
        var items = users.Select(MapToSummaryDto);
        var result = PagedResult<UserSummaryDto>.Create(items, totalCount, filters);

        return ApiResponse<PagedResult<UserSummaryDto>>.Ok(result);
    }

    public async Task<ApiResponse<IEnumerable<UserOptionDto>>> GetOptionsAsync(UserOptionFilterParams filters)
    {
        var users = await _userRepo.GetOptionsAsync(filters);
        var items = users.Select(u => new UserOptionDto(
            u.UserId,
            u.Username,
            u.Email,
            u.Employee?.FullName));

        return ApiResponse<IEnumerable<UserOptionDto>>.Ok(items);
    }



    public async Task<ApiResponse<UserDto>> GetByIdAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse<UserDto>.NotFound($"User with ID {userId} was not found.");

        return ApiResponse<UserDto>.Ok(MapToDto(user));
    }



    public async Task<ApiResponse<UserDto>> CreateAsync(CreateUserRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<UserDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage));

        if (await _userRepo.UsernameExistsAsync(request.Username))
            return ApiResponse<UserDto>.Fail(
                $"Username '{request.Username}' is already in use.");

        if (await _userRepo.EmailExistsAsync(request.Email))
            return ApiResponse<UserDto>.Fail(
                $"Email '{request.Email}' is already registered.");




        var entity = new User
        {
            EmployeeId = request.EmployeeId,
            Username = request.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Email = request.Email.Trim().ToLowerInvariant(),
            Role = request.Role,
            MustChangePassword = request.MustChangePassword,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var newId = await _userRepo.CreateAsync(entity);

        _logger.LogInformation("User created: {Username} (ID {Id})", entity.Username, newId);

        var created = await _userRepo.GetByIdAsync(newId);
        return ApiResponse<UserDto>.Created(MapToDto(created!));
    }



    public async Task<ApiResponse<UserDto>> UpdateAsync(int userId, UpdateUserRequest request)
    {

        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<UserDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage));


        var existing = await _userRepo.GetByIdAsync(userId);
        if (existing is null)
            return ApiResponse<UserDto>.NotFound($"User with ID {userId} was not found.");

        if (await _userRepo.UsernameExistsAsync(request.Username, userId))
            return ApiResponse<UserDto>.Fail(
                $"Username '{request.Username}' is already in use.");

        if (await _userRepo.EmailExistsAsync(request.Email, userId))
            return ApiResponse<UserDto>.Fail(
                $"Email '{request.Email}' is already registered.");




        existing.EmployeeId = request.EmployeeId;
        existing.Username = request.Username.Trim();
        existing.Email = request.Email.Trim().ToLowerInvariant();
        existing.Role = request.Role;
        existing.IsActive = request.IsActive;
        existing.MustChangePassword = request.MustChangePassword;
        existing.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(existing);

        _logger.LogInformation("User updated: {Username} (ID {Id})", existing.Username, userId);

        var updated = await _userRepo.GetByIdAsync(userId);
        return ApiResponse<UserDto>.Ok(MapToDto(updated!), "Updated successfully.");
    }

    public async Task<ApiResponse<MyProfileDto>> GetMyProfileAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse<MyProfileDto>.NotFound($"User with ID {userId} was not found.");

        return ApiResponse<MyProfileDto>.Ok(MapToMyProfileDto(user));
    }

    public async Task<ApiResponse<MyProfileDto>> UpdateMyProfileAsync(int userId, UpdateMyProfileRequest request)
    {
        var validation = await _updateMyProfileValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<MyProfileDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage));

        var existing = await _userRepo.GetByIdAsync(userId);
        if (existing is null)
            return ApiResponse<MyProfileDto>.NotFound($"User with ID {userId} was not found.");

        if (await _userRepo.UsernameExistsAsync(request.Username, userId))
            return ApiResponse<MyProfileDto>.Fail(
                $"Username '{request.Username}' is already in use.");

        if (await _userRepo.EmailExistsAsync(request.Email, userId))
            return ApiResponse<MyProfileDto>.Fail(
                $"Email '{request.Email}' is already registered.");

        existing.Username = request.Username.Trim();
        existing.Email = request.Email.Trim().ToLowerInvariant();
        existing.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(existing);

        _logger.LogInformation("Profile updated by user ID {UserId}", userId);

        var updated = await _userRepo.GetByIdAsync(userId);
        return ApiResponse<MyProfileDto>.Ok(MapToMyProfileDto(updated!), "Profile updated successfully.");
    }

    public async Task<ApiResponse> ResetPasswordAsync(int userId, ResetPasswordRequest request)
    {

        var validation = await _resetPasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage));


        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse.Fail($"User with ID {userId} was not found.");

  
        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepo.UpdatePasswordAsync(userId, newHash);
        await _userRepo.ResetFailedLoginAttemptsAsync(userId);

  
        if (request.MustChangePassword)
        {
            user.MustChangePassword = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);
        }

        _logger.LogInformation("Password reset for user ID {UserId} by admin", userId);

        return ApiResponse.Ok("Password reset successfully.");
    }



    public async Task<ApiResponse> DeleteAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse.Fail($"User with ID {userId} was not found.");

        await _userRepo.SoftDeleteAsync(userId);

        _logger.LogInformation("User soft-deleted: ID {UserId}", userId);

        return ApiResponse.Ok("User deleted successfully.");
    }



    private static UserDto MapToDto(User u) => new(
       u.UserId,
       u.EmployeeId,
       u.Employee?.FullName,
       u.Username,
       u.Email,
       u.Role,
       u.IsActive,
       u.LastLogin,
       u.FailedLoginAttempts,
       u.LockoutUntil,
       u.MustChangePassword,
       u.CreatedAt,
       u.UpdatedAt);

    private static UserSummaryDto MapToSummaryDto(User u) => new(
        u.UserId,
        u.Username,
        u.Email,
        u.Role,
        u.IsActive,
        u.LastLogin);

    private static MyProfileDto MapToMyProfileDto(User u) => new(
        u.UserId,
        u.EmployeeId,
        u.Employee?.FullName,
        u.Username,
        u.Email,
        u.Role,
        u.MustChangePassword,
        u.CreatedAt,
        u.UpdatedAt);
}
