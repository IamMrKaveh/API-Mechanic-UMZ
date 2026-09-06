using Infrastructure.Common.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tests.Infrastructure.Common.Extensions;

public class RowVersionConfigurationExtensionsTests
{
    private sealed class RowVersionEntity
    {
        public int Id { get; set; }
    }

    private sealed class RowVersionOwner
    {
        public int Id { get; set; }
        public RowVersionOwned Owned { get; set; } = new();
    }

    private sealed class RowVersionOwned
    {
        public string? Name { get; set; }
    }

    [Fact]
    public void RowVersionColumnName_IsRowVersion()
    {
        RowVersionConfigurationExtensions.RowVersionColumnName.ShouldBe("RowVersion");
    }

    [Fact]
    public void AddInterceptorRowVersion_ForEntity_ConfiguresShadowConcurrencyToken()
    {
        var modelBuilder = new ModelBuilder();

        var result = modelBuilder.Entity<RowVersionEntity>()
            .AddInterceptorRowVersion<RowVersionEntity>();

        result.ShouldNotBeNull();
        result.ShouldBeOfType<PropertyBuilder<byte[]>>();

        var property = modelBuilder.Model
            .FindEntityType(typeof(RowVersionEntity))!
            .FindProperty(RowVersionConfigurationExtensions.RowVersionColumnName);

        property.ShouldNotBeNull();
        property!.ClrType.ShouldBe(typeof(byte[]));
        property.IsConcurrencyToken.ShouldBeTrue();
        property.IsNullable.ShouldBeFalse();
        property.ValueGenerated.ShouldBe(ValueGenerated.OnAddOrUpdate);
        property.GetBeforeSaveBehavior().ShouldBe(PropertySaveBehavior.Save);
        property.GetAfterSaveBehavior().ShouldBe(PropertySaveBehavior.Save);
    }

    [Fact]
    public void AddInterceptorRowVersion_ForOwnedType_ConfiguresShadowConcurrencyToken()
    {
        var modelBuilder = new ModelBuilder();
        var ownership = modelBuilder.Entity<RowVersionOwner>().OwnsOne(o => o.Owned);

        var result = ownership.AddInterceptorRowVersion();

        result.ShouldNotBeNull();
        result.ShouldBeOfType<PropertyBuilder<byte[]>>();

        var ownedType = modelBuilder.Model.FindEntityType(typeof(RowVersionOwned))!;
        ownedType.IsOwned().ShouldBeTrue();

        var property = ownedType.FindProperty(RowVersionConfigurationExtensions.RowVersionColumnName);

        property.ShouldNotBeNull();
        property!.ClrType.ShouldBe(typeof(byte[]));
        property.IsConcurrencyToken.ShouldBeTrue();
        property.IsNullable.ShouldBeFalse();
        property.ValueGenerated.ShouldBe(ValueGenerated.OnAddOrUpdate);
        property.GetBeforeSaveBehavior().ShouldBe(PropertySaveBehavior.Save);
        property.GetAfterSaveBehavior().ShouldBe(PropertySaveBehavior.Save);
    }

    [Fact]
    public void AddInterceptorRowVersion_ForEntity_DoesNotDisturbOtherProperties()
    {
        // NOTE: a bare ModelBuilder runs no discovery conventions, so the
        // sibling property is added explicitly here.
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<RowVersionEntity>().Property(e => e.Id);
        modelBuilder.Entity<RowVersionEntity>().AddInterceptorRowVersion<RowVersionEntity>();

        var entityType = modelBuilder.Model.FindEntityType(typeof(RowVersionEntity))!;

        entityType.FindProperty(nameof(RowVersionEntity.Id)).ShouldNotBeNull();
        entityType.GetProperties().Count().ShouldBe(2);
    }
}
