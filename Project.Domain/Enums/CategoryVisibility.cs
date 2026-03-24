namespace Project.Domain.Enums;

public static class CategoryVisibility
{
    public const string Global = "global";
    public const string Department = "department";
    public const string Private = "private";

    public static readonly string[] All = [Global, Department, Private];
}
