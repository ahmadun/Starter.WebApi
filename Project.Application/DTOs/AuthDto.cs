using FluentValidation;

using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record LoginRequest(
    string Username,
    string Password
);

public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string ConfirmPassword
);


public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    int UserId,
    string Username,
    string Email,
    string Role,
    bool MustChangePassword
);


public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword
);

public sealed record ForgotPasswordRequest(
    string Email
);

public sealed record ForgotPasswordResponse(
    string? ResetUrl
);

public sealed record ResetPasswordWithTokenRequest(
    string Token,
    string Email,
    string NewPassword,
    string ConfirmNewPassword
);


public sealed record RefreshTokenRequest(
    string RefreshToken
);


public sealed record RefreshTokenResponse(
    string Token,
    DateTime ExpiresAt
);


public sealed record UserDto(
    int UserId,
    int? EmployeeId,
    string? EmployeeName,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    DateTime? LastLogin,
    int FailedLoginAttempts,
    DateTime? LockoutUntil,
    bool MustChangePassword,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);


public sealed record UserSummaryDto(
    int UserId,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    DateTime? LastLogin
);

public sealed class UserFilterParams : PaginationParams
{
    public string? Name { get; set; }
}

public sealed record UserOptionDto(
    int UserId,
    string Username,
    string Email,
    string? EmployeeName
);

public sealed class UserOptionFilterParams
{
    private const int MaxTop = 50;
    private int _top = 20;

    public string? Name { get; set; }

    public int Top
    {
        get => _top;
        set => _top = value > MaxTop ? MaxTop
                     : value < 1 ? 1
                     : value;
    }
}

public sealed record CreateUserRequest(
    int? EmployeeId,
    string Username,
    string Email,
    string Password,
    string Role,
    bool MustChangePassword
);

public sealed record UpdateUserRequest(
    int? EmployeeId,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    bool MustChangePassword
);

public sealed record ResetPasswordRequest(
    string NewPassword,
    bool MustChangePassword
);

public sealed record MyProfileDto(
    int UserId,
    int? EmployeeId,
    string? FullName,
    string Username,
    string Email,
    string Role,
    bool MustChangePassword,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record UpdateMyProfileRequest(
    string Username,
    string Email
);



public static class UserConstants
{
    public static readonly string[] Roles =
        ["SuperAdmin", "Admin", "Manager", "Employee"];
}



public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");
    }
}

public sealed class ResetPasswordByTokenRequestValidator : AbstractValidator<ResetPasswordWithTokenRequest>
{
    public ResetPasswordByTokenRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => UserConstants.Roles.Contains(r))
            .WithMessage($"Role must be one of: {string.Join(", ", UserConstants.Roles)}.");

        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).When(x => x.EmployeeId.HasValue)
            .WithMessage("EmployeeId must be a positive integer.");
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => UserConstants.Roles.Contains(r))
            .WithMessage($"Role must be one of: {string.Join(", ", UserConstants.Roles)}.");

        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).When(x => x.EmployeeId.HasValue)
            .WithMessage("EmployeeId must be a positive integer.");
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");
    }
}

public sealed class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");
    }
}
