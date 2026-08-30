using Domain.Inventory.Aggregates;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using Infrastructure.Inventory.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Inventory.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WarehouseRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IWarehouseRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WarehouseRepository(_context);
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
    public async Task AddAsync_ValidWarehouse_PersistsAcrossContexts()
    {
        var warehouse = new WarehouseBuilder()
            .WithCode("WH-MAIN-01")
            .WithName("Main Warehouse")
            .WithCity("Tehran")
            .WithAddress("Some street")
            .WithPhone("02133445566")
            .WithPriority(1)
            .AsDefault()
            .Build();

        await _sut.AddAsync(warehouse);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new WarehouseRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(warehouse.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(warehouse.Id);
        loaded.Code.Value.ShouldBe("WH-MAIN-01");
        loaded.Name.ShouldBe("Main Warehouse");
        loaded.City.ShouldBe("Tehran");
        loaded.Priority.ShouldBe(1);
        loaded.IsDefault.ShouldBeTrue();
        loaded.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenWarehouseDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(WarehouseId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPersistedWarehouses()
    {
        var w1 = new WarehouseBuilder().WithCode("WH-A1").WithPriority(1).Build();
        var w2 = new WarehouseBuilder().WithCode("WH-A2").WithPriority(2).Build();

        await _sut.AddAsync(w1);
        await _sut.AddAsync(w2);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var results = await _sut.GetAllAsync();

        results.Count.ShouldBeGreaterThanOrEqualTo(2);
        results.ShouldContain(w => w.Id == w1.Id);
        results.ShouldContain(w => w.Id == w2.Id);
    }

    [Fact]
    public async Task GetDefaultAsync_WhenDefaultWarehouseExists_ReturnsIt()
    {
        var defaultWh = new WarehouseBuilder().WithCode("WH-DEF").AsDefault().Build();
        var otherWh = new WarehouseBuilder().WithCode("WH-OTH").Build();

        await _sut.AddAsync(defaultWh);
        await _sut.AddAsync(otherWh);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetDefaultAsync();

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(defaultWh.Id);
        loaded.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task GetDefaultAsync_WhenNoDefaultExists_ReturnsNull()
    {
        var w = new WarehouseBuilder().WithCode("WH-NONDEF").Build();

        await _sut.AddAsync(w);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetDefaultAsync();

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsByCodeAsync_MatchingCode_ReturnsTrue()
    {
        var w = new WarehouseBuilder().WithCode("WH-EXISTS").Build();

        await _sut.AddAsync(w);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByCodeAsync("WH-EXISTS");

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByCodeAsync_NonMatchingCode_ReturnsFalse()
    {
        var exists = await _sut.ExistsByCodeAsync("WH-NOT-THERE");

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByCodeAsync_WithExcludeId_ExcludesOwnEntry()
    {
        var w = new WarehouseBuilder().WithCode("WH-SELF").Build();

        await _sut.AddAsync(w);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByCodeAsync("WH-SELF", w.Id);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task Update_AfterDeactivate_PersistsIsActiveFalse()
    {
        var w = new WarehouseBuilder().WithCode("WH-DEACT").Build();

        await _sut.AddAsync(w);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(w.Id);
        loaded.ShouldNotBeNull();
        loaded!.Deactivate();
        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new WarehouseRepository(freshContext);
        var final = await freshRepo.GetByIdAsync(w.Id);

        final.ShouldNotBeNull();
        final!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Remove_ExistingWarehouse_DeletesFromDatabase()
    {
        var w = new WarehouseBuilder().WithCode("WH-REM").Build();

        await _sut.AddAsync(w);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var toRemove = await _sut.GetByIdAsync(w.Id);
        toRemove.ShouldNotBeNull();
        _sut.Remove(toRemove!);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        (await _sut.GetByIdAsync(w.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_DuplicateCode_ThrowsOnSaveDueToUniqueIndex()
    {
        var first = new WarehouseBuilder().WithCode("WH-DUP").Build();
        var second = new WarehouseBuilder().WithCode("WH-DUP").Build();

        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }
}

