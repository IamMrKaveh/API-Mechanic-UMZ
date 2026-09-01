using Domain.Discount.Entities;
using Domain.Discount.Enums;
using Domain.Discount.ValueObjects;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Discount.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class DiscountRestrictionConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;

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

    [Fact]
    public void Model_MappedToDiscountRestrictionsTable()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = _context.Model.FindEntityType(typeof(DiscountRestriction));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("DiscountRestrictions");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountRestriction));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(DiscountRestriction.Id));
    }

    [Fact]
    public void Model_DiscountCodeId_IsRequired()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountRestriction));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(DiscountRestriction.DiscountCodeId));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_RestrictionType_IsRequiredAndHasMaxLength50AndStringConversion()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountRestriction));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(DiscountRestriction.RestrictionType));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(50);
        property.GetValueConverter().ShouldNotBeNull();
        property.ClrType.ShouldBe(typeof(DiscountRestrictionType));
    }

    [Fact]
    public void Model_RestrictionValue_IsRequiredAndHasMaxLength500()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountRestriction));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(DiscountRestriction.RestrictionValue));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(500);
    }

    [Fact]
    public void Model_HasNavigationToDiscountCode()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountRestriction));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(DiscountRestriction.DiscountCode));
        navigation.ShouldNotBeNull();
    }

    [Fact]
    public async Task SaveChanges_WhenDiscountCodeDeleted_CascadesDeleteToRestrictions()
    {
        var discount = new DiscountCodeBuilder().WithCode("CASCADE-RESTR").Build();
        discount.ClearDomainEvents();

        _context.DiscountCodes.Add(discount);
        await _context.SaveChangesAsync();

        var discountId = discount.Id;
        _context.DiscountCodes.Remove(discount);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var remaining = await _context.Set<DiscountRestriction>()
            .CountAsync(r => r.DiscountCodeId == discountId);

        remaining.ShouldBe(0);
    }
}
