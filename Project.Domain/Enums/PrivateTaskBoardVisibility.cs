namespace Project.Domain.Enums;

public static class PrivateTaskBoardVisibility
{
    public const string OnlyMe = "only_me";
    public const string AssignedUsers = "assigned_users";
    public const string ProjectMembers = "project_members";

    public static readonly string[] All = [OnlyMe, AssignedUsers, ProjectMembers];
}
