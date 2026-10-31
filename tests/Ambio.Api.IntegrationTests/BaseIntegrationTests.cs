using System.Net.Http.Json;

using Ambio.Api.Data;

using Microsoft.Extensions.DependencyInjection;

namespace Ambio.Api.IntegrationTests;

[Collection(AmbioApiFactory.BaseCollectionDefinition)]
public abstract class BaseIntegrationTests : IAsyncLifetime
{
    protected readonly HttpClient Client;
    private readonly AmbioApiFactory _factory;

    private AsyncServiceScope _testScope;
    private AsyncServiceScope _verificationScope;
    protected AmbioDbContext DbContext { get; private set; } = null!;
    protected AmbioDbContext VerificationDbContext { get; private set; } = null!;

    protected BaseIntegrationTests(AmbioApiFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();
    }

    public async ValueTask InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        _testScope = _factory.Services.CreateAsyncScope();
        DbContext = _testScope.ServiceProvider.GetRequiredService<AmbioDbContext>();

        _verificationScope = _factory.Services.CreateAsyncScope();
        VerificationDbContext = _verificationScope.ServiceProvider.GetRequiredService<AmbioDbContext>();
    }

    public async ValueTask DisposeAsync()
    {
        await _testScope.DisposeAsync();
        await _verificationScope.DisposeAsync();
    }

    protected static async Task<T> ReadContentAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken);
        Assert.NotNull(content);
        return content;
    }
}
