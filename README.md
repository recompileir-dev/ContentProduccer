# Content Producer

Content Producer is an automated content production project. The first MVP
contains a small scheduler agent that starts the content production service at
a configured time every day.

## Current MVP

- C# Worker Service
- One daily execution time
- Configurable time zone
- No database
- No distributed lock
- No retry policy
- No external scheduler package

## Run

Set the schedule in
`src/ContentProducer.Worker/appsettings.json`:

```json
{
  "Scheduler": {
    "StartAt": "08:00:00",
    "TimeZoneId": "Asia/Tehran"
  }
}
```

Then run:

```bash
dotnet run --project src/ContentProducer.Worker
```

The content production workflow will be implemented in
`ContentProductionService.RunAsync`.

More project documentation is available in [docs](./docs/README.md).
