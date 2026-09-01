using Domain.Order.Entities;
using Domain.Order.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Tests.Infrastructure.Order.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrderStatusConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private static OrderStatus BuildStatus(
        string? name = null,
        string displayName = "نمایش",
        int sortOrder = 0,
        bool allowCancel = false,
        bool allowEdit = false)
    {
        var status = OrderStatus.Create(
            name: name ?? $"Status-{Guid.NewGuid():N}"[..20],
            displayName: displayName,
            icon: "clock",
            color: "gray",
            sortOrder: sortOrder,
            allowCancel: allowCancel,
            allowEdit: allowEdit);
        status.ClearDomainEvents();
        return status;
    }

    [Fact]
    public async Task SaveChanges_ThenReload_RoundTripsOrderStatusIdConversion()
    {
        var status = BuildStatus();

        await _context.OrderStatuses.AddAsync(status);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderStatuses.FirstAsync(s => s.Id == status.Id);

        loaded.Id.Value.ShouldBe(status.Id.Value);
    }

    [Fact]
    public async Task SaveChanges_ThenReload_PreservesAllScalarProperties()
    {
        var status = OrderStatus.Create(
            name: "AwaitingReview",
            displayName: "در انتظار بررسی",
            icon: "eye",
            color: "blue",
            sortOrder: 9,
            allowCancel: true,
            allowEdit: false);
        status.ClearDomainEvents();

        await _context.OrderStatuses.AddAsync(status);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderStatuses.FirstAsync(s => s.Id == status.Id);

        loaded.Name.ShouldBe("AwaitingReview");
        loaded.DisplayName.ShouldBe("در انتظار بررسی");
        loaded.Icon.ShouldBe("eye");
        loaded.Color.ShouldBe("blue");
        loaded.SortOrder.ShouldBe(9);
        loaded.AllowCancel.ShouldBeTrue();
        loaded.AllowEdit.ShouldBeFalse();
        loaded.IsActive.ShouldBeTrue();
        loaded.IsDefault.ShouldBeFalse();
        loaded.RowVersion.ShouldNotBeNull();
        loaded.RowVersion.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task SaveChanges_DuplicateName_ThrowsDbUpdateExceptionDueToUniqueIndex()
    {
        var first = BuildStatus(name: "DuplicateStatusName");
        await _context.OrderStatuses.AddAsync(first);
        await _context.SaveChangesAsync();

        var second = BuildStatus(name: "DuplicateStatusName");

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await _context.OrderStatuses.AddAsync(second);
            await _context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Query_ByName_ExercisesNameIndexAndReturnsMatchingRow()
    {
        var target = BuildStatus(name: "Confirmed");
        var other = BuildStatus(name: "Rejected");

        _context.OrderStatuses.AddRange(target, other);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderStatuses.FirstOrDefaultAsync(s => s.Name == "Confirmed");

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(target.Id);
    }

    [Fact]
    public async Task Query_BySortOrder_ExercisesSortOrderIndexAndReturnsOrdered()
    {
        var first = BuildStatus(sortOrder: 1);
        var second = BuildStatus(sortOrder: 2);
        var third = BuildStatus(sortOrder: 3);

        _context.OrderStatuses.AddRange(third, first, second);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderStatuses
            .Where(s => s.Id == first.Id || s.Id == second.Id || s.Id == third.Id)
            .OrderBy(s => s.SortOrder)
            .Select(s => s.SortOrder)
            .ToListAsync();

        loaded.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task Query_ByIsActive_ExercisesIsActiveIndex()
    {
        var active = BuildStatus();
        var inactive = BuildStatus();
        inactive.Deactivate();
        inactive.ClearDomainEvents();

        _context.OrderStatuses.AddRange(active, inactive);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var actives = await freshContext.OrderStatuses
            .Where(s => s.IsActive && (s.Id == active.Id || s.Id == inactive.Id))
            .ToListAsync();
        var inactives = await freshContext.OrderStatuses
            .Where(s => !s.IsActive && (s.Id == active.Id || s.Id == inactive.Id))
            .ToListAsync();

        actives.Count.ShouldBe(1);
        actives[0].Id.ShouldBe(active.Id);
        inactives.Count.ShouldBe(1);
        inactives[0].Id.ShouldBe(inactive.Id);
    }

    [Fact]
    public async Task Query_ByIsDefault_ExercisesIsDefaultIndex()
    {
        var defaultStatus = BuildStatus();
        defaultStatus.SetAsDefault();
        defaultStatus.ClearDomainEvents();
        var regular = BuildStatus();

        _context.OrderStatuses.AddRange(defaultStatus, regular);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderStatuses
            .FirstOrDefaultAsync(s => s.IsDefault && s.Id == defaultStatus.Id);

        loaded.ShouldNotBeNull();
        loaded!.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task RowVersion_ConfiguredAsConcurrencyToken_ChangesBetweenInsertAndUpdate()
    {
        var status = BuildStatus();

        await _context.OrderStatuses.AddAsync(status);
        await _context.SaveChangesAsync();
        var initialRowVersion = status.RowVersion.ToArray();

        await using var mutationContext = _fixture.CreateContext();
        var tracked = await mutationContext.OrderStatuses.FirstAsync(s => s.Id == status.Id);
        tracked.Update(
            displayName: "updated-display",
            icon: "new-icon",
            color: "green",
            sortOrder: 99,
            allowCancel: true,
            allowEdit: true);
        tracked.ClearDomainEvents();
        await mutationContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.OrderStatuses.FirstAsync(s => s.Id == status.Id);

        reloaded.RowVersion.SequenceEqual(initialRowVersion).ShouldBeFalse();
        reloaded.DisplayName.ShouldBe("updated-display");
        reloaded.SortOrder.ShouldBe(99);
    }

    [Fact]
    public async Task ConcurrencyConflict_WhenTwoContextsUpdateSameRowVersion_SecondSaveThrowsDbUpdateConcurrencyException()
    {
        var status = BuildStatus();
        await _context.OrderStatuses.AddAsync(status);
        await _context.SaveChangesAsync();

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var trackedByA = await contextA.OrderStatuses.FirstAsync(s => s.Id == status.Id);
        var trackedByB = await contextB.OrderStatuses.FirstAsync(s => s.Id == status.Id);

        trackedByA.Update("a-display", null, null, 1, false, false);
        trackedByA.ClearDomainEvents();
        await contextA.SaveChangesAsync();

        trackedByB.Update("b-display", null, null, 2, false, false);
        trackedByB.ClearDomainEvents();

        await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
            await contextB.SaveChangesAsync());
    }

    [Fact]
    public async Task DomainEvents_AreIgnoredByEfCore_AndNotPersistedAsColumn()
    {
        var status = OrderStatus.Create(
            name: $"WithEvents-{Guid.NewGuid():N}"[..20],
            displayName: "دارای رویداد",
            sortOrder: 0);
        status.DomainEvents.ShouldNotBeEmpty();

        await _context.OrderStatuses.AddAsync(status);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderStatuses.FirstAsync(s => s.Id == status.Id);

        loaded.DomainEvents.ShouldBeEmpty();
    }
}
