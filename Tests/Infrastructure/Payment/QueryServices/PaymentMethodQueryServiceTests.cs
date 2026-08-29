using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;
using Infrastructure.Payment.QueryServices;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Payment.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class PaymentMethodQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private PaymentMethodQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new PaymentMethodQueryService(_context);
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
    public async Task GetByIdAsync_ExistingMethod_ReturnsMappedDto()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Query Get By Id")
            .WithCode("q-get-by-id")
            .WithFee(750m, 1.5m)
            .WithDescription("Query description.")
            .WithIconUrl("q/icon.png")
            .WithSortOrder(5)
            .Build();
        await PersistAsync(method);

        var dto = await _sut.GetByIdAsync(method.Id, CancellationToken.None);

        dto.ShouldNotBeNull();
        dto!.Id.ShouldBe(method.Id.Value);
        dto.Name.ShouldBe("Query Get By Id");
        dto.Code.ShouldBe("q-get-by-id");
        dto.Description.ShouldBe("Query description.");
        dto.IconUrl.ShouldBe("q/icon.png");
        dto.FeeAmount.ShouldBe(750m);
        dto.FeePercentage.ShouldBe(1.5m);
        dto.IsActive.ShouldBeTrue();
        dto.SortOrder.ShouldBe(5);
    }

    [Fact]
    public async Task GetByIdAsync_SoftDeletedMethod_ReturnsDtoBecauseFiltersIgnored()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Query Soft Deleted")
            .WithCode("q-soft-deleted")
            .Build();
        await PersistAsync(method);

        method.RequestDeletion();
        method.ClearDomainEvents();
        _context.PaymentMethods.Update(method);
        await _context.SaveChangesAsync();

        var dto = await _sut.GetByIdAsync(method.Id, CancellationToken.None);

        dto.ShouldNotBeNull();
        dto!.Id.ShouldBe(method.Id.Value);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentMethod_ReturnsNull()
    {
        var dto = await _sut.GetByIdAsync(PaymentMethodId.NewId(), CancellationToken.None);

        dto.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllAsync_DefaultParameters_ReturnsActiveOnlyOrderedBySortAndName()
    {
        var active1 = new PaymentMethodBuilder().WithName("Alpha Active").WithCode("q-all-alpha").WithSortOrder(10).Build();
        var active2 = new PaymentMethodBuilder().WithName("Bravo Active").WithCode("q-all-bravo").WithSortOrder(20).Build();
        var inactive = new PaymentMethodBuilder().WithName("Charlie Inactive").WithCode("q-all-charlie").WithSortOrder(30).Build();
        inactive.Deactivate();

        await PersistAsync(active1);
        await PersistAsync(active2);
        await PersistAsync(inactive);

        var result = await _sut.GetAllAsync(includeInactive: false, includeDeleted: false, CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(active1.Id.Value);
        result[1].Id.ShouldBe(active2.Id.Value);
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeInactive_IncludesInactiveButNotDeleted()
    {
        var active = new PaymentMethodBuilder().WithName("Active Incl").WithCode("q-all-active-incl").WithSortOrder(10).Build();
        var inactive = new PaymentMethodBuilder().WithName("Inactive Incl").WithCode("q-all-inactive-incl").WithSortOrder(20).Build();
        inactive.Deactivate();
        var deleted = new PaymentMethodBuilder().WithName("Deleted Incl").WithCode("q-all-deleted-incl").WithSortOrder(30).Build();
        deleted.RequestDeletion();

        await PersistAsync(active);
        await PersistAsync(inactive);
        await PersistAsync(deleted);

        var result = await _sut.GetAllAsync(includeInactive: true, includeDeleted: false, CancellationToken.None);

        result.Count.ShouldBe(2);
        result.Any(r => r.Id == active.Id.Value).ShouldBeTrue();
        result.Any(r => r.Id == inactive.Id.Value).ShouldBeTrue();
        result.Any(r => r.Id == deleted.Id.Value).ShouldBeFalse();
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeDeleted_IncludesSoftDeletedMethods()
    {
        var active = new PaymentMethodBuilder().WithName("Active Del Incl").WithCode("q-all-active-del").WithSortOrder(10).Build();
        var deleted = new PaymentMethodBuilder().WithName("Deleted Del Incl").WithCode("q-all-deleted-del").WithSortOrder(20).Build();
        deleted.RequestDeletion();

        await PersistAsync(active);
        await PersistAsync(deleted);

        var result = await _sut.GetAllAsync(includeInactive: false, includeDeleted: true, CancellationToken.None);

        result.Count.ShouldBe(2);
        result.Any(r => r.IsDeleted && r.Id == deleted.Id.Value).ShouldBeTrue();
    }

    [Fact]
    public async Task GetActiveAsync_ComputesFeePerOrderAmount()
    {
        var fixedOnly = new PaymentMethodBuilder()
            .WithName("Fixed Fee Method")
            .WithCode("q-active-fixed")
            .WithFee(1000m, 0m)
            .WithSortOrder(10)
            .Build();

        var percentOnly = new PaymentMethodBuilder()
            .WithName("Percent Fee Method")
            .WithCode("q-active-percent")
            .WithFee(0m, 2m)
            .WithSortOrder(20)
            .Build();

        var inactive = new PaymentMethodBuilder()
            .WithName("Inactive Skipped")
            .WithCode("q-active-inactive")
            .WithSortOrder(30)
            .Build();
        inactive.Deactivate();

        await PersistAsync(fixedOnly);
        await PersistAsync(percentOnly);
        await PersistAsync(inactive);

        var result = await _sut.GetActiveAsync(orderAmount: 50000m, CancellationToken.None);

        result.Count.ShouldBe(2);
        var fixedDto = result.Single(r => r.Id == fixedOnly.Id.Value);
        var percentDto = result.Single(r => r.Id == percentOnly.Id.Value);
        fixedDto.Fee.ShouldBe(1000m);
        percentDto.Fee.ShouldBe(1000m);
    }
}
