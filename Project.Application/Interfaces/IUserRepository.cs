using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;


public interface IUserRepository
{

    Task<User?> GetByUsernameAsync(string username);

    Task<User?> GetByEmailAsync(string email);

    Task UpdateLastLoginAsync(int userId);
    Task IncrementFailedLoginAttemptsAsync(int userId);

    Task ResetFailedLoginAttemptsAsync(int userId);

    Task LockAccountAsync(int userId, DateTime lockoutUntil);

    Task StoreRefreshTokenAsync(int userId, string refreshToken, DateTime expiresAt);

    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task CreatePasswordResetTokenAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetValidPasswordResetTokenAsync(string token);
    Task InvalidatePasswordResetTokensAsync(int userId);
    Task ConsumePasswordResetTokenAsync(long id, DateTime consumedAt);


    Task<(IEnumerable<User> Items, int TotalCount)> GetAllAsync(UserFilterParams filters);
    Task<IEnumerable<User>> GetOptionsAsync(UserOptionFilterParams filters);
    Task<User?> GetByIdAsync(int userId);
    Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);

    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);


    Task<int> CreateAsync(User user);

    Task<bool> UpdateAsync(User user);

    Task<bool> UpdatePasswordAsync(int userId, string newPasswordHash);

    Task<bool> SoftDeleteAsync(int userId);
}
