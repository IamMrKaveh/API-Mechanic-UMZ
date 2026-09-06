using Infrastructure.Order.Seeders;
using Infrastructure.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure.Order.Seeders;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrderStatusSeederTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private ILogger<OrderStatusSeeder> _logger = null!;
    private OrderStatusSeeder _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");
        _context = _fixture.CreateContext();

        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(DBContext)).Returns(_context);
        scope.ServiceProvider.Returns(provider);

        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scopeFactory.CreateScope().Returns(scope);

        _logger = Substitute.For<ILogger<OrderStatusSeeder>>();
        _sut = new OrderStatusSeeder(_scopeFactory, _logger);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [Fact]
    public async Task StopAsync_CompletesWithoutThrowing()
    {
        await _sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_SeedsSortOrdersSequentiallyFromZero()
    {
        await _sut.StartAsync(CancellationToken.None);

        await using var verify = _fixture.CreateContext();
        var orders = await verify.OrderStatuses.AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new { s.Name, s.SortOrder })
            .ToListAsync();

        orders.Count.ShouldBe(12);
        orders.Select((x, i) => (x, i)).ShouldAllBe(t => t.x.SortOrder == t.i);
        orders.First().Name.ShouldBe("Created");
        orders.Last().Name.ShouldBe("Expired");
    }

    [Fact]
    public async Task StartAsync_SetsCancelAndEditFlagsPerDefinition()
    {
        await _sut.StartAsync(CancellationToken.None);

        await using var verify = _fixture.CreateContext();
        var created = await verify.OrderStatuses.AsNoTracking().FirstAsync(s => s.Name == "Created");
        created.AllowCancel.ShouldBeTrue();
        created.AllowEdit.ShouldBeTrue();

        var paid = await verify.OrderStatuses.AsNoTracking().FirstAsync(s => s.Name == "Paid");
        paid.AllowCancel.ShouldBeTrue();
        paid.AllowEdit.ShouldBeFalse();

        var shipped = await verify.OrderStatuses.AsNoTracking().FirstAsync(s => s.Name == "Shipped");
        shipped.AllowCancel.ShouldBeFalse();
        shipped.AllowEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_SetsIconsAndColors()
    {
        await _sut.StartAsync(CancellationToken.None);

        await using var verify = _fixture.CreateContext();
        var paid = await verify.OrderStatuses.AsNoTracking().FirstAsync(s => s.Name == "Paid");
        paid.Icon.ShouldBe("payments");
        paid.Color.ShouldBe("#28a745");
        paid.DisplayName.ShouldNotBeNullOrWhiteSpace();

        var expired = await verify.OrderStatuses.AsNoTracking().FirstAsync(s => s.Name == "Expired");
        expired.Icon.ShouldBe("hourglass_disabled");
    }

    [Fact]
    public async Task StartAsync_MarksOnlyCreatedAsDefault()
    {
        await _sut.StartAsync(CancellationToken.None);

        await using var verify = _fixture.CreateContext();
        var defaults = await verify.OrderStatuses.AsNoTracking()
            .Where(s => s.IsDefault)
            .Select(s => s.Name)
            .ToListAsync();

        defaults.ShouldBe(["Created"]);
    }

    [Fact]
    public async Task StartAsync_WhenSaveFails_LogsErrorAndDoesNotThrow()
    {
        await _context.DisposeAsync();

        await _sut.StartAsync(CancellationToken.None);
    }
}
