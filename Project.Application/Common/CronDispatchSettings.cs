namespace Project.Application.Common;

public sealed class CronDispatchSettings
{
    public const string SectionName = "CronDispatch";

    public string HeaderName { get; set; } = "X-Cron-Key";

    public string ApiKey { get; set; } = string.Empty;
}
