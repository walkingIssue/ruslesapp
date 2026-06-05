using rustles_platform.Services;

namespace rustles_platform.tests.Services;

internal sealed class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
}
