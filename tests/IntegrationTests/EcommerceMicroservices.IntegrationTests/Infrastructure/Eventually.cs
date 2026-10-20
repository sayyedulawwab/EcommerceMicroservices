namespace EcommerceMicroservices.IntegrationTests.Infrastructure;

public static class Eventually
{
    /// <summary>Polls <paramref name="probe"/> until it returns a value matching <paramref name="isDone"/>, or fails the test.</summary>
    public static async Task<T> UntilAsync<T>(Func<Task<T>> probe, Func<T, bool> isDone, string description, TimeSpan? timeout = null)
    {
        DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(90));
        T last = await probe();

        while (!isDone(last))
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail($"Timed out waiting for: {description}. Last value: {last}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));

            last = await probe();
        }

        return last;
    }
}
