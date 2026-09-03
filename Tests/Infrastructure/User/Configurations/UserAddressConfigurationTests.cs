using global::Domain.User.Entities;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.User.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class UserAddressConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<global::Domain.User.Aggregates.User> SeedUserAsync(CancellationToken ct = default)
    {
        var user = new UserBuilder().Build();
        user.ClearDomainEvents();

        Context.Users.Add(user);
        await Context.SaveChangesAsync(ct);
        return user;
    }

    [Fact]
    public async Task SaveChanges_AddAddress_RoundTripsAllProperties()
    {
        var user = await SeedUserAsync();
        var parameters = new UserAddressParametersBuilder()
            .WithTitle("Home")
            .WithReceiverName("Ali Rezaei")
            .WithProvince("Tehran")
            .WithCity("Tehran")
            .WithAddress("Valiasr St, No 123")
            .WithPostalCode("1234567890")
            .WithLatitude(35.6892m)
            .WithLongitude(51.3890m);

        var address = parameters.AddTo(user);
        user.ClearDomainEvents();
        Context.Users.Update(user);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.UserAddresses.FirstOrDefaultAsync(a => a.Id == address.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(address.Id);
        reloaded.UserId.ShouldBe(user.Id);
        reloaded.Title.ShouldBe("Home");
        reloaded.ReceiverName.ShouldBe("Ali Rezaei");
        reloaded.PhoneNumber.ShouldNotBeNull();
        reloaded.Province.ShouldBe("Tehran");
        reloaded.City.ShouldBe("Tehran");
        reloaded.Address.ShouldBe("Valiasr St, No 123");
        reloaded.PostalCode.ShouldBe("1234567890");
        reloaded.Latitude.ShouldBe(35.6892m);
        reloaded.Longitude.ShouldBe(51.3890m);
        reloaded.IsDefault.ShouldBeFalse();
        reloaded.CreatedAt.ShouldNotBe(default);
        reloaded.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SaveChanges_SetDefaultAddress_PersistsDefaultFlag()
    {
        var user = await SeedUserAsync();
        var first = new UserAddressParametersBuilder().WithTitle("Home").AddTo(user);
        var second = new UserAddressParametersBuilder().WithTitle("Office").AddTo(user);
        user.ClearDomainEvents();
        Context.Users.Update(user);
        await Context.SaveChangesAsync();

        user.SetDefaultAddress(second.Id);
        user.ClearDomainEvents();
        Context.Users.Update(user);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.UserAddresses.Where(a => a.UserId == user.Id).ToListAsync();
        reloaded.Count.ShouldBe(2);
        reloaded.Single(a => a.Id == second.Id).IsDefault.ShouldBeTrue();
        reloaded.Single(a => a.Id == first.Id).IsDefault.ShouldBeFalse();

        var owner = await Context.Users.FirstAsync(u => u.Id == user.Id);
        owner.DefaultAddressId.ShouldBe(second.Id);
    }

    [Fact]
    public async Task SaveChanges_WhenUserIsDeleted_AddressesAreCascadeDeleted()
    {
        var user = await SeedUserAsync();
        var address = new UserAddressParametersBuilder().AddTo(user);
        user.ClearDomainEvents();
        Context.Users.Update(user);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var loaded = await Context.Users
            .Include(u => u.Addresses)
            .IgnoreQueryFilters()
            .FirstAsync(u => u.Id == user.Id);
        Context.Users.Remove(loaded);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var remaining = await Context.UserAddresses
            .IgnoreQueryFilters()
            .CountAsync(a => a.UserId == user.Id);
        remaining.ShouldBe(0);
    }

    [Fact]
    public async Task QueryFilter_WhenUserIsDeactivated_AddressesAreHidden()
    {
        var user = await SeedUserAsync();
        var address = new UserAddressParametersBuilder().AddTo(user);
        user.ClearDomainEvents();
        Context.Users.Update(user);
        await Context.SaveChangesAsync();

        user.Deactivate();
        user.ClearDomainEvents();
        Context.Users.Update(user);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var visible = await Context.UserAddresses.FirstOrDefaultAsync(a => a.Id == address.Id);
        visible.ShouldBeNull();

        var hidden = await Context.UserAddresses.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == address.Id);
        hidden.ShouldNotBeNull();
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(UserAddress));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(UserAddress.Id));
    }

    [Fact]
    public void Model_User_ForeignKeyIsRequiredWithCascadeDelete()
    {
        var entityType = Context.Model.FindEntityType(typeof(UserAddress));
        entityType.ShouldNotBeNull();

        var fk = entityType!.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(UserAddress.UserId));
        fk.ShouldNotBeNull();
        fk!.IsRequired.ShouldBeTrue();
        fk.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        fk.PrincipalToDependent.ShouldNotBeNull();
        fk.PrincipalToDependent!.Name.ShouldBe(nameof(global::Domain.User.Aggregates.User.Addresses));
    }

    [Fact]
    public void Model_TitleReceiverProvinceCityAddressPostal_HaveExpectedMaxLengths()
    {
        var entityType = Context.Model.FindEntityType(typeof(UserAddress));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(UserAddress.Title))!.GetMaxLength().ShouldBe(100);

        var receiver = entityType.FindProperty(nameof(UserAddress.ReceiverName));
        receiver.ShouldNotBeNull();
        receiver!.IsNullable.ShouldBeFalse();
        receiver.GetMaxLength().ShouldBe(100);

        entityType.FindProperty(nameof(UserAddress.Province))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(UserAddress.Province))!.GetMaxLength().ShouldBe(100);
        entityType.FindProperty(nameof(UserAddress.City))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(UserAddress.City))!.GetMaxLength().ShouldBe(100);
        entityType.FindProperty(nameof(UserAddress.Address))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(UserAddress.Address))!.GetMaxLength().ShouldBe(500);
        entityType.FindProperty(nameof(UserAddress.PostalCode))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(UserAddress.PostalCode))!.GetMaxLength().ShouldBe(20);
    }

    [Fact]
    public void Model_PhoneNumber_IsRequiredOwnedType()
    {
        var navigation = Context.Model.FindEntityType(typeof(UserAddress))!.FindNavigation(nameof(UserAddress.PhoneNumber));
        navigation.ShouldNotBeNull();
        navigation!.IsCollection.ShouldBeFalse();

        var value = navigation.TargetEntityType.FindProperty("Value");
        value.ShouldNotBeNull();
        value!.IsNullable.ShouldBeFalse();
        value.GetMaxLength().ShouldBe(20);
        value.GetColumnName().ShouldBe("ReceiverPhoneNumber");
    }

    [Fact]
    public void Model_LatitudeAndLongitude_HaveDecimalColumnTypes()
    {
        var entityType = Context.Model.FindEntityType(typeof(UserAddress));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(UserAddress.Latitude))!.GetColumnType().ShouldBe("numeric(9,6)");
        entityType.FindProperty(nameof(UserAddress.Longitude))!.GetColumnType().ShouldBe("numeric(9,6)");
    }

    [Fact]
    public void Model_HasIndexOnUserId()
    {
        var entityType = Context.Model.FindEntityType(typeof(UserAddress));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(UserAddress.UserId));
        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasQueryFilter()
    {
        var entityType = Context.Model.FindEntityType(typeof(UserAddress));
        entityType.ShouldNotBeNull();
        entityType!.GetQueryFilter().ShouldNotBeNull();
    }
}
