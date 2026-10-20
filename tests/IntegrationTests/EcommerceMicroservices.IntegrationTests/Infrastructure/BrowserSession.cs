using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using System.Net;

namespace EcommerceMicroservices.IntegrationTests.Infrastructure;

/// <summary>A browser-like client: HTTPS (the auth cookies are Secure) with a cookie jar the test can inspect.</summary>
public sealed class BrowserSession : IDisposable
{
    public static readonly Uri BaseAddress = new("https://localhost");

    private readonly CookieContainerHandler _cookieHandler;

    public BrowserSession(Func<Uri, DelegatingHandler[], HttpClient> createClient)
    {
        Cookies = new CookieContainer();
        _cookieHandler = new CookieContainerHandler(Cookies);
        Client = createClient(BaseAddress, [_cookieHandler]);
    }

    public HttpClient Client { get; }

    public CookieContainer Cookies { get; }

    public string? GetCookie(string name) => Cookies.GetCookies(BaseAddress)[name]?.Value;

    public void Dispose()
    {
        Client.Dispose();
        _cookieHandler.Dispose();
    }
}
