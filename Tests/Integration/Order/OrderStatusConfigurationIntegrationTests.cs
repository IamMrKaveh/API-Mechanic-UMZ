using Domain.Order.Entities;
using Infrastructure.Persistence.Context;

namespace Tests.Integration.Order;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrderStatusConfigurationIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");
        _context = _fixture.CreateContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable) return;
        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [Fact]
    public async Task OrderStatusConfiguration_EnforcesUniqueName()
    {
        _context.OrderStatuses.Add(OrderStatus.Create("Paid", "پرداخت شده", null, null, 4, false, false));
        await _context.SaveChangesAsync();

        await using var second = _fixture.CreateContext();
        second.OrderStatuses.Add(OrderStatus.Create("Paid", "پرداخت شده 2", null, null, 5, false, false));
        await Should.ThrowAsync<DbUpdateException>(async () => await second.SaveChangesAsync());
    }

    [Fact]
    public async Task OrderStatusConfiguration_PersistsAllScalarProperties()
    {
        var s = OrderStatus.Create("Shipped", "ارسال شده", "truck", "#6610f2", 6, true, false);
        _context.OrderStatuses.Add(s);
        await _context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        var loaded = await verify.OrderStatuses.AsNoTracking().FirstAsync(x => x.Id == s.Id);
        loaded.Name.ShouldBe("Shipped");
        loaded.DisplayName.ShouldBe("ارسال شده");
        loaded.Icon.ShouldBe("truck");
        loaded.Color.ShouldBe("#6610f2");
        loaded.SortOrder.ShouldBe(6);
        loaded.AllowCancel.ShouldBeTrue();
        loaded.AllowEdit.ShouldBeFalse();
        loaded.IsActive.ShouldBeTrue();
        loaded.IsDefault.ShouldBeFalse();
        loaded.RowVersion.ShouldNotBeNull();
        loaded.RowVersion.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task OrderStatusConfiguration_IgnoresDomainEvents()
    {
        var s = OrderStatus.Create("Delivered", "تحویل شده", null, null, 7, false, false);
        _context.OrderStatuses.Add(s);
        var save = async () => await _context.SaveChangesAsync();
        await save.ShouldNotThrowAsync();
    }
}
