namespace AzureMcp.ApplicationInsights.Options;

public static class ProfilerOptionDefinitions
{
    public const string ResourceNameName = "resource-name";
    public const string ResourceGroupName = "resource-group";
    public const string MaxInsightsName = "max-insights";
    public const string StartDateTimeUtcName = "start-date-time-utc";
    public const string EndDateTimeUtcName = "end-date-time-utc";


    public static readonly Option<DateTime> StartDateTimeUtc = new(
        $"--{StartDateTimeUtcName}",
        "The start date time for the insights query. Defaults to 24 hours ago. Formatted as ISO 8601."
    )
    {
        IsRequired = false
    };

    public static readonly Option<DateTime> EndDateTimeUtc = new(
        $"--{EndDateTimeUtcName}",
        "The end date time for the insights query. Defaults to the current time. Formatted as ISO 8601."
    )
    {
        IsRequired = false
    };

    public static readonly Option<int> MaxInsights = new(
        $"--{MaxInsightsName}",
        "The maximum number of insights to return. Defaults to 50."
    )
    {
        IsRequired = false,
    };
}
