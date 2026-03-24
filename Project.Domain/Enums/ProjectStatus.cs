namespace Project.Domain.Enums;

public static class ProjectStatus
{
    public const string Active = "active";
    public const string Archived = "archived";
    public const string Completed = "completed";

    public static readonly string[] All = [Active, Archived, Completed];
}
