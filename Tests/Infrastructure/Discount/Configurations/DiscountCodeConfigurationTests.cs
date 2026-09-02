using Domain.Discount.Aggregates;
using Domain.Discount.Enums;
using Domain.Discount.ValueObjects;

namespace Tests.Infrastructure.Discount.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class DiscountCodeConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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
    public async Task SaveChanges_PersistsAllScalarPropertiesAndOwnedDiscountValue()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("SAVE10")
            .WithValue(DiscountValue.Percentage(10m))
            .WithMaximumDiscountAmount(500m, "IRT")
            .WithUsageLimit(20)
            .Build();
        discount.ClearDomainEvents();

        _context.DiscountCodes.Add(discount);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.DiscountCodes.FirstAsync(d => d.Id == discount.Id);

        reloaded.Id.ShouldBe(discount.Id);
        reloaded.Code.ShouldBe("SAVE10");
        reloaded.Value.Amount.ShouldBe(10m);
        reloaded.Value.Type.ShouldBe(DiscountType.Percentage);
        reloaded.MaximumDiscountAmount.ShouldNotBeNull();
        reloaded.MaximumDiscountAmount!.Amount.ShouldBe(500m);
        reloaded.UsageLimit.ShouldBe(20);
        reloaded.UsageCount.ShouldBe(0);
        reloaded.IsActive.ShouldBeTrue();
        reloaded.IsDeleted.ShouldBeFalse();

        var rowVersion = _context.Entry(reloaded).Property<byte[]>("RowVersion").CurrentValue;
        rowVersion.ShouldNotBeNull();
        rowVersion!.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task SaveChanges_WithNullMaximumDiscountAmount_PersistsAsNull()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("NOCAP")
            .WithMaximumDiscountAmount(null)
            .Build();
        discount.ClearDomainEvents();

        _context.DiscountCodes.Add(discount);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.DiscountCodes.FirstAsync(d => d.Id == discount.Id);
        reloaded.MaximumDiscountAmount.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_DuplicateCode_ThrowsDbUpdateException()
    {
        var first = new DiscountCodeBuilder().WithCode("DUP1").Build();
        first.ClearDomainEvents();
        _context.DiscountCodes.Add(first);
        await _context.SaveChangesAsync();

        var second = new DiscountCodeBuilder().WithCode("DUP1").Build();
        second.ClearDomainEvents();
        _context.DiscountCodes.Add(second);

        await Should.ThrowAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public void Model_MappedToDiscountCodesTable()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = _context.Model.FindEntityType(typeof(DiscountCode));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("DiscountCodes");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountCode));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(DiscountCode.Id));
    }

    [Fact]
    public void Model_Code_IsRequiredAndHasMaxLength50()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountCode));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(DiscountCode.Code));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(50);
    }

    [Fact]
    public void Model_HasUniqueIndexOnCode()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountCode));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(DiscountCode.Code));

        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Model_OwnsDiscountValue_WithConfiguredColumnNamesAndTypes()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountCode));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(DiscountCode.Value));
        navigation.ShouldNotBeNull();
        navigation!.ForeignKey.IsOwnership.ShouldBeTrue();

        var ownedType = navigation.ForeignKey.DeclaringEntityType;
        var amount = ownedType.FindProperty(nameof(DiscountValue.Amount));
        amount.ShouldNotBeNull();
        amount!.GetColumnName().ShouldBe("DiscountValue");
        amount.GetColumnType().ShouldBe("numeric(18,4)");
        amount.IsNullable.ShouldBeFalse();

        var type = ownedType.FindProperty(nameof(DiscountValue.Type));
        type.ShouldNotBeNull();
        type!.GetColumnName().ShouldBe("DiscountType");
        type.GetMaxLength().ShouldBe(50);
        type.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_MaximumDiscountAmount_HasDecimal184ColumnType()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountCode));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(DiscountCode.MaximumDiscountAmount));
        property.ShouldNotBeNull();
        property!.GetColumnName().ShouldBe("MaximumDiscountAmount");
        property.GetColumnType().ShouldBe("numeric(18,4)");
    }

    [Fact]
    public void Model_HasRowVersionShadowProperty()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountCode));
        entityType.ShouldNotBeNull();

        var rowVersion = entityType!.FindProperty("RowVersion");
        rowVersion.ShouldNotBeNull();
        rowVersion!.IsConcurrencyToken.ShouldBeTrue();
        rowVersion.ValueGenerated.ShouldBe(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);
    }

    [Fact]
    public void Model_HasCascadeDeleteFromDiscountCodeToRestrictions()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountCode));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(DiscountCode.Restrictions));
        navigation.ShouldNotBeNull();
        navigation!.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }

    [Fact]
    public void Model_HasCascadeDeleteFromDiscountCodeToUsages()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountCode));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(DiscountCode.Usages));
        navigation.ShouldNotBeNull();
        navigation!.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }
}
