using Domain.Brand.Aggregates;
using Domain.Category.Aggregates;
using Domain.Category.ValueObjects;
using Domain.User.Aggregates;
using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Base;

public abstract class IntegrationTestBase(PostgresContainerFixture fixture) : IAsyncLifetime
{
    protected PostgresContainerFixture Fixture { get; } = fixture;
    protected DBContext Context { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, Fixture.UnavailabilityReason ?? "Docker engine not available.");

        Context = Fixture.CreateContext();
        await OnInitializeAsync();
    }

    public virtual async Task DisposeAsync()
    {
        if (!Fixture.IsDockerAvailable)
            return;

        await OnDisposeAsync();
        await Context.DisposeAsync();
        await Fixture.ResetAsync();
    }

    protected virtual Task OnInitializeAsync() => Task.CompletedTask;

    protected virtual Task OnDisposeAsync() => Task.CompletedTask;

    protected async Task<Category> SeedCategoryAsync(
        string? name = null,
        string? slug = null,
        CancellationToken ct = default)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var category = await new CategoryBuilder()
            .WithName(name ?? $"Category-{suffix}")
            .WithSlug(slug ?? $"category-{suffix}")
            .BuildAsync(ct);

        category.ClearDomainEvents();
        Context.Categories.Add(category);
        await Context.SaveChangesAsync(ct);
        return category;
    }

    protected async Task<Brand> SeedBrandAsync(
        CategoryId? categoryId = null,
        string? name = null,
        string? slug = null,
        CancellationToken ct = default)
    {
        var effectiveCategoryId = categoryId ?? (await SeedCategoryAsync(ct: ct)).Id;
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var brand = await new BrandBuilder()
            .WithCategoryId(effectiveCategoryId)
            .WithName(name ?? $"Brand-{suffix}")
            .WithSlug(slug ?? $"brand-{suffix}")
            .BuildAsync(ct);

        brand.ClearDomainEvents();
        Context.Brands.Add(brand);
        await Context.SaveChangesAsync(ct);
        return brand;
    }

    protected async Task<(Brand brand, Category category)> SeedBrandWithCategoryAsync(CancellationToken ct = default)
    {
        var category = await SeedCategoryAsync(ct: ct);
        var brand = await SeedBrandAsync(category.Id, ct: ct);
        return (brand, category);
    }

    protected async Task<User> SeedUserAsync(
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        string? phone = null,
        CancellationToken ct = default)
    {
        var rawHex = Guid.NewGuid().ToString("N");
        var lettersOnly = new string(rawHex.Where(char.IsLetter).ToArray());
        var suffix = lettersOnly.Length >= 8 ? lettersOnly[..8] : lettersOnly.PadRight(8, 'a');

        var builder = new UserBuilder()
            .WithFullName(FullName.Create(firstName ?? $"First{suffix}", lastName ?? $"Last{suffix}"))
            .WithEmail(email ?? $"user-{rawHex[..8]}@example.com");

        if (!string.IsNullOrWhiteSpace(phone))
            builder = builder.WithPhoneNumber(PhoneNumber.Create(phone));

        var user = builder.Build();
        user.ClearDomainEvents();
        Context.Users.Add(user);
        await Context.SaveChangesAsync(ct);
        return user;
    }
}
