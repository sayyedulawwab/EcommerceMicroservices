using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Infrastructure;

/// <summary>Ordering calls the tests use to drive and observe it.</summary>
internal static class OrderingApi
{
    public static HttpClient CustomerClient(SystemFixture system, Guid userId)
    {
        return SystemFixture.CreateClient(system.Ordering, TestJwt.Create(userId));
    }

    public static HttpClient AdminClient(SystemFixture system)
    {
        return SystemFixture.CreateClient(system.Ordering, TestJwt.Create(TestJwt.NewUserId(), isAdmin: true));
    }

    /// <summary>The bus of the Ordering service, to publish the integration events other services would send it.</summary>
    public static IBus Bus(SystemFixture system)
    {
        return system.Ordering.Services.GetRequiredService<IBus>();
    }

    /// <summary>Reads the <c>id</c> property of an accepted response body.</summary>
    public static async Task<Guid> ReadIdAsync(HttpResponseMessage response)
    {
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        return body.GetProperty("id").GetGuid();
    }

    public static Task<HttpResponseMessage> PostAsync(HttpClient client, string url)
    {
        return client.PostAsync(url, content: null, TestContext.Current.CancellationToken);
    }

    public static Task<HttpResponseMessage> PlaceAsync(HttpClient client, params (Guid ProductId, int Quantity)[] items)
    {
        return client.PostAsJsonAsync(
            "/api/v1/orders",
            new { orderItems = items.Select(item => new { productId = item.ProductId, quantity = item.Quantity }).ToArray() },
            TestContext.Current.CancellationToken);
    }

    /// <summary>Reads an order through an admin client, which can see every user's orders.</summary>
    public static async Task<JsonElement> GetOrderAsync(HttpClient admin, Guid orderId)
    {
        return await admin.GetFromJsonAsync<JsonElement>($"/api/v1/orders/{orderId}", TestContext.Current.CancellationToken);
    }

    public static async Task<string?> GetStatusAsync(HttpClient admin, Guid orderId)
    {
        return (await GetOrderAsync(admin, orderId)).GetProperty("status").GetString();
    }

    public static Task<JsonElement> WaitForStatusAsync(HttpClient admin, Guid orderId, string status)
    {
        return Eventually.UntilAsync(
            () => GetOrderAsync(admin, orderId),
            order => order.GetProperty("status").GetString() == status,
            $"order {orderId} to be {status}");
    }
}
