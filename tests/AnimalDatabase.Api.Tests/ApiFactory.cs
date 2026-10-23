using System.Net;
using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalDatabase.Api.Tests;

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string PeerHeader = "X-Test-Peer";
    public const string TestKey = "test-admin-key-for-rate-limits";

    private readonly string _environment;
    private readonly Dictionary<string, string?> _settings;

    public ApiFactory(string environment = "Testing", Dictionary<string, string?>? settings = null)
    {
        _environment = environment;
        _settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:AnimalDatabase"] = "Server=127.0.0.1;Port=1;Database=none;User=none",
            ["Admin:ApiKey"] = TestKey
        };
        foreach (var pair in settings ?? new Dictionary<string, string?>())
        {
            _settings[pair.Key] = pair.Value;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ISearchRepository, InMemorySearchRepository>();
            services.AddSingleton<IStartupFilter, TestPeerStartupFilter>();
        });
    }

    private sealed class TestPeerStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    var peer = context.Request.Headers[PeerHeader].ToString();
                    context.Connection.RemoteIpAddress = IPAddress.Parse(string.IsNullOrEmpty(peer) ? "127.0.0.1" : peer);
                    await nextMiddleware();
                });
                next(app);
            };
        }
    }

    private sealed class InMemorySearchRepository : ISearchRepository
    {
        public Task<bool> AddAsync(CreateSearchRequest request) => Task.FromResult(true);

        public Task<IReadOnlyList<TopSearch>> GetTopAsync(int limit, int? days) =>
            Task.FromResult<IReadOnlyList<TopSearch>>(Array.Empty<TopSearch>());
    }
}
