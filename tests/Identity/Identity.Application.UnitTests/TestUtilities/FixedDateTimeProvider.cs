using Identity.Application.Abstractions.Clock;

namespace Identity.Application.UnitTests.TestUtilities;

internal sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow => utcNow;
}
