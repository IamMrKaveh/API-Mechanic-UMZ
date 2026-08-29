using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;
using Infrastructure.Payment.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Payment.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class PaymentMethodRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private PaymentMethodRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new PaymentMethodRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<PaymentMethod> PersistAsync(PaymentMethod method)
    {
        method.ClearDomainEvents();
        await _context.PaymentMethods.AddAsync(method);
        await _context.SaveChangesAsync();
        return method;
    }

    [Fact]
    public async Task GetByIdAsync_ExistingMethod_ReturnsPaymentMethod()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Cash on Delivery")
            .WithCode("get-by-id-existing")
            .WithSortOrder(1)
            .Build();
        await PersistAsync(method);

        var loaded = await _sut.GetByIdAsync(method.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(method.Id);
        loaded.Name.Value.ShouldBe("Cash on Delivery");
        loaded.Code.Value.ShouldBe("get-by-id-existing");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentMethod_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(PaymentMethodId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetByCodeAsync_ExistingCode_ReturnsPaymentMethod()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Wallet Payment")
            .WithCode("wallet-lookup")
            .Build();
        await PersistAsync(method);

        var loaded = await _sut.GetByCodeAsync(PaymentMethodCode.Create("wallet-lookup"));

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(method.Id);
    }

    [Fact]
    public async Task GetByCodeAsync_UnknownCode_ReturnsNull()
    {
        var loaded = await _sut.GetByCodeAsync(PaymentMethodCode.Create("no-such-code"));

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsByNameAsync_MatchingName_ReturnsTrue()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Duplicate Name Check")
            .WithCode("exists-by-name-1")
            .Build();
        await PersistAsync(method);

        var exists = await _sut.ExistsByNameAsync(PaymentMethodName.Create("Duplicate Name Check"));

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_WithExcludeId_ExcludesOwnEntry()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Self Exclude Name")
            .WithCode("exists-by-name-self")
            .Build();
        await PersistAsync(method);

        var exists = await _sut.ExistsByNameAsync(method.Name, method.Id);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_UnknownName_ReturnsFalse()
    {
        var exists = await _sut.ExistsByNameAsync(PaymentMethodName.Create("Nonexistent Method"));

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByCodeAsync_MatchingCode_ReturnsTrue()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Exists Code Check")
            .WithCode("exists-by-code-1")
            .Build();
        await PersistAsync(method);

        var exists = await _sut.ExistsByCodeAsync(PaymentMethodCode.Create("exists-by-code-1"));

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByCodeAsync_WithExcludeId_ExcludesOwnEntry()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Self Exclude Code")
            .WithCode("exists-by-code-self")
            .Build();
        await PersistAsync(method);

        var exists = await _sut.ExistsByCodeAsync(method.Code, method.Id);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByCodeAsync_UnknownCode_ReturnsFalse()
    {
        var exists = await _sut.ExistsByCodeAsync(PaymentMethodCode.Create("never-inserted"));

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAllAsync_DefaultParameters_ReturnsOnlyActiveNonDeleted()
    {
        var active = new PaymentMethodBuilder()
            .WithName("Getall Active")
            .WithCode("getall-active")
            .WithSortOrder(10)
            .Build();

        var inactive = new PaymentMethodBuilder()
            .WithName("Getall Inactive")
            .WithCode("getall-inactive")
            .WithSortOrder(20)
            .Build();
        inactive.Deactivate();

        var deleted = new PaymentMethodBuilder()
            .WithName("Getall Deleted")
            .WithCode("getall-deleted")
            .WithSortOrder(30)
            .Build();
        deleted.RequestDeletion();

        await PersistAsync(active);
        await PersistAsync(inactive);
        await PersistAsync(deleted);

        var result = await _sut.GetAllAsync();

        result.Count.ShouldBe(1);
        result.Single().Id.ShouldBe(active.Id);
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeInactive_ReturnsActiveAndInactiveNonDeleted()
    {
        var active = new PaymentMethodBuilder()
            .WithName("Getall Active Incl")
            .WithCode("getall-active-incl")
            .WithSortOrder(10)
            .Build();

        var inactive = new PaymentMethodBuilder()
            .WithName("Getall Inactive Incl")
            .WithCode("getall-inactive-incl")
            .WithSortOrder(20)
            .Build();
        inactive.Deactivate();

        var deleted = new PaymentMethodBuilder()
            .WithName("Getall Deleted Incl")
            .WithCode("getall-deleted-incl")
            .WithSortOrder(30)
            .Build();
        deleted.RequestDeletion();

        await PersistAsync(active);
        await PersistAsync(inactive);
        await PersistAsync(deleted);

        var result = await _sut.GetAllAsync(includeInactive: true);

        result.Count.ShouldBe(2);
        result.Any(m => m.Id == active.Id).ShouldBeTrue();
        result.Any(m => m.Id == inactive.Id).ShouldBeTrue();
        result.Any(m => m.Id == deleted.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeDeleted_ReturnsAllIncludingSoftDeleted()
    {
        var active = new PaymentMethodBuilder()
            .WithName("Getall Active Del")
            .WithCode("getall-active-del")
            .WithSortOrder(10)
            .Build();

        var deleted = new PaymentMethodBuilder()
            .WithName("Getall Deleted Del")
            .WithCode("getall-deleted-del")
            .WithSortOrder(20)
            .Build();
        deleted.RequestDeletion();

        await PersistAsync(active);
        await PersistAsync(deleted);

        var result = await _sut.GetAllAsync(includeDeleted: true);

        result.Count.ShouldBe(2);
        result.Any(m => m.Id == active.Id).ShouldBeTrue();
        result.Any(m => m.Id == deleted.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllAsync_MultipleMethods_OrdersBySortOrderThenName()
    {
        var third = new PaymentMethodBuilder()
            .WithName("Charlie Method")
            .WithCode("order-charlie")
            .WithSortOrder(30)
            .Build();

        var first = new PaymentMethodBuilder()
            .WithName("Alpha Method")
            .WithCode("order-alpha")
            .WithSortOrder(10)
            .Build();

        var secondA = new PaymentMethodBuilder()
            .WithName("Alpha Same Sort")
            .WithCode("order-alpha-same")
            .WithSortOrder(20)
            .Build();

        var secondB = new PaymentMethodBuilder()
            .WithName("Bravo Same Sort")
            .WithCode("order-bravo-same")
            .WithSortOrder(20)
            .Build();

        await PersistAsync(third);
        await PersistAsync(secondB);
        await PersistAsync(secondA);
        await PersistAsync(first);

        var result = (await _sut.GetAllAsync()).ToList();

        result.Count.ShouldBe(4);
        result[0].Id.ShouldBe(first.Id);
        result[1].Id.ShouldBe(secondA.Id);
        result[2].Id.ShouldBe(secondB.Id);
        result[3].Id.ShouldBe(third.Id);
    }

    [Fact]
    public async Task AddAsync_ThenSave_PersistsMethodAcrossContexts()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Persist Roundtrip")
            .WithCode("persist-roundtrip")
            .WithFee(500m, 2.5m)
            .WithDescription("A description")
            .WithIconUrl("payments/icon.png")
            .WithSortOrder(15)
            .Build();
        method.ClearDomainEvents();

        await _sut.AddAsync(method);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new PaymentMethodRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(method.Id);

        loaded.ShouldNotBeNull();
        loaded!.Name.Value.ShouldBe("Persist Roundtrip");
        loaded.Code.Value.ShouldBe("persist-roundtrip");
        loaded.Description.ShouldBe("A description");
        loaded.IconUrl.ShouldBe("payments/icon.png");
        loaded.SortOrder.ShouldBe(15);
        loaded.IsActive.ShouldBeTrue();
        loaded.Fee.Amount.Amount.ShouldBe(500m);
        loaded.Fee.Percentage.ShouldBe(2.5m);
    }

    [Fact]
    public async Task Update_Deactivate_PersistsIsActiveFalse()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Update Deactivate")
            .WithCode("update-deactivate")
            .Build();
        await PersistAsync(method);

        method.Deactivate();
        method.ClearDomainEvents();
        _sut.Update(method);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new PaymentMethodRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(method.Id);

        loaded.ShouldNotBeNull();
        loaded!.IsActive.ShouldBeFalse();
    }
}
