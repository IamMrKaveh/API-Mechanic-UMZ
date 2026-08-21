using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.User.Repositories;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.User.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class UserRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private UserRepository _sut = null!;

    public async Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new UserRepository(_context);

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [RequiresDockerFact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsAggregateFromDatabase()
    {
        var fullName = FullName.Create("Ali", "Rezaei");
        var email = Email.Create($"user-{Guid.NewGuid():N}@example.com");
        var phone = PhoneNumber.Create("09121234567");

        var user = new UserBuilder()
            .WithFullName(fullName)
            .WithEmail(email)
            .WithPhoneNumber(phone)
            .WithPasswordHash("hash-value")
            .Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(user.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(user.Id);
        loaded.FullName.FirstName.ShouldBe("Ali");
        loaded.FullName.LastName.ShouldBe("Rezaei");
        loaded.Email.Value.ShouldBe(email.Value);
        loaded.PhoneNumber.ShouldNotBeNull();
        loaded.PhoneNumber!.Value.ShouldBe("09121234567");
        loaded.PasswordHash.ShouldBe("hash-value");
        loaded.IsActive.ShouldBeTrue();
        loaded.IsAdmin.ShouldBeFalse();
        loaded.IsEmailVerified.ShouldBeFalse();
        loaded.FailedLoginAttempts.ShouldBe(0);
    }

    [RequiresDockerFact]
    public async Task GetByIdAsync_WhenIdDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(UserId.NewId());

        loaded.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetByIdAsync_WhenUserIsDeactivated_ReturnsNullDueToGlobalQueryFilter()
    {
        var user = new UserBuilder().Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();

        user.Deactivate();
        _sut.Update(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(user.Id);

        loaded.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetActiveByIdAsync_WhenUserIsActive_ReturnsUser()
    {
        var user = new UserBuilder().Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetActiveByIdAsync(user.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(user.Id);
        loaded.IsActive.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task GetActiveByIdAsync_WhenUserIsDeactivated_ReturnsNull()
    {
        var user = new UserBuilder().Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();

        user.Deactivate();
        _sut.Update(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetActiveByIdAsync(user.Id);

        loaded.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetByEmailAsync_WhenEmailExists_ReturnsUser()
    {
        var email = Email.Create($"lookup-{Guid.NewGuid():N}@example.com");
        var user = new UserBuilder().WithEmail(email).Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByEmailAsync(email);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(user.Id);
        loaded.Email.Value.ShouldBe(email.Value);
    }

    [RequiresDockerFact]
    public async Task GetByEmailAsync_WhenEmailDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByEmailAsync(Email.Create($"missing-{Guid.NewGuid():N}@example.com"));

        loaded.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetByPhoneNumberAsync_WhenPhoneExists_ReturnsUser()
    {
        var phone = PhoneNumber.Create("09121112233");
        var user = new UserBuilder().WithPhoneNumber(phone).Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByPhoneNumberAsync(phone);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(user.Id);
        loaded.PhoneNumber.ShouldNotBeNull();
        loaded.PhoneNumber!.Value.ShouldBe("09121112233");
    }

    [RequiresDockerFact]
    public async Task GetByPhoneNumberAsync_WhenPhoneDoesNotExist_ReturnsNull()
    {
        var user = new UserBuilder().WithPhoneNumber(null).Build();
        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByPhoneNumberAsync(PhoneNumber.Create("09129998877"));

        loaded.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task ExistsByPhoneNumberAsync_WhenPhoneExists_ReturnsTrue()
    {
        var phone = PhoneNumber.Create("09122223344");
        var user = new UserBuilder().WithPhoneNumber(phone).Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByPhoneNumberAsync(phone);

        exists.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task ExistsByPhoneNumberAsync_WhenPhoneDoesNotExist_ReturnsFalse()
    {
        var exists = await _sut.ExistsByPhoneNumberAsync(PhoneNumber.Create("09127776655"));

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task ExistsByPhoneNumberAsync_WhenExcludeIdMatchesOwner_ReturnsFalse()
    {
        var phone = PhoneNumber.Create("09123334455");
        var user = new UserBuilder().WithPhoneNumber(phone).Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByPhoneNumberAsync(phone, user.Id);

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task ExistsByPhoneNumberAsync_WhenOwnerIsDeactivated_StillReturnsTrue()
    {
        var phone = PhoneNumber.Create("09124445566");
        var user = new UserBuilder().WithPhoneNumber(phone).Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();

        user.Deactivate();
        _sut.Update(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByPhoneNumberAsync(phone);

        exists.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task GetWithAddressesAsync_WhenUserHasAddresses_LoadsAddresses()
    {
        var user = new UserBuilder().Build();

        new UserAddressParametersBuilder().AddTo(user);
        new UserAddressParametersBuilder()
            .WithTitle("Office")
            .AddTo(user);

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetWithAddressesAsync(user.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(user.Id);
        loaded.Addresses.Count.ShouldBe(2);
        loaded.Addresses.ShouldContain(a => a.Title == "Office");
    }

    [RequiresDockerFact]
    public async Task GetWithAddressesAsync_WhenUserHasNoAddresses_ReturnsUserWithEmptyAddresses()
    {
        var user = new UserBuilder().Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetWithAddressesAsync(user.Id);

        loaded.ShouldNotBeNull();
        loaded!.Addresses.ShouldBeEmpty();
    }

    [RequiresDockerFact]
    public async Task GetUserAddressAsync_WhenAddressExists_ReturnsAddress()
    {
        var user = new UserBuilder().Build();
        var parameters = new UserAddressParametersBuilder();
        parameters.AddTo(user);

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetUserAddressAsync(parameters.AddressId);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(parameters.AddressId);
        loaded.UserId.ShouldBe(user.Id);
    }

    [RequiresDockerFact]
    public async Task GetUserAddressAsync_WhenAddressDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetUserAddressAsync(UserAddressId.NewId());

        loaded.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetUserAddressAsync_WhenOwnerIsDeactivated_ReturnsNullDueToQueryFilter()
    {
        var user = new UserBuilder().Build();
        var parameters = new UserAddressParametersBuilder();
        parameters.AddTo(user);

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();

        user.Deactivate();
        _sut.Update(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetUserAddressAsync(parameters.AddressId);

        loaded.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetAllActiveUserIdsAsync_ReturnsOnlyActiveUsers()
    {
        var activeOne = new UserBuilder().Build();
        var activeTwo = new UserBuilder().Build();
        var inactive = new UserBuilder().Build();

        await _sut.AddAsync(activeOne);
        await _sut.AddAsync(activeTwo);
        await _sut.AddAsync(inactive);
        await _context.SaveChangesAsync();

        inactive.Deactivate();
        _sut.Update(inactive);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var ids = await _sut.GetAllActiveUserIdsAsync();

        ids.ShouldContain(activeOne.Id.Value);
        ids.ShouldContain(activeTwo.Id.Value);
        ids.ShouldNotContain(inactive.Id.Value);
    }

    [RequiresDockerFact]
    public async Task Update_AfterUpdatingProfile_PersistsChanges()
    {
        var user = new UserBuilder().Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(user.Id);
        loaded.ShouldNotBeNull();
        loaded!.UpdateProfile(FullName.Create("Reza", "Ahmadi"), PhoneNumber.Create("09121110000"));

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(user.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.FullName.FirstName.ShouldBe("Reza");
        reloaded.FullName.LastName.ShouldBe("Ahmadi");
        reloaded.PhoneNumber.ShouldNotBeNull();
        reloaded.PhoneNumber!.Value.ShouldBe("09121110000");
    }

    [RequiresDockerFact]
    public async Task AddAsync_TwoUsersWithSameEmail_ThrowsOnSaveChangesDueToUniqueIndex()
    {
        var email = Email.Create($"dup-{Guid.NewGuid():N}@example.com");
        var first = new UserBuilder().WithEmail(email).WithPhoneNumber(PhoneNumber.Create("09121000001")).Build();
        var second = new UserBuilder().WithEmail(email).WithPhoneNumber(PhoneNumber.Create("09121000002")).Build();

        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [RequiresDockerFact]
    public async Task AddAsync_TwoUsersWithSamePhoneNumber_ThrowsOnSaveChangesDueToUniqueIndex()
    {
        var phone = PhoneNumber.Create("09125000000");
        var first = new UserBuilder()
            .WithEmail(Email.Create($"phone-a-{Guid.NewGuid():N}@example.com"))
            .WithPhoneNumber(phone)
            .Build();
        var second = new UserBuilder()
            .WithEmail(Email.Create($"phone-b-{Guid.NewGuid():N}@example.com"))
            .WithPhoneNumber(phone)
            .Build();

        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [RequiresDockerFact]
    public async Task AddAsync_TwoUsersBothWithoutPhoneNumber_SucceedsBecauseUniqueIndexIsFiltered()
    {
        var first = new UserBuilder()
            .WithEmail(Email.Create($"nophone-a-{Guid.NewGuid():N}@example.com"))
            .WithPhoneNumber(null)
            .Build();
        var second = new UserBuilder()
            .WithEmail(Email.Create($"nophone-b-{Guid.NewGuid():N}@example.com"))
            .WithPhoneNumber(null)
            .Build();

        await _sut.AddAsync(first);
        await _sut.AddAsync(second);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loadedFirst = await _sut.GetByIdAsync(first.Id);
        var loadedSecond = await _sut.GetByIdAsync(second.Id);

        loadedFirst.ShouldNotBeNull();
        loadedSecond.ShouldNotBeNull();
        loadedFirst!.PhoneNumber.ShouldBeNull();
        loadedSecond!.PhoneNumber.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task AddAsync_UserWithDefaultAddress_PersistsDefaultAddressIdAndIsDefaultFlag()
    {
        var user = new UserBuilder().Build();
        var parameters = new UserAddressParametersBuilder();
        parameters.AddTo(user);
        user.SetDefaultAddress(parameters.AddressId);

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetWithAddressesAsync(user.Id);

        loaded.ShouldNotBeNull();
        loaded!.DefaultAddressId.ShouldNotBeNull();
        loaded.DefaultAddressId!.Value.ShouldBe(parameters.AddressId.Value);
        loaded.Addresses.Count.ShouldBe(1);
        loaded.Addresses[0].IsDefault.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task AddAsync_UserOwnedValueObjectsRoundTripCorrectly()
    {
        var email = Email.Create($"vo-{Guid.NewGuid():N}@example.com");
        var phone = PhoneNumber.Create("09126667788");
        var fullName = FullName.Create("Sara", "Karimi");

        var user = new UserBuilder()
            .WithFullName(fullName)
            .WithEmail(email)
            .WithPhoneNumber(phone)
            .Build();

        await _sut.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(user.Id);

        loaded.ShouldNotBeNull();
        loaded!.FullName.FirstName.ShouldBe("Sara");
        loaded.FullName.LastName.ShouldBe("Karimi");
        loaded.Email.Value.ShouldBe(email.Value);
        loaded.PhoneNumber.ShouldNotBeNull();
        loaded.PhoneNumber!.Value.ShouldBe(phone.Value);
    }
}
