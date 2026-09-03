using global::Domain.Shipping.Aggregates;
using Tests.TestInfrastructure.Base;
using Shippings = global::Domain.Shipping.Aggregates.Shipping;

namespace Tests.Infrastructure.Shipping.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ShippingConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<Shippings> PersistAsync(Shippings shipping, CancellationToken ct = default)
    {
        shipping.ClearDomainEvents();
        Context.Shippings.Add(shipping);
        await Context.SaveChangesAsync(ct);
        return shipping;
    }

    [Fact]
    public async Task SaveChanges_PersistsShippingAndRoundTripsAllScalarAndOwnedProperties()
    {
        var shipping = new ShippingBuilder()
            .WithName($"Roundtrip Ship {Guid.NewGuid():N}"[..24])
            .WithBaseCost(150_000m, "IRT")
            .WithDescription("Fast nationwide delivery.")
            .WithEstimatedDeliveryTime("2-4 days")
            .WithDeliveryDays(2, 4)
            .Build();
        await PersistAsync(shipping);
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Shippings.FirstOrDefaultAsync(s => s.Id == shipping.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(shipping.Id);
        reloaded.Name.Value.ShouldBe(shipping.Name.Value);
        reloaded.BaseCost.Amount.ShouldBe(150_000m);
        reloaded.BaseCost.Currency.ShouldBe("IRT");
        reloaded.Description.ShouldBe("Fast nationwide delivery.");
        reloaded.EstimatedDeliveryTime.ShouldBe("2-4 days");
        reloaded.DeliveryTime.MinDays.ShouldBe(2);
        reloaded.DeliveryTime.MaxDays.ShouldBe(4);
        reloaded.IsActive.ShouldBeTrue();
        reloaded.IsDefault.ShouldBeFalse();
        reloaded.CreatedAt.ShouldNotBe(default);
        reloaded.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_DuplicateName_ThrowsDbUpdateException()
    {
        var name = $"Dup Ship {Guid.NewGuid():N}"[..20];
        await PersistAsync(new ShippingBuilder().WithName(name).Build());

        var duplicate = new ShippingBuilder().WithName(name).Build();
        duplicate.ClearDomainEvents();

        Context.Shippings.Add(duplicate);

        await Should.ThrowAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_SetAsDefaultAndUnsetDefault_PersistFlags()
    {
        var shipping = new ShippingBuilder()
            .WithName($"Default Ship {Guid.NewGuid():N}"[..22])
            .Build();
        await PersistAsync(shipping);

        shipping.SetAsDefault();
        shipping.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Shippings.FirstAsync(s => s.Id == shipping.Id);
        reloaded.IsDefault.ShouldBeTrue();

        reloaded.UnsetDefault();
        reloaded.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var afterUnset = await Context.Shippings.FirstAsync(s => s.Id == shipping.Id);
        afterUnset.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChanges_RequestDeletion_DeactivatesShipping()
    {
        var shipping = new ShippingBuilder()
            .WithName($"Delete Ship {Guid.NewGuid():N}"[..21])
            .Build();
        await PersistAsync(shipping);

        shipping.RequestDeletion();
        shipping.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Shippings.FirstAsync(s => s.Id == shipping.Id);
        reloaded.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChanges_DefaultFreeShipping_IsDisabledWithNullThreshold()
    {
        var shipping = new ShippingBuilder()
            .WithName($"Free Ship {Guid.NewGuid():N}"[..19])
            .WithBaseCost(50_000m, "IRT")
            .Build();
        await PersistAsync(shipping);
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Shippings.FirstAsync(s => s.Id == shipping.Id);
        reloaded.FreeShipping.IsEnabled.ShouldBeFalse();
        reloaded.FreeShipping.ThresholdAmount.ShouldBeNull();
        reloaded.QualifiesForFreeShipping(global::SharedKernel.ValueObjects.Money.FromDecimal(1_000_000m, "IRT")).ShouldBeFalse();
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(Shippings));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(Shippings.Id));
    }

    [Fact]
    public void Model_Name_IsRequiredWithMaxLength200AndUniqueIndex()
    {
        var entityType = Context.Model.FindEntityType(typeof(Shippings));
        entityType.ShouldNotBeNull();

        var name = entityType!.FindProperty(nameof(Shippings.Name));
        name.ShouldNotBeNull();
        name!.IsNullable.ShouldBeFalse();
        name.GetMaxLength().ShouldBe(200);
        name.GetValueConverter().ShouldNotBeNull();

        var index = entityType.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(Shippings.Name));
        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Model_DescriptionAndEstimatedDeliveryTime_HaveExpectedMaxLengths()
    {
        var entityType = Context.Model.FindEntityType(typeof(Shippings));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(Shippings.Description))!.GetMaxLength().ShouldBe(500);
        entityType.FindProperty(nameof(Shippings.EstimatedDeliveryTime))!.GetMaxLength().ShouldBe(200);
    }

    [Fact]
    public void Model_MaxWeight_HasDecimalColumnType()
    {
        var property = Context.Model.FindEntityType(typeof(Shippings))!.FindProperty(nameof(Shippings.MaxWeight));
        property.ShouldNotBeNull();
        property!.GetColumnType().ShouldBe("numeric(18,2)");
    }

    [Fact]
    public void Model_BaseCost_IsRequiredOwnedType()
    {
        var navigation = Context.Model.FindEntityType(typeof(Shippings))!.FindNavigation(nameof(Shippings.BaseCost));
        navigation.ShouldNotBeNull();
        navigation!.IsCollection.ShouldBeFalse();
    }

    [Fact]
    public void Model_FreeShippingDeliveryTimeAndOrderRange_AreOwnedTypes()
    {
        var entityType = Context.Model.FindEntityType(typeof(Shippings));
        entityType.ShouldNotBeNull();

        entityType!.FindNavigation(nameof(Shippings.FreeShipping)).ShouldNotBeNull();
        entityType.FindNavigation(nameof(Shippings.DeliveryTime)).ShouldNotBeNull();
        entityType.FindNavigation(nameof(Shippings.OrderRange)).ShouldNotBeNull();
    }

    [Fact]
    public void Model_IsActiveAndIsDefault_HaveNonUniqueIndexes()
    {
        var entityType = Context.Model.FindEntityType(typeof(Shippings));
        entityType.ShouldNotBeNull();

        foreach (var propertyName in new[] { nameof(Shippings.IsActive), nameof(Shippings.IsDefault) })
        {
            var index = entityType!.GetIndexes()
                .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == propertyName);
            index.ShouldNotBeNull($"index on {propertyName} should exist");
            index!.IsUnique.ShouldBeFalse();
        }
    }

    [Fact]
    public void Model_CreatedAtIsRequired_UpdatedAtIsOptional()
    {
        var entityType = Context.Model.FindEntityType(typeof(Shippings));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(Shippings.CreatedAt))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(Shippings.UpdatedAt))!.IsNullable.ShouldBeTrue();
    }
}
