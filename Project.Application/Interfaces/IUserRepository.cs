using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;

public interface IUserRepository
{
    Task<Tenant?> GetTenantByCodeAsync(string tenantCode);
    Task<User?> GetByTenantAndUsernameAsync(string tenantCode, string username);
    Task<User?> GetByIdAsync(long userId);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetMemberRegistrationCandidateAsync(string tenantCode, string memberNo);
    Task<(IEnumerable<User> Items, int TotalCount)> GetAllAsync(long tenantId, UserFilterParams filters);
    Task<IEnumerable<User>> GetOptionsAsync(long tenantId, UserOptionFilterParams filters);
    Task<bool> UsernameExistsAsync(long tenantId, string username, long? excludeUserId = null);
    Task<bool> EmailExistsAsync(long tenantId, string email, long? excludeUserId = null);
    Task<long> CreateAsync(User user, IReadOnlyCollection<string> roles);
    Task<bool> UpdateAsync(User user, IReadOnlyCollection<string> roles);
    Task<bool> UpdatePasswordAsync(long userId, string newPasswordHash);
    Task<bool> SoftDeleteAsync(long tenantId, long userId);
    Task<User?> GetByTenantAndUsernameOrEmailAsync(string tenantCode, string usernameOrEmail);
    Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token);
    Task<long> CreatePasswordResetTokenAsync(long userId, string token, DateTime expiresAt);
    Task InvalidatePasswordResetTokensAsync(long userId);
    Task ConsumePasswordResetTokenAsync(long passwordResetTokenId, DateTime consumedAt);
    Task UpdateLastLoginAsync(long userId);
    Task IncrementFailedLoginAttemptsAsync(long userId);
    Task ResetFailedLoginAttemptsAsync(long userId);
    Task LockAccountAsync(long userId, DateTime lockoutUntil);
    Task StoreRefreshTokenAsync(long userId, string refreshToken, DateTime expiresAt);
}
