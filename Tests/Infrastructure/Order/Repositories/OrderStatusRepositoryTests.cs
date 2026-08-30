using Domain.Order.Entities;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Infrastructure.Order.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Order.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrderStatusRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IOrderStatusRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new OrderStatusRepository(_context);
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
    public async Task AddAsync_ValidStatus_PersistsAcrossContexts()
    {
        var status = OrderStatus.Create(
            name: "Awaiting-Review",
            displayName: "در انتظار بررسی",
            icon: "clock",
            color: "gray",
            sortOrder: 5,
            allowCancel: true,
            allowEdit: false);
        status.ClearDomainEvents();

        await _sut.AddAsync(status);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new OrderStatusRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(status.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(status.Id);
        loaded.Name.ShouldBe("Awaiting-Review");
        loaded.DisplayName.ShouldBe("در انتظار بررسی");
        loaded.Icon.ShouldBe("clock");
        loaded.Color.ShouldBe("gray");
        loaded.SortOrder.ShouldBe(5);
        loaded.AllowCancel.ShouldBeTrue();
        loaded.AllowEdit.ShouldBeFalse();
        loaded.IsActive.ShouldBeTrue();
        loaded.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WhenStatusDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(OrderStatusId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetDefaultAsync_WhenDefaultExists_ReturnsIt()
    {
        var defaultStatus = OrderStatus.Create("Placed", "ثبت شده", sortOrder: 0);
        defaultStatus.SetAsDefault();
        defaultStatus.ClearDomainEvents();

        var other = OrderStatus.Create("Processing", "در حال پردازش", sortOrder: 1);
        other.ClearDomainEvents();

        await _sut.AddAsync(defaultStatus);
        await _sut.AddAsync(other);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetDefaultAsync();

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(defaultStatus.Id);
        loaded.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task GetDefaultAsync_WhenNoDefaultExists_ReturnsNull()
    {
        var status = OrderStatus.Create("NoDefault", "بدون پیش‌فرض");
        status.ClearDomainEvents();

        await _sut.AddAsync(status);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetDefaultAsync();

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsByNameAsync_MatchingName_ReturnsTrue()
    {
        var status = OrderStatus.Create("UniqueName-1", "نام یکتا");
        status.ClearDomainEvents();

        await _sut.AddAsync(status);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByNameAsync("UniqueName-1");

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_NonMatchingName_ReturnsFalse()
    {
        var exists = await _sut.ExistsByNameAsync("Does-Not-Exist");

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_WithExcludeId_ExcludesOwnEntry()
    {
        var status = OrderStatus.Create("UniqueName-2", "نام یکتا ۲");
        status.ClearDomainEvents();

        await _sut.AddAsync(status);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByNameAsync("UniqueName-2", status.Id);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task IsInUseAsync_WhenNoOrderReferencesStatus_ReturnsFalse()
    {
        var status = OrderStatus.Create("UnusedStatus", "استفاده نشده");
        status.ClearDomainEvents();

        await _sut.AddAsync(status);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var inUse = await _sut.IsInUseAsync(status.Id);

        inUse.ShouldBeFalse();
    }

    [Fact]
    public async Task IsInUseAsync_WhenAnOrderHasMatchingStatusValue_ReturnsTrue()
    {
        var status = OrderStatus.Create("Created", "ایجاد شده");
        status.ClearDomainEvents();

        await _sut.AddAsync(status);
        await _context.SaveChangesAsync();

        var user = new UserBuilder().Build();
        user.ClearDomainEvents();
        await _context.Users.AddAsync(user);

        var order = new OrderBuilder().WithUserId(user.Id).Build();
        order.ClearDomainEvents();
        await _context.Orders.AddAsync(order);

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var inUse = await _sut.IsInUseAsync(status.Id);

        inUse.ShouldBeTrue();
    }

    [Fact]
    public async Task Update_AfterUpdate_PersistsNewValues()
    {
        var status = OrderStatus.Create("Editable", "قابل ویرایش", sortOrder: 1);
        status.ClearDomainEvents();

        await _sut.AddAsync(status);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(status.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.Update(
            displayName: "به‌روز شده",
            icon: "star",
            color: "blue",
            sortOrder: 9,
            allowCancel: true,
            allowEdit: true);
        reloaded.ClearDomainEvents();
        _sut.Update(reloaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new OrderStatusRepository(freshContext);
        var final = await freshRepo.GetByIdAsync(status.Id);

        final.ShouldNotBeNull();
        final!.DisplayName.ShouldBe("به‌روز شده");
        final.Icon.ShouldBe("star");
        final.Color.ShouldBe("blue");
        final.SortOrder.ShouldBe(9);
        final.AllowCancel.ShouldBeTrue();
        final.AllowEdit.ShouldBeTrue();
    }

    [Fact]
    public async Task Remove_ExistingStatus_DeletesFromDatabase()
    {
        var status = OrderStatus.Create("Removable", "قابل حذف");
        status.ClearDomainEvents();

        await _sut.AddAsync(status);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var toRemove = await _sut.GetByIdAsync(status.Id);
        toRemove.ShouldNotBeNull();
        _sut.Remove(toRemove!);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        (await _sut.GetByIdAsync(status.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_DuplicateName_ThrowsOnSaveDueToUniqueIndex()
    {
        var first = OrderStatus.Create("DupName", "تکراری");
        var second = OrderStatus.Create("DupName", "تکراری");
        first.ClearDomainEvents();
        second.ClearDomainEvents();

        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }
}

