using Ordering.Application.Abstractions.Clock;

namespace Ordering.Infrastructure.Clock;
internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}