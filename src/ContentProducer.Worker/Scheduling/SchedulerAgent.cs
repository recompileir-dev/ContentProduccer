using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class SchedulerAgent : BackgroundService
{
    private readonly IContentProductionService _contentProductionService;
    private readonly ILogger<SchedulerAgent> _logger;
    private readonly TimeSpan _startAt;
    private readonly TimeZoneInfo _timeZone;

    public SchedulerAgent(
        IContentProductionService contentProductionService,
        IOptions<SchedulerOptions> options,
        ILogger<SchedulerAgent> logger)
    {
        _contentProductionService = contentProductionService;
        _logger = logger;

        SchedulerOptions schedulerOptions = options.Value;

        if (!TimeSpan.TryParse(schedulerOptions.StartAt, out _startAt))
        {
            throw new InvalidOperationException(
                $"Scheduler:StartAt value '{schedulerOptions.StartAt}' is invalid.");
        }

        if (_startAt < TimeSpan.Zero || _startAt >= TimeSpan.FromDays(1))
        {
            throw new InvalidOperationException(
                "Scheduler:StartAt must be a time between 00:00:00 and 23:59:59.");
        }

        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedulerOptions.TimeZoneId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Scheduler agent started. Daily execution time: {StartAt}, time zone: {TimeZoneId}.",
            _startAt,
            _timeZone.Id);

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTimeOffset nextRun = GetNextRun(DateTimeOffset.UtcNow);
            TimeSpan delay = nextRun - DateTimeOffset.UtcNow;

            _logger.LogInformation("Next content production run is scheduled for {NextRun}.", nextRun);

            await Task.Delay(delay, stoppingToken);
            await _contentProductionService.RunAsync(stoppingToken);
        }
    }

    private DateTimeOffset GetNextRun(DateTimeOffset utcNow)
    {
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        DateTime nextLocalRun = localNow.Date.Add(_startAt);

        if (nextLocalRun <= localNow.DateTime)
        {
            nextLocalRun = nextLocalRun.AddDays(1);
        }

        DateTime nextUtcRun = TimeZoneInfo.ConvertTimeToUtc(nextLocalRun, _timeZone);
        return new DateTimeOffset(nextUtcRun, TimeSpan.Zero);
    }
}
