using global::Domain.User.ValueObjects;
using Tests.TestInfrastructure.Base;
using Users = global::Domain.User.Aggregates.User;

namespace Tests.Infrastructure.User.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class UserConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<Users> PersistAsync(Users user, CancellationToken ct = default)
    {
        user.ClearDomainEvents();
        Context.Users.Add(user);
        await Context.SaveChangesAsync(ct);
        return user;
    }

    [Fact]
    public async Task SaveChanges_NewUser_RoundTripsAllScalarProperties()
    {
        var email = $"roundtrip-{Guid.NewGuid():N}@example.com";
        var user = new UserBuilder()
            .WithFullName(FullName.Create("Ali", "Rezaei"))
            .WithEmail(email)
            .WithPasswordHash("hashed-password")
            .WithPhoneNumber(PhoneNumber.Create("09121234567"))
            .Build();
        await PersistAsync(user);
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(user.Id);
        reloaded.FullName.FirstName.ShouldBe("Ali");
        reloaded.FullName.LastName.ShouldBe("Rezaei");
        reloaded.Email.Value.ShouldBe(email);
        reloaded.PhoneNumber.ShouldNotBeNull();
        reloaded.PhoneNumber!.Value.ShouldBe("09121234567");
        reloaded.PasswordHash.ShouldBe("hashed-password");
        reloaded.IsActive.ShouldBeTrue();
        reloaded.IsAdmin.ShouldBeFalse();
        reloaded.IsEmailVerified.ShouldBeFalse();
        reloaded.FailedLoginAttempts.ShouldBe(0);
        reloaded.DefaultAddressId.ShouldBeNull();
        reloaded.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task SaveChanges_DuplicateEmail_ThrowsDbUpdateException()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        await PersistAsync(new UserBuilder().WithEmail(email).Build());

        var duplicate = new UserBuilder().WithEmail(email).Build();
        duplicate.ClearDomainEvents();

        Context.Users.Add(duplicate);

        await Should.ThrowAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_DuplicatePhoneNumber_ThrowsDbUpdateException()
    {
        await PersistAsync(new UserBuilder().WithPhoneNumber(PhoneNumber.Create("09121112233")).Build());

        var duplicate = new UserBuilder().WithPhoneNumber(PhoneNumber.Create("09121112233")).Build();
        duplicate.ClearDomainEvents();

        Context.Users.Add(duplicate);

        await Should.ThrowAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_TwoUsersWithNullPhoneNumber_BothPersist()
    {
        await PersistAsync(new UserBuilder().WithPhoneNumber(null).Build());
        await PersistAsync(new UserBuilder().WithPhoneNumber(null).Build());

        var count = await Context.Users.CountAsync();
        count.ShouldBe(2);
    }

    [Fact]
    public async Task QueryFilter_DeactivatedUser_IsExcludedFromDefaultQuery()
    {
        var user = await PersistAsync(new UserBuilder().Build());

        user.Deactivate();
        user.ClearDomainEvents();
        Context.Users.Update(user);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var visible = await Context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        visible.ShouldBeNull();
    }

    [Fact]
    public async Task IgnoreQueryFilters_DeactivatedUser_IsReturned()
    {
        var user = await PersistAsync(new UserBuilder().Build());

        user.Deactivate();
        user.ClearDomainEvents();
        Context.Users.Update(user);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var loaded = await Context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == user.Id);

        loaded.ShouldNotBeNull();
        loaded!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChanges_PromoteToAdminAndVerifyEmail_PersistFlags()
    {
        var user = await PersistAsync(new UserBuilder().Build());

        user.PromoteToAdmin();
        user.VerifyEmail();
        user.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Users.FirstAsync(u => u.Id == user.Id);
        reloaded.IsAdmin.ShouldBeTrue();
        reloaded.IsEmailVerified.ShouldBeTrue();
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(Users));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(Users.Id));
    }

    [Fact]
    public void Model_FullName_IsRequiredOwnedTypeWithMaxLengths()
    {
        var entityType = Context.Model.FindEntityType(typeof(Users));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(Users.FullName));
        navigation.ShouldNotBeNull();
        navigation!.IsCollection.ShouldBeFalse();

        var owned = navigation.TargetEntityType;
        var firstName = owned.FindProperty(nameof(FullName.FirstName));
        firstName.ShouldNotBeNull();
        firstName!.IsNullable.ShouldBeFalse();
        firstName.GetMaxLength().ShouldBe(100);
        firstName.GetColumnName().ShouldBe("FirstName");

        var lastName = owned.FindProperty(nameof(FullName.LastName));
        lastName.ShouldNotBeNull();
        lastName!.IsNullable.ShouldBeFalse();
        lastName.GetMaxLength().ShouldBe(100);
        lastName.GetColumnName().ShouldBe("LastName");
    }

    [Fact]
    public void Model_Email_IsRequiredWithMaxLength256AndUniqueIndex()
    {
        var entityType = Context.Model.FindEntityType(typeof(Users));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(Users.Email));
        navigation.ShouldNotBeNull();

        var owned = navigation!.TargetEntityType;
        var value = owned.FindProperty("Value");
        value.ShouldNotBeNull();
        value!.IsNullable.ShouldBeFalse();
        value.GetMaxLength().ShouldBe(256);
        value.GetColumnName().ShouldBe("Email");

        var index = owned.GetIndexes().SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == "Value");
        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Model_PhoneNumber_HasFilteredUniqueIndex()
    {
        var entityType = Context.Model.FindEntityType(typeof(Users));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(Users.PhoneNumber));
        navigation.ShouldNotBeNull();

        var owned = navigation!.TargetEntityType;
        var value = owned.FindProperty("Value");
        value.ShouldNotBeNull();
        value!.GetMaxLength().ShouldBe(20);
        value.GetColumnName().ShouldBe("PhoneNumber");

        var index = owned.GetIndexes().SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == "Value");
        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
        index.GetFilter().ShouldContain("PhoneNumber");
    }

    [Fact]
    public void Model_PasswordHash_HasMaxLength500AndIsOptional()
    {
        var property = Context.Model.FindEntityType(typeof(Users))!.FindProperty(nameof(Users.PasswordHash));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeTrue();
        property.GetMaxLength().ShouldBe(500);
    }

    [Fact]
    public void Model_DefaultAddressId_IsOptional()
    {
        var property = Context.Model.FindEntityType(typeof(Users))!.FindProperty(nameof(Users.DefaultAddressId));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeTrue();
        property.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasActiveQueryFilter()
    {
        var entityType = Context.Model.FindEntityType(typeof(Users));
        entityType.ShouldNotBeNull();
        entityType!.GetQueryFilter().ShouldNotBeNull();
    }
}
