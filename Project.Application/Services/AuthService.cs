using System.Net;
using System.Net.Mail;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;

namespace Project.Application.Services;

/// <inheritdoc cref="IAuthService"/>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenService _jwtService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<ChangePasswordRequest> _passwordValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordWithTokenRequest> _resetPasswordValidator;
    private readonly ApprovalNotificationSettings _notificationSettings;
    private readonly ILogger<AuthService> _logger;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 30;
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(30);

    public AuthService(
        IUserRepository userRepo,
        IJwtTokenService jwtService,
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterRequest> registerValidator,
        IValidator<ChangePasswordRequest> passwordValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordWithTokenRequest> resetPasswordValidator,
        IOptions<ApprovalNotificationSettings> notificationSettings,
        ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _jwtService = jwtService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _passwordValidator = passwordValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _notificationSettings = notificationSettings.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<LoginResponse>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage));

        var user = await _userRepo.GetByUsernameAsync(request.Username);
        if (user is null)
        {
            _logger.LogWarning("Login failed: Username '{Username}' not found", request.Username);
            return ApiResponse<LoginResponse>.Fail("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User '{Username}' is inactive", request.Username);
            return ApiResponse<LoginResponse>.Fail("Account is inactive. Contact administrator.");
        }

        if (user.IsLockedOut)
        {
            var remainingMinutes = (int)(user.LockoutUntil!.Value - DateTime.UtcNow).TotalMinutes;
            _logger.LogWarning("Login failed: User '{Username}' is locked out", request.Username);
            return ApiResponse<LoginResponse>.Fail(
                $"Account is temporarily locked. Try again in {remainingMinutes} minutes.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await _userRepo.IncrementFailedLoginAttemptsAsync(user.UserId);

            if (user.FailedLoginAttempts + 1 >= MaxFailedAttempts)
            {
                await _userRepo.LockAccountAsync(
                    user.UserId,
                    DateTime.UtcNow.AddMinutes(LockoutMinutes));

                _logger.LogWarning(
                    "User '{Username}' locked after {Attempts} failed login attempts",
                    request.Username, MaxFailedAttempts);

                return ApiResponse<LoginResponse>.Fail(
                    $"Account locked due to {MaxFailedAttempts} failed login attempts. " +
                    $"Try again in {LockoutMinutes} minutes.");
            }

            _logger.LogWarning("Login failed: Invalid password for '{Username}'", request.Username);
            return ApiResponse<LoginResponse>.Fail("Invalid username or password.");
        }

        await _userRepo.ResetFailedLoginAttemptsAsync(user.UserId);
        await _userRepo.UpdateLastLoginAsync(user.UserId);

        return await BuildLoginResponseAsync(user, request.Username);
    }

    public async Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<LoginResponse>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage));

        if (await _userRepo.UsernameExistsAsync(request.Username))
            return ApiResponse<LoginResponse>.Fail($"Username '{request.Username}' is already in use.");

        if (await _userRepo.EmailExistsAsync(request.Email))
            return ApiResponse<LoginResponse>.Fail($"Email '{request.Email}' is already registered.");

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Employee",
            IsActive = true,
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow
        };

        user.UserId = await _userRepo.CreateAsync(user);

        _logger.LogInformation("Self-service registration created user '{Username}' with ID {UserId}", user.Username, user.UserId);

        return await BuildLoginResponseAsync(user, user.Username);
    }

    public async Task<ApiResponse<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var validation = await _forgotPasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<ForgotPasswordResponse>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage));

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepo.GetByEmailAsync(normalizedEmail);

        if (user is null || !user.IsActive)
        {
            _logger.LogInformation("Forgot password requested for unavailable email {Email}", normalizedEmail);
            return ApiResponse<ForgotPasswordResponse>.Ok(
                new ForgotPasswordResponse(null),
                "If an account exists for that email, password reset instructions have been sent.");
        }

        var tokenValue = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.Add(PasswordResetTokenLifetime);

        await _userRepo.InvalidatePasswordResetTokensAsync(user.UserId);
        await _userRepo.CreatePasswordResetTokenAsync(new PasswordResetToken
        {
            UserId = user.UserId,
            Token = tokenValue,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        });

        var resetUrl = BuildResetUrl(tokenValue, user.Email);

        try
        {
            await SendPasswordResetEmailAsync(user, resetUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset email to user ID {UserId}", user.UserId);
        }

        return ApiResponse<ForgotPasswordResponse>.Ok(
            new ForgotPasswordResponse(resetUrl),
            "If an account exists for that email, password reset instructions have been sent.");
    }

    public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordWithTokenRequest request)
    {
        var validation = await _resetPasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage));

        var resetToken = await _userRepo.GetValidPasswordResetTokenAsync(request.Token);
        if (resetToken is null)
            return ApiResponse.Fail("Reset token is invalid or expired.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepo.GetByIdAsync(resetToken.UserId);
        if (user is null || !user.IsActive || !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            return ApiResponse.Fail("Reset token is invalid or expired.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepo.UpdatePasswordAsync(user.UserId, newHash);
        await _userRepo.ResetFailedLoginAttemptsAsync(user.UserId);
        await _userRepo.ConsumePasswordResetTokenAsync(resetToken.Id, DateTime.UtcNow);
        await _userRepo.InvalidatePasswordResetTokensAsync(user.UserId);

        _logger.LogInformation("Password reset completed for user ID {UserId}", user.UserId);

        return ApiResponse.Ok("Password reset successfully.");
    }

    public async Task<ApiResponse> ChangePasswordAsync(
        int userId, ChangePasswordRequest request)
    {
        var validation = await _passwordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage));

        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse.Fail("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return ApiResponse.Fail("Current password is incorrect.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepo.UpdatePasswordAsync(userId, newHash);

        _logger.LogInformation("Password changed for user ID {UserId}", userId);

        return ApiResponse.Ok("Password changed successfully.");
    }

    public async Task<ApiResponse<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var user = await _userRepo.GetByRefreshTokenAsync(request.RefreshToken);
        if (user is null)
        {
            _logger.LogWarning("Refresh token invalid or expired");
            return ApiResponse<RefreshTokenResponse>.Fail("Invalid or expired refresh token.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Refresh token used by inactive user ID {UserId}", user.UserId);
            return ApiResponse<RefreshTokenResponse>.Fail("User account is inactive.");
        }

        var token = _jwtService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        _logger.LogInformation("User ID {UserId} refreshed token", user.UserId);

        return ApiResponse<RefreshTokenResponse>.Ok(new RefreshTokenResponse(token, expiresAt));
    }

    private async Task<ApiResponse<LoginResponse>> BuildLoginResponseAsync(User user, string usernameForLog)
    {
        var token = _jwtService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await _userRepo.StoreRefreshTokenAsync(user.UserId, refreshToken, refreshTokenExpiresAt);

        _logger.LogInformation("User '{Username}' authenticated successfully", usernameForLog);

        return ApiResponse<LoginResponse>.Ok(new LoginResponse(
            token,
            expiresAt,
            refreshToken,
            refreshTokenExpiresAt,
            user.UserId,
            user.Username,
            user.Email,
            user.Role,
            user.MustChangePassword));
    }

    private string BuildResetUrl(string token, string email)
    {
        var baseUrl = _notificationSettings.FrontendBaseUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return $"/session/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
        }

        return $"{baseUrl}/session/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
    }

    private async Task SendPasswordResetEmailAsync(User user, string resetUrl)
    {
        if (!_notificationSettings.Enabled ||
            string.IsNullOrWhiteSpace(_notificationSettings.SmtpHost) ||
            string.IsNullOrWhiteSpace(_notificationSettings.SenderEmail))
        {
            return;
        }

        using var client = new SmtpClient(_notificationSettings.SmtpHost, _notificationSettings.SmtpPort)
        {
            EnableSsl = _notificationSettings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = string.IsNullOrWhiteSpace(_notificationSettings.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_notificationSettings.Username, _notificationSettings.Password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_notificationSettings.SenderEmail, _notificationSettings.SenderName),
            Subject = "Reset your password",
            Body =
                $"Hello {user.Username},\n\n" +
                "We received a request to reset your password.\n" +
                $"Use the link below to set a new password:\n{resetUrl}\n\n" +
                "If you did not request this, you can ignore this email.",
            IsBodyHtml = false
        };

        message.To.Add(new MailAddress(user.Email, user.Username));
        await client.SendMailAsync(message);
    }
}
