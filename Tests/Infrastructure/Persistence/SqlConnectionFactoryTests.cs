using Application.Common.Contracts;
using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace Tests.Infrastructure.Persistence;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class SqlConnectionFactoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _fixture.ResetAsync();
    }

    [Fact]
    public void Ctor_WithoutDefaultConnectionString_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Should.Throw<InvalidOperationException>(() => new SqlConnectionFactory(configuration));
    }

    [Fact]
    public void Ctor_WithEmptyDefaultConnectionString_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = null
            })
            .Build();

        Should.Throw<InvalidOperationException>(() => new SqlConnectionFactory(configuration));
    }

    [Fact]
    public void ImplementsISqlConnectionFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=db;Username=u;Password=p"
            })
            .Build();

        new SqlConnectionFactory(configuration).ShouldBeAssignableTo<ISqlConnectionFactory>();
    }

    [Fact]
    public async Task CreateConnectionAsync_WithValidConnectionString_ReturnsOpenNpgsqlConnection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _fixture.ConnectionString
            })
            .Build();

        var sut = new SqlConnectionFactory(configuration);

        var connection = await sut.CreateConnectionAsync();

        try
        {
            connection.ShouldNotBeNull();
            connection.ShouldBeOfType<NpgsqlConnection>();
            connection.State.ShouldBe(System.Data.ConnectionState.Open);
        }
        finally
        {
            connection.Dispose();
        }
    }
}
