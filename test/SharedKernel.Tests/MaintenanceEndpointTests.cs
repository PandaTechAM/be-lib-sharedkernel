using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Maintenance;

namespace SharedKernel.Tests;

public class MaintenanceEndpointTests
{
    [Fact]
    public async Task Get_ReturnsCurrentModeAfterPut()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await CreateAppAsync(cancellationToken: ct);
        using var client = CreateClient(app);

        var initial = await client.GetFromJsonAsync<MaintenanceModeStatusResponse>("/above-board/maintenance", ct);

        Assert.NotNull(initial);
        Assert.Equal(MaintenanceMode.Disabled, initial.Mode);

        using var putResponse = await client.PutAsJsonAsync("/above-board/maintenance",
            new MaintenanceModeRequest(MaintenanceMode.EnabledForClients), ct);
        putResponse.EnsureSuccessStatusCode();

        var updated = await client.GetFromJsonAsync<MaintenanceModeStatusResponse>("/above-board/maintenance", ct);

        Assert.NotNull(updated);
        Assert.Equal(MaintenanceMode.EnabledForClients, updated.Mode);
    }

    [Fact]
    public async Task Get_WithQuerySecret_RejectsInvalidSecret()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var app = await CreateAppAsync("expected-secret", ct);
        using var client = CreateClient(app);

        using var unauthorized = await client.GetAsync("/above-board/maintenance?secret=wrong-secret", ct);
        using var authorized = await client.GetAsync("/above-board/maintenance?secret=expected-secret", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
    }

    private static async Task<WebApplication> CreateAppAsync(string? querySecret = null,
        CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton<HybridCache, TestHybridCache>();
        builder.AddMaintenanceMode();

        var app = builder.Build();
        app.UseMaintenanceMode();
        app.MapMaintenanceEndpoint(querySecret);
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync(cancellationToken);

        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!;
        return new HttpClient
        {
            BaseAddress = new Uri(addresses.Addresses.Single())
        };
    }

    private sealed class TestHybridCache : HybridCache
    {
        private readonly ConcurrentDictionary<string, object?> _values = new();

        public override async ValueTask<T> GetOrCreateAsync<TState, T>(string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            if (_values.TryGetValue(key, out var value))
            {
                return (T)value!;
            }

            var created = await factory(state, cancellationToken);
            _values[key] = created;
            return created;
        }

        public override ValueTask SetAsync<T>(string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            _values[key] = value;
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.TryRemove(key, out _);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
