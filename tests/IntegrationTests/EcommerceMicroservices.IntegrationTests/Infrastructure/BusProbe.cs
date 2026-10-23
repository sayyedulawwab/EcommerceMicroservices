using MassTransit;
using System.Collections.Concurrent;

namespace EcommerceMicroservices.IntegrationTests.Infrastructure;

/// <summary>
/// Listens to every message of type <typeparamref name="T"/> that passes the broker from the moment it starts, through
/// a temporary queue of its own, so a test can see which integration events a service published (or that it published none).
/// </summary>
public sealed class BusProbe<T> : IAsyncDisposable
    where T : class
{
    private readonly ConcurrentQueue<T> _messages = new();
    private HostReceiveEndpointHandle? _endpoint;

    private BusProbe()
    {
    }

    public IReadOnlyList<T> Messages => [.. _messages];

    /// <summary>Starts listening on the broker the given service's bus is connected to.</summary>
    public static async Task<BusProbe<T>> StartAsync(IBus bus)
    {
        var probe = new BusProbe<T>();

        probe._endpoint = bus.ConnectReceiveEndpoint(
            $"test-probe-{typeof(T).Name}-{Guid.NewGuid():N}",
            endpoint =>
            {
                if (endpoint is IRabbitMqReceiveEndpointConfigurator rabbitMq)
                {
                    rabbitMq.Durable = false;
                    rabbitMq.AutoDelete = true;
                }

                endpoint.Handler<T>(context =>
                {
                    probe._messages.Enqueue(context.Message);

                    return Task.CompletedTask;
                });
            });

        await probe._endpoint.Ready;

        return probe;
    }

    /// <summary>Waits for a message matching <paramref name="predicate"/> and returns it.</summary>
    public async Task<T> WaitForAsync(Func<T, bool> predicate, string description)
    {
        return await Eventually.UntilAsync(
            () => Task.FromResult(_messages.FirstOrDefault(predicate)),
            message => message is not null,
            description) ?? throw new InvalidOperationException(description);
    }

    /// <summary>
    /// Gives the service time to publish something it should not have, then returns the matching messages seen so far.
    /// </summary>
    public async Task<IReadOnlyList<T>> ExpectNoneAsync(Func<T, bool> predicate)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        return [.. _messages.Where(predicate)];
    }

    public async ValueTask DisposeAsync()
    {
        if (_endpoint is not null)
        {
            await _endpoint.StopAsync();
        }
    }
}
