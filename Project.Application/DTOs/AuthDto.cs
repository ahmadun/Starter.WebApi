using FluentValidation;
using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record LoginRequest(string TenantCode, string Username, string Password);

public sealed record RegisterMemberRequest(
    string TenantCode,
    string MemberNo,
    string Username,
    string Email,
    string Password,
    string ConfirmPassword);

public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    long UserId,
    long TenantId,
    long? MemberId,
    string Username,
    string Email,
    string DisplayName,
    string UserType,
    IReadOnlyCollection<string> Roles);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record RefreshTokenResponse(string Token, DateTime ExpiresAt);
public sealed record ForgotPasswordRequest(string TenantCode, string UsernameOrEmail);
public sealed record ForgotPasswordResponse(string ResetToken, DateTime ExpiresAt);
public sealed record ConfirmResetPasswordRequest(string ResetToken, string NewPassword, string ConfirmNewPassword);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.TenantCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RegisterMemberRequestValidator : AbstractValidator<RegisterMemberRequest>
{
    public RegisterMemberRequestValidator()
    {
        RuleFor(x => x.TenantCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MemberNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]")
            .Matches(@"[a-z]")
            .Matches(@"[0-9]");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]")
            .Matches(@"[a-z]")
            .Matches(@"[0-9]");
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword);
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.TenantCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.UsernameOrEmail).NotEmpty().MaximumLength(200);
    }
}

public sealed class ConfirmResetPasswordRequestValidator : AbstractValidator<ConfirmResetPasswordRequest>
{
    public ConfirmResetPasswordRequestValidator()
    {
        RuleFor(x => x.ResetToken).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]")
            .Matches(@"[a-z]")
            .Matches(@"[0-9]");
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword);
    }
}

public static class RoleConstants
{
    public const string Admin = "admin";
    public const string Cashier = "cashier";
    public const string Manager = "manager";
    public const string Member = "member";

    public static readonly string[] All = [Admin, Cashier, Manager, Member];
    public static readonly string[] InternalAssignable = [Admin, Cashier, Manager];
}

public sealed record UserDto(
    long UserId,
    long TenantId,
    long? MemberId,
    string Username,
    string Email,
    string DisplayName,
    string UserType,
    bool IsActive,
    DateTime? LastLoginAt,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record UserSummaryDto(
    long UserId,
    string Username,
    string Email,
    string DisplayName,
    string UserType,
    bool IsActive,
    IReadOnlyCollection<string> Roles,
    DateTime? LastLoginAt);

public sealed record UserOptionDto(long UserId, string Username, string DisplayName, string Email);

public sealed class UserFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public string? UserType { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class UserOptionFilterParams
{
    private const int MaxTop = 50;
    private int _top = 20;
    public string? Search { get; set; }
    public int Top
    {
        get => _top;
        set => _top = value < 1 ? 1 : value > MaxTop ? MaxTop : value;
    }
}

public sealed record CreateInternalUserRequest(
    long TenantId,
    string Username,
    string Email,
    string DisplayName,
    string Password,
    string UserType,
    bool IsActive,
    IReadOnlyCollection<string> Roles);

public sealed record UpdateUserRequest(
    string Username,
    string Email,
    string DisplayName,
    bool IsActive,
    IReadOnlyCollection<string> Roles);

public sealed record MyProfileDto(
    long UserId,
    long TenantId,
    long? MemberId,
    string Username,
    string Email,
    string DisplayName,
    string UserType,
    IReadOnlyCollection<string> Roles,
    string? MemberNo,
    string? EmployeeCode);

public sealed record UpdateMyProfileRequest(string Username, string Email, string DisplayName);

public sealed class CreateInternalUserRequestValidator : AbstractValidator<CreateInternalUserRequest>
{
    public CreateInternalUserRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.UserType).Equal("internal");
        RuleFor(x => x.Roles)
            .NotEmpty()
            .Must(roles => roles.All(RoleConstants.InternalAssignable.Contains));
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Roles)
            .NotEmpty()
            .Must(roles => roles.All(RoleConstants.All.Contains));
    }
}

public sealed class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}

public sealed record ResetPasswordRequest(string NewPassword);

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
