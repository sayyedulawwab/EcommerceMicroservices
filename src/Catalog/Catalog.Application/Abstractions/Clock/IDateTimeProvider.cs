namespace Catalog.Application.Abstractions.Clock;
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}