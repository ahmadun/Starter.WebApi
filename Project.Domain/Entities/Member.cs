namespace Project.Domain.Entities;

public sealed class Member
{
    public long MemberId { get; set; }
    public long TenantId { get; set; }
    public string MemberNo { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? IdentityNo { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public DateOnly JoinDate { get; set; }
    public string MemberStatus { get; set; } = "active";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
