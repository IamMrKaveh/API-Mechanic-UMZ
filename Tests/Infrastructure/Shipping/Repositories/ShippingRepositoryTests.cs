using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Shipping.Repositories;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Database;
using Shippings = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Infrastructure.Shipping.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ShippingRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private ShippingRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new ShippingRepository(_context);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private static Shippings CreateShipping(
        string name = "Standard Post",
        decimal baseCost = 1000m,
        string? description = "Standard delivery method",
        string? estimatedDeliveryTime = "3 تا 5 روز کاری",
        int minDays = 3,
        int maxDays = 5)
    {
        return Shippings.Create(
            ShippingName.Create(name),
            Money.Create(baseCost),
            description,
            estimatedDeliveryTime,
            minDays,
            maxDays);
    }

    [RequiresDockerFact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsAggregateFromDatabase()
    {
        var shipping = CreateShipping();

        await _sut.AddAsync(shipping);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(shipping.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(shipping.Id);
        loaded.Name.Value.ShouldBe("Standard Post");
        loaded.Description.ShouldBe("Standard delivery method");
        loaded.EstimatedDeliveryTime.ShouldBe("3 تا 5 روز کاری");
        loaded.BaseCost.Amount.ShouldBe(1000m);
        loaded.BaseCost.Currency.ShouldBe("IRT");
        loaded.DeliveryTime.MinDays.ShouldBe(3);
        loaded.DeliveryTime.MaxDays.ShouldBe(5);
        loaded.IsActive.ShouldBeTrue();
        loaded.IsDefault.ShouldBeFalse();
        loaded.SortOrder.ShouldBe(0);
        loaded.FreeShipping.IsEnabled.ShouldBeFalse();
        loaded.FreeShipping.ThresholdAmount.ShouldBeNull();
        loaded.OrderRange.MinOrderAmount.ShouldBeNull();
        loaded.OrderRange.MaxOrderAmount.ShouldBeNull();
        loaded.MaxWeight.ShouldBeNull();
        loaded.IsDeleted.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(ShippingId.NewId());

        loaded.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetAllAsync_WhenIncludeInactiveFalse_ReturnsOnlyActive()
    {
        var active = CreateShipping("Active Method", 500m);
        var inactive = CreateShipping("Inactive Method", 700m);
        inactive.RequestDeletion(null);

        await _sut.AddAsync(active);
        await _sut.AddAsync(inactive);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetAllAsync(includeInactive: false);

        result.Count.ShouldBe(1);
        result.ShouldContain(s => s.Id == active.Id);
        result.ShouldNotContain(s => s.Id == inactive.Id);
    }

    [RequiresDockerFact]
    public async Task GetAllAsync_WhenIncludeInactiveTrue_ReturnsAllShippings()
    {
        var active = CreateShipping("Active Method", 500m);
        var inactive = CreateShipping("Inactive Method", 700m);
        inactive.RequestDeletion(null);

        await _sut.AddAsync(active);
        await _sut.AddAsync(inactive);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetAllAsync(includeInactive: true);

        result.Count.ShouldBe(2);
        result.ShouldContain(s => s.Id == active.Id);
        result.ShouldContain(s => s.Id == inactive.Id);
    }

    [RequiresDockerFact]
    public async Task GetByIdsAsync_ReturnsOnlyRequestedShippings()
    {
        var first = CreateShipping("First Method", 400m);
        var second = CreateShipping("Second Method", 600m);
        var third = CreateShipping("Third Method", 800m);

        await _sut.AddAsync(first);
        await _sut.AddAsync(second);
        await _sut.AddAsync(third);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdsAsync(new[] { first.Id, third.Id });

        result.Count.ShouldBe(2);
        result.ShouldContain(s => s.Id == first.Id);
        result.ShouldContain(s => s.Id == third.Id);
        result.ShouldNotContain(s => s.Id == second.Id);
    }

    [RequiresDockerFact]
    public async Task GetByIdsAsync_WhenNoMatch_ReturnsEmptyCollection()
    {
        var seeded = CreateShipping("Seeded Method", 500m);
        await _sut.AddAsync(seeded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdsAsync(new[] { ShippingId.NewId(), ShippingId.NewId() });

        result.ShouldBeEmpty();
    }

    [RequiresDockerFact]
    public async Task GetDefaultAsync_WhenActiveDefaultExists_ReturnsIt()
    {
        var defaultShipping = CreateShipping("Default Method", 900m);
        defaultShipping.SetAsDefault();

        var otherShipping = CreateShipping("Other Method", 500m);

        await _sut.AddAsync(defaultShipping);
        await _sut.AddAsync(otherShipping);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetDefaultAsync();

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(defaultShipping.Id);
        result.IsDefault.ShouldBeTrue();
        result.IsActive.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task GetDefaultAsync_WhenNoDefaultExists_ReturnsNull()
    {
        var shipping = CreateShipping("Non Default Method", 500m);
        await _sut.AddAsync(shipping);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetDefaultAsync();

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task ExistsByNameAsync_WhenNameExists_ReturnsTrue()
    {
        var name = ShippingName.Create("Unique Shipping Name");
        var shipping = Shippings.Create(
            name,
            Money.Create(500m));

        await _sut.AddAsync(shipping);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByNameAsync(name);

        exists.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task ExistsByNameAsync_WhenNameDoesNotExist_ReturnsFalse()
    {
        var seeded = CreateShipping("Seeded Method", 500m);
        await _sut.AddAsync(seeded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByNameAsync(ShippingName.Create("Non Existing Name"));

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task ExistsByNameAsync_WithExcludeId_ExcludesThatShipping()
    {
        var name = ShippingName.Create("Reusable Shipping Name");
        var shipping = Shippings.Create(name, Money.Create(500m));

        await _sut.AddAsync(shipping);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var existsExcludingSelf = await _sut.ExistsByNameAsync(name, shipping.Id);
        var existsWithoutExclusion = await _sut.ExistsByNameAsync(name);

        existsExcludingSelf.ShouldBeFalse();
        existsWithoutExclusion.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task Update_AfterMutatingAggregate_PersistsChangesToDatabase()
    {
        var shipping = CreateShipping("Original Name", 500m, "Original description", "Original ETA", 2, 4);
        await _sut.AddAsync(shipping);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(shipping.Id);
        loaded.ShouldNotBeNull();

        loaded!.Update(
            ShippingName.Create("Updated Name"),
            Money.Create(1200m),
            "Updated description",
            "Updated ETA",
            5,
            10);

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(shipping.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Name.Value.ShouldBe("Updated Name");
        reloaded.BaseCost.Amount.ShouldBe(1200m);
        reloaded.Description.ShouldBe("Updated description");
        reloaded.EstimatedDeliveryTime.ShouldBe("Updated ETA");
        reloaded.DeliveryTime.MinDays.ShouldBe(5);
        reloaded.DeliveryTime.MaxDays.ShouldBe(10);
        reloaded.UpdatedAt.ShouldNotBeNull();
    }

    [RequiresDockerFact]
    public async Task Update_AfterSetAsDefault_PersistsIsDefaultFlag()
    {
        var shipping = CreateShipping("Default Candidate", 500m);
        await _sut.AddAsync(shipping);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(shipping.Id);
        loaded.ShouldNotBeNull();
        loaded!.SetAsDefault();

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(shipping.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.IsDefault.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task AddAsync_WhenNameIsDuplicate_ThrowsDbUpdateExceptionDueToUniqueIndex()
    {
        var firstName = ShippingName.Create("Duplicated Name");
        var first = Shippings.Create(firstName, Money.Create(500m));
        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var second = Shippings.Create(
            ShippingName.Create("Duplicated Name"),
            Money.Create(700m));
        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [RequiresDockerFact]
    public async Task RequestDeletion_ThenPersist_MarksAggregateInactive()
    {
        var shipping = CreateShipping("To Be Deactivated", 500m);
        await _sut.AddAsync(shipping);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(shipping.Id);
        loaded.ShouldNotBeNull();
        loaded!.RequestDeletion(UserId.NewId());

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(shipping.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.IsActive.ShouldBeFalse();
    }
}
