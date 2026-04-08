namespace Project.Domain.Entities;

public sealed class User
{
    public long UserId { get; set; }
    public long TenantId { get; set; }
    public long? MemberId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string UserType { get; set; } = "internal";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Member? Member { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsLockedOut => LockoutUntil.HasValue && LockoutUntil.Value > DateTime.UtcNow;
    public string PrimaryRole => Roles.FirstOrDefault() ?? (UserType == "member" ? "member" : "admin");
}
