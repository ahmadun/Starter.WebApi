namespace Project.Domain.Enums;

public static class PlacementZone
{
    public const string AfterTask = "after_task";
    public const string AfterTimeline = "after_timeline";

    public static readonly string[] All = [AfterTask, AfterTimeline];
}
