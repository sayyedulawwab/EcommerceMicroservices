extern alias CartApi;
extern alias CatalogApi;
extern alias IdentityApi;
extern alias OrderingApi;
extern alias PaymentApi;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using System.Net.Http.Headers;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using CartProgram = CartApi::Program;
using CatalogProgram = CatalogApi::Program;
using IdentityProgram = IdentityApi::Program;
using OrderingProgram = OrderingApi::Program;
using PaymentProgram = PaymentApi::Program;

namespace EcommerceMicroservices.IntegrationTests.Infrastructure;

/// <summary>
/// The whole system in one process: SQL Server, RabbitMQ and Redis in containers, and every service hosted
/// in-memory against them. Services talk to each other only through the real broker, as they do in production.
/// Shared by all test classes (one instance of each consumer per queue), so the classes run one after another.
/// </summary>
public sealed class SystemFixture : IAsyncLifetime
{
    public const decimal PaymentLimit = 5000m;

    private const string BrokerUser = "guest";

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4.2.5-management-alpine")
        .WithUsername(BrokerUser)
        .WithPassword(BrokerUser)
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:alpine")
        .Build();

    private readonly string _logDirectory = Path.Combine(Path.GetTempPath(), "ecommerce-integration-tests", Guid.NewGuid().ToString("N"));

    public ServiceFactory<IdentityProgram> Identity { get; private set; } = null!;
    public ServiceFactory<CatalogProgram> Catalog { get; private set; } = null!;
    public ServiceFactory<CartProgram> Cart { get; private set; } = null!;
    public ServiceFactory<OrderingProgram> Ordering { get; private set; } = null!;
    public ServiceFactory<PaymentProgram> Payment { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_sqlServer.StartAsync(), _rabbitMq.StartAsync(), _redis.StartAsync());

        Identity = new ServiceFactory<IdentityProgram>(Settings("identity", database: "IdentityDB"));
        Catalog = new ServiceFactory<CatalogProgram>(Settings("catalog", database: "CatalogDB"));
        Cart = new ServiceFactory<CartProgram>(Settings("cart", database: null));
        Ordering = new ServiceFactory<OrderingProgram>(Settings("ordering", database: "OrderingDB"));
        Payment = new ServiceFactory<PaymentProgram>(Settings("payment", database: null));

        // Start every host, and with it every bus, before the first test publishes anything.
        _ = Identity.Server;
        _ = Catalog.Server;
        _ = Cart.Server;
        _ = Ordering.Server;
        _ = Payment.Server;
    }

    public async ValueTask DisposeAsync()
    {
        await Payment.DisposeAsync();
        await Ordering.DisposeAsync();
        await Cart.DisposeAsync();
        await Catalog.DisposeAsync();
        await Identity.DisposeAsync();

        await _redis.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }

    /// <summary>A client that authenticates with a bearer token (the services' fallback when there is no cookie).</summary>
    public static HttpClient CreateClient<TProgram>(ServiceFactory<TProgram> factory, string? bearerToken = null)
        where TProgram : class
    {
        HttpClient client = factory.CreateClient();

        if (bearerToken is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return client;
    }

    /// <summary>A browser-like session against Identity.</summary>
    public BrowserSession CreateBrowserSession()
    {
        return new BrowserSession(Identity.CreateDefaultClient);
    }

    /// <summary>A client without a cookie jar, for sending hand-made Cookie headers.</summary>
    public HttpClient CreateCookielessIdentityClient()
    {
        return Identity.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = BrowserSession.BaseAddress,
            HandleCookies = false
        });
    }

    private Dictionary<string, string?> Settings(string service, string? database)
    {
        var settings = new Dictionary<string, string?>
        {
            ["MessageBroker:Host"] = $"amqp://{_rabbitMq.Hostname}:{_rabbitMq.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort)}",
            ["MessageBroker:Username"] = BrokerUser,
            ["MessageBroker:Password"] = BrokerUser,
            ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
            ["Payment:SuccessRate"] = "1",
            ["Payment:FailAboveAmount"] = PaymentLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Serilog:WriteTo:0:Args:path"] = Path.Combine(_logDirectory, $"{service}-log-.json"),
            ["Serilog:WriteTo:1:Args:path"] = Path.Combine(_logDirectory, $"{service}-errorlog-.json"),
        };

        if (database is not null)
        {
            settings["ConnectionStrings:EcommerceDB"] = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
            {
                InitialCatalog = database
            }.ConnectionString;
        }

        return settings;
    }
}

[CollectionDefinition(Name)]
public sealed class SystemCollection : ICollectionFixture<SystemFixture>
{
    public const string Name = "System";
}
