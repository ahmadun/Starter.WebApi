namespace Project.Domain.Entities;


public class User
{
    public int UserId { get; set; }

    public int? EmployeeId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "Employee";

    public bool IsActive { get; set; } = true;

    public DateTime? LastLogin { get; set; }

    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutUntil { get; set; }

    public bool MustChangePassword { get; set; } = true;

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Employee? Employee { get; set; }
    public bool IsLockedOut => LockoutUntil.HasValue && LockoutUntil.Value > DateTime.UtcNow;
}