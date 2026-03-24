namespace Project.Domain.Entities;

public sealed class PasswordResetToken
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConsumedAt { get; set; }
}
