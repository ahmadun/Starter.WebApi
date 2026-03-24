namespace Project.Application.Common;

public sealed class ApprovalNotificationSettings
{
    public const string SectionName = "ApprovalNotifications";

    public bool Enabled { get; init; }
    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string SenderEmail { get; init; } = string.Empty;
    public string SenderName { get; init; } = "Project Management System";
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? FrontendBaseUrl { get; init; }
    public string? ProjectUrlTemplate { get; init; }
}
