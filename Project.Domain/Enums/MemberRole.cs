namespace Project.Domain.Enums;

public static class MemberRole
{
    public const string Owner = "owner";
    public const string Manager = "manager";
    public const string Editor = "editor";
    public const string Viewer = "viewer";

    public static readonly string[] All = [Owner, Manager, Editor, Viewer];
}
