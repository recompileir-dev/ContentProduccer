namespace ContentProducer.Worker;

public sealed class SchedulerOptions
{
    public const string SectionName = "Scheduler";

    public string StartAt { get; init; } = "08:00:00";

    public string TimeZoneId { get; init; } = "Asia/Tehran";
}
