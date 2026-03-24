namespace Project.Domain.Enums;

public static class TaskStatus
{
    public const string NotStarted = "not_started";
    public const string InProgress = "in_progress";
    public const string Done = "done";
    public const string Blocked = "blocked";

    public static readonly string[] All = [NotStarted, InProgress, Done, Blocked];
}
