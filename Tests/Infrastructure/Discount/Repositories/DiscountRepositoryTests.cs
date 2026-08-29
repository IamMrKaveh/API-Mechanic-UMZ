using Domain.Discount.Enums;
using Domain.Discount.ValueObjects;
using Infrastructure.Discount.Repositories;
using Infrastructure.Persistence.Context;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Discount.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class DiscountRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private DiscountRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new DiscountRepository(_context);
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
    public async Task AddAsync_ThenGetByIdAsync_ReturnsPersistedDiscountCode()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("SAVE10")
            .WithValue(DiscountValue.Percentage(10m))
            .WithUsageLimit(50)
            .Build();

        await _sut.AddAsync(discount, CancellationToken.None);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(discount.Id, CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(discount.Id);
        loaded.Code.ShouldBe("SAVE10");
        loaded.Value.Amount.ShouldBe(10m);
        loaded.Value.Type.ShouldBe(DiscountType.Percentage);
        loaded.UsageLimit.ShouldBe(50);
        loaded.UsageCount.ShouldBe(0);
        loaded.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(DiscountCodeId.NewId(), CancellationToken.None);

        loaded.ShouldBeNull();
    }

    [Theory]
    [InlineData("save10")]
    [InlineData("  Save10  ")]
    [InlineData("SAVE10")]
    public async Task GetByCodeAsync_WithArbitraryCasingOrPadding_ReturnsPersistedDiscount(string input)
    {
        var discount = new DiscountCodeBuilder().WithCode("SAVE10").Build();

        await _sut.AddAsync(discount, CancellationToken.None);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByCodeAsync(input, CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(discount.Id);
        loaded.Code.ShouldBe("SAVE10");
    }

    [Fact]
    public async Task GetByCodeAsync_WhenCodeDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByCodeAsync("DOES-NOT-EXIST", CancellationToken.None);

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_PreservesMaximumDiscountAmountThroughMoneyConverter()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("CAP")
            .WithValue(DiscountValue.Percentage(50m))
            .WithMaximumDiscountAmount(200m, "IRT")
            .Build();

        await _sut.AddAsync(discount, CancellationToken.None);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(discount.Id, CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded.MaximumDiscountAmount.ShouldNotBeNull();
        loaded.MaximumDiscountAmount!.Amount.ShouldBe(200m);
        loaded.MaximumDiscountAmount.Currency.ShouldBe("IRT");
    }

    [Fact]
    public async Task AddAsync_WithNullMaximumDiscountAmount_PersistsAsNull()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("NOCAP")
            .WithMaximumDiscountAmount(null)
            .Build();

        await _sut.AddAsync(discount, CancellationToken.None);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(discount.Id, CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded.MaximumDiscountAmount.ShouldBeNull();
    }

    [Fact]
    public async Task Update_ModifiesPersistedDiscount_ChangesAreReflectedOnReload()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("UPD")
            .WithValue(DiscountValue.Percentage(10m))
            .WithUsageLimit(10)
            .Build();

        await _sut.AddAsync(discount, CancellationToken.None);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var toModify = await _sut.GetByIdAsync(discount.Id, CancellationToken.None);
        toModify.ShouldNotBeNull();
        toModify!.Update(
            DiscountValue.Fixed(500m),
            Money.Create(1000m, "IRT"),
            25,
            null,
            new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _sut.Update(toModify);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(discount.Id, CancellationToken.None);

        reloaded.ShouldNotBeNull();
        reloaded!.Value.Type.ShouldBe(DiscountType.FixedAmount);
        reloaded.Value.Amount.ShouldBe(500m);
        reloaded.UsageLimit.ShouldBe(25);
        reloaded.MaximumDiscountAmount.ShouldNotBeNull();
        reloaded.MaximumDiscountAmount!.Amount.ShouldBe(1000m);
        reloaded.ExpiresAt!.Value.ShouldBe(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetByIdWithUsagesAsync_WhenNoUsages_ReturnsDiscountWithEmptyUsagesCollection()
    {
        var discount = new DiscountCodeBuilder().WithCode("EMPTYU").Build();

        await _sut.AddAsync(discount, CancellationToken.None);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdWithUsagesAsync(discount.Id, CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(discount.Id);
        loaded.Usages.ShouldBeEmpty();
        loaded.Restrictions.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddAsync_PersistsFreeShippingValueThroughOwnedTypeMapping()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("SHIP")
            .WithValue(DiscountValue.FreeShipping())
            .Build();

        await _sut.AddAsync(discount, CancellationToken.None);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(discount.Id, CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded!.Value.Type.ShouldBe(DiscountType.FreeShipping);
        loaded.Value.Amount.ShouldBe(0m);
    }
}
