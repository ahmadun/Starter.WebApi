namespace Project.Domain.Enums;

public static class ColumnType
{
    public const string FreeText = "free_text";
    public const string Option = "option";
    public const string Checkbox = "checkbox";

    public static readonly string[] All = [FreeText, Option, Checkbox];
}
