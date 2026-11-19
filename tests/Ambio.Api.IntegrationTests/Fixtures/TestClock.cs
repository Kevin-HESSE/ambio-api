namespace Ambio.Api.IntegrationTests.Fixtures;

/// <summary>Fixed starting point of the fake clock. Fixtures are generated relative to it,
/// so that tests do not depend on the real date.</summary>
public static class TestClock
{
    public static readonly DateTimeOffset Start = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    public static DateOnly Today => DateOnly.FromDateTime(Start.UtcDateTime);

    // Other tests advance the shared fake clock, so "today" must be read from it, not from Start
    public static DateOnly TodayFromProvider(TimeProvider provider) => DateOnly.FromDateTime(provider.GetLocalNow().DateTime);
}
