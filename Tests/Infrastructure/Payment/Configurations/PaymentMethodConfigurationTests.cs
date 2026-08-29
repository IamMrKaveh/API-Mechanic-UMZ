using Domain.Payment.Aggregates;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Payment.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class PaymentMethodConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private async Task<PaymentMethod> PersistAsync(PaymentMethod method)
    {
        method.ClearDomainEvents();
        await _context.PaymentMethods.AddAsync(method);
        await _context.SaveChangesAsync();
        return method;
    }

    [Fact]
    public async Task Save_ThenReload_PreservesAllPropertiesAndOwnedFee()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Full Roundtrip")
            .WithCode("cfg-full-roundtrip")
            .WithFee(1200m, 3.75m)
            .WithDescription("A configured description.")
            .WithIconUrl("assets/icon.svg")
            .WithSortOrder(42)
            .Build();
        await PersistAsync(method);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.PaymentMethods.FirstAsync(p => p.Id == method.Id);

        loaded.Name.Value.ShouldBe("Full Roundtrip");
        loaded.Code.Value.ShouldBe("cfg-full-roundtrip");
        loaded.Description.ShouldBe("A configured description.");
        loaded.IconUrl.ShouldBe("assets/icon.svg");
        loaded.SortOrder.ShouldBe(42);
        loaded.IsActive.ShouldBeTrue();
        loaded.IsDeleted.ShouldBeFalse();
        loaded.Fee.Amount.Amount.ShouldBe(1200m);
        loaded.Fee.Percentage.ShouldBe(3.75m);
    }

    [Fact]
    public async Task QueryFilter_WithSoftDeletedMethod_ExcludesFromDefaultQuery()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Soft Deleted Method")
            .WithCode("cfg-soft-deleted")
            .Build();
        await PersistAsync(method);

        method.RequestDeletion();
        method.ClearDomainEvents();
        _context.PaymentMethods.Update(method);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var visible = await freshContext.PaymentMethods
            .FirstOrDefaultAsync(p => p.Id == method.Id);

        visible.ShouldBeNull();
    }

    [Fact]
    public async Task IgnoreQueryFilters_WithSoftDeletedMethod_ReturnsSoftDeletedRow()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Soft Deleted Visible")
            .WithCode("cfg-soft-deleted-visible")
            .Build();
        await PersistAsync(method);

        method.RequestDeletion();
        method.ClearDomainEvents();
        _context.PaymentMethods.Update(method);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.PaymentMethods
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == method.Id);

        loaded.ShouldNotBeNull();
        loaded!.IsDeleted.ShouldBeTrue();
        loaded.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChanges_DuplicateCode_ThrowsDbUpdateException()
    {
        var first = new PaymentMethodBuilder()
            .WithName("Duplicate Code A")
            .WithCode("cfg-duplicate-code")
            .Build();
        await PersistAsync(first);

        var second = new PaymentMethodBuilder()
            .WithName("Duplicate Code B")
            .WithCode("cfg-duplicate-code")
            .Build();
        second.ClearDomainEvents();

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await _context.PaymentMethods.AddAsync(second);
            await _context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task SaveChanges_DuplicateName_ThrowsDbUpdateException()
    {
        var first = new PaymentMethodBuilder()
            .WithName("Duplicate Name Value")
            .WithCode("cfg-duplicate-name-a")
            .Build();
        await PersistAsync(first);

        var second = new PaymentMethodBuilder()
            .WithName("Duplicate Name Value")
            .WithCode("cfg-duplicate-name-b")
            .Build();
        second.ClearDomainEvents();

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await _context.PaymentMethods.AddAsync(second);
            await _context.SaveChangesAsync();
        });
    }
}
