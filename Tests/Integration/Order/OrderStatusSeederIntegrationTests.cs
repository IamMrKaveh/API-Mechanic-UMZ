using Infrastructure.Order.Seeders; using Infrastructure.Persistence.Context; using Microsoft.EntityFrameworkCore; using Microsoft.Extensions.DependencyInjection; using Tests.TestInfrastructure.Database;

namespace Tests.Integration.Order;

[Collection(nameof(DatabaseCollection))] [Trait("Category", "Integration")] public class OrderStatusSeederIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime { private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private IServiceScopeFactory _scopeFactory = null!; private OrderStatusSeeder _sut = null!;

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

    var logger = Substitute.For<ILogger<OrderStatusSeeder>>();
    _sut = new OrderStatusSeeder(_scopeFactory, logger);
    return Task.CompletedTask;
}

public async Task DisposeAsync()
{
    if (!_fixture.IsDockerAvailable) return;
    await _context.DisposeAsync();
    await _fixture.ResetAsync();
}

[RequiresDockerFact]
public async Task StartAsync_InsertsAllTwelveStatuses_WhenDatabaseEmpty()
{
    await _sut.StartAsync(CancellationToken.None);

    await using var verify = _fixture.CreateContext();
    var names = await verify.OrderStatuses.AsNoTracking().Select(s => s.Name).ToListAsync();

    names.Count.ShouldBe(12);
    names.ShouldContain("Created");
    names.ShouldContain("Reserved");
    names.ShouldContain("Pending");
    names.ShouldContain("Failed");
    names.ShouldContain("Paid");
    names.ShouldContain("Processing");
    names.ShouldContain("Shipped");
    names.ShouldContain("Delivered");
    names.ShouldContain("Cancelled");
    names.ShouldContain("Returned");
    names.ShouldContain("Refunded");
    names.ShouldContain("Expired");
}

[RequiresDockerFact]
public async Task StartAsync_MarksCreatedAsDefaultAndActive()
{
    await _sut.StartAsync(CancellationToken.None);

    await using var verify = _fixture.CreateContext();
    var created = await verify.OrderStatuses.AsNoTracking().FirstAsync(s => s.Name == "Created");
    created.IsDefault.ShouldBeTrue();
    created.IsActive.ShouldBeTrue();
}

[RequiresDockerFact]
public async Task StartAsync_MarksNonDefaultsAsActiveAndNotDefault()
{
    await _sut.StartAsync(CancellationToken.None);

    await using var verify = _fixture.CreateContext();
    var reserved = await verify.OrderStatuses.AsNoTracking().FirstAsync(s => s.Name == "Reserved");
    reserved.IsActive.ShouldBeTrue();
    reserved.IsDefault.ShouldBeFalse();
}

[RequiresDockerFact]
public async Task StartAsync_IsIdempotent_DoesNotDuplicateExistingStatuses()
{
    await _sut.StartAsync(CancellationToken.None);
    await _sut.StartAsync(CancellationToken.None);

    await using var verify = _fixture.CreateContext();
    var count = await verify.OrderStatuses.AsNoTracking().CountAsync();
    count.ShouldBe(12);
}
}