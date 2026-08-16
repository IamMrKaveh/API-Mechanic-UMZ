using Domain.Order.Entities; using Domain.Order.ValueObjects; using Infrastructure.Order.Repositories; using Infrastructure.Persistence.Context; using Microsoft.EntityFrameworkCore; using Tests.TestInfrastructure.Database;

namespace Tests.Integration.Order;

[Collection(nameof(DatabaseCollection))] [Trait("Category", "Integration")] public class OrderStatusRepositoryIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime { private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private OrderStatusRepository _sut = null!;

public Task InitializeAsync()
{
    Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");
    _context = _fixture.CreateContext();
    _sut = new OrderStatusRepository(_context);
    return Task.CompletedTask;
}

public async Task DisposeAsync()
{
    if (!_fixture.IsDockerAvailable) return;
    await _context.DisposeAsync();
    await _fixture.ResetAsync();
}

[SkippableFact]
public async Task GetByIdAsync_ReturnsStatus_WhenExists()
{
    var status = OrderStatus.Create("Created", "ایجاد شده", "icon", "#000", 0, true, true);
    _context.OrderStatuses.Add(status);
    await _context.SaveChangesAsync();

    var result = await _sut.GetByIdAsync(status.Id, CancellationToken.None);

    result.ShouldNotBeNull();
    result!.Id.ShouldBe(status.Id);
    result.Name.ShouldBe("Created");
}

[SkippableFact]
public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
{
    var result = await _sut.GetByIdAsync(OrderStatusId.NewId(), CancellationToken.None);
    result.ShouldBeNull();
}

[SkippableFact]
public async Task GetDefaultAsync_ReturnsDefaultStatus()
{
    var normal = OrderStatus.Create("Reserved", "رزرو", null, null, 1, true, false);
    var def = OrderStatus.Create("Created", "ایجاد شده", null, null, 0, true, true);
    def.SetAsDefault();
    _context.OrderStatuses.Add(normal);
    _context.OrderStatuses.Add(def);
    await _context.SaveChangesAsync();

    var result = await _sut.GetDefaultAsync(CancellationToken.None);

    result.ShouldNotBeNull();
    result!.Name.ShouldBe("Created");
    result.IsDefault.ShouldBeTrue();
}

[SkippableFact]
public async Task GetDefaultAsync_ReturnsNull_WhenNoDefaultExists()
{
    var s = OrderStatus.Create("Reserved", "رزرو", null, null, 1, true, false);
    _context.OrderStatuses.Add(s);
    await _context.SaveChangesAsync();

    var result = await _sut.GetDefaultAsync(CancellationToken.None);
    result.ShouldBeNull();
}

[SkippableFact]
public async Task ExistsByNameAsync_ReturnsTrue_ForExistingName_TrimmedAndCaseSensitive()
{
    var s = OrderStatus.Create("Reserved", "رزرو", null, null, 1, true, false);
    _context.OrderStatuses.Add(s);
    await _context.SaveChangesAsync();

    (await _sut.ExistsByNameAsync("  Reserved  ", null, CancellationToken.None)).ShouldBeTrue();
}

[SkippableFact]
public async Task ExistsByNameAsync_ReturnsFalse_ForNonExistingName()
{
    (await _sut.ExistsByNameAsync("Nonexistent", null, CancellationToken.None)).ShouldBeFalse();
}

[SkippableFact]
public async Task ExistsByNameAsync_ExcludesGivenId()
{
    var s = OrderStatus.Create("Reserved", "رزرو", null, null, 1, true, false);
    _context.OrderStatuses.Add(s);
    await _context.SaveChangesAsync();

    (await _sut.ExistsByNameAsync("Reserved", s.Id, CancellationToken.None)).ShouldBeFalse();
}

[SkippableFact]
public async Task AddAsync_PersistsStatus_AfterSave()
{
    var s = OrderStatus.Create("Pending", "در انتظار", null, null, 2, true, false);
    await _sut.AddAsync(s, CancellationToken.None);
    await _context.SaveChangesAsync();

    await using var verify = _fixture.CreateContext();
    var loaded = await verify.OrderStatuses.FirstOrDefaultAsync(x => x.Id == s.Id);
    loaded.ShouldNotBeNull();
    loaded!.Name.ShouldBe("Pending");
}

[SkippableFact]
public async Task Update_PersistsMutations()
{
    var s = OrderStatus.Create("Pending", "در انتظار", null, null, 2, true, false);
    _context.OrderStatuses.Add(s);
    await _context.SaveChangesAsync();

    s.Update("در انتظار پرداخت", "clock", "#FFF", 5, true, true);
    _sut.Update(s);
    await _context.SaveChangesAsync();

    await using var verify = _fixture.CreateContext();
    var loaded = await verify.OrderStatuses.FirstAsync(x => x.Id == s.Id);
    loaded.DisplayName.ShouldBe("در انتظار پرداخت");
    loaded.Icon.ShouldBe("clock");
    loaded.Color.ShouldBe("#FFF");
    loaded.SortOrder.ShouldBe(5);
    loaded.AllowCancel.ShouldBeTrue();
    loaded.AllowEdit.ShouldBeTrue();
}

[SkippableFact]
public async Task Remove_DeletesStatus()
{
    var s = OrderStatus.Create("Failed", "ناموفق", null, null, 3, false, false);
    _context.OrderStatuses.Add(s);
    await _context.SaveChangesAsync();

    _sut.Remove(s);
    await _context.SaveChangesAsync();

    await using var verify = _fixture.CreateContext();
    (await verify.OrderStatuses.AnyAsync(x => x.Id == s.Id)).ShouldBeFalse();
}

[SkippableFact]
public async Task SetOriginalRowVersion_WithStaleValue_ThrowsConcurrencyOnSave()
{
    var s = OrderStatus.Create("Paid", "پرداخت شده", null, null, 4, false, false);
    _context.OrderStatuses.Add(s);
    await _context.SaveChangesAsync();

    s.Update("پرداخت شده!", null, null, 4, false, false);
    var stale = Guid.NewGuid().ToByteArray();
    _sut.Update(s, stale);

    await Should.ThrowAsync<DbUpdateConcurrencyException>(async () => await _context.SaveChangesAsync());
}
}