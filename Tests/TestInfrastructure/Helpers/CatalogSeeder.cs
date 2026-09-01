using Domain.Brand.Aggregates;
using Domain.Category.Aggregates;
using Domain.Product.Aggregates;

namespace Tests.TestInfrastructure.Helpers;

public static class CatalogSeeder
{
    public static async Task<Category> SeedCategoryAsync(DBContext context, CancellationToken ct = default)
    {
        var category = await new CategoryBuilder()
            .WithName($"cat-{Guid.NewGuid():N}"[..24])
            .WithSlug($"slug-{Guid.NewGuid():N}"[..24])
            .WithUniquenessChecker(new StubCategoryUniquenessChecker())
            .BuildAsync(ct);

        category.ClearDomainEvents();
        await context.Categories.AddAsync(category, ct);
        await context.SaveChangesAsync(ct);
        return category;
    }

    public static async Task<Brand> SeedBrandAsync(
        DBContext context,
        Category? category = null,
        CancellationToken ct = default)
    {
        category ??= await SeedCategoryAsync(context, ct);

        var brand = await new BrandBuilder()
            .WithName($"brand-{Guid.NewGuid():N}"[..24])
            .WithSlug($"bslug-{Guid.NewGuid():N}"[..24])
            .WithCategoryId(category.Id)
            .WithUniquenessChecker(new StubBrandUniquenessChecker())
            .BuildAsync(ct);

        brand.ClearDomainEvents();
        await context.Brands.AddAsync(brand, ct);
        await context.SaveChangesAsync(ct);
        return brand;
    }

    public static async Task<(Brand brand, Category category, Product product)> SeedProductAsync(
        DBContext context,
        CancellationToken ct = default)
    {
        var category = await SeedCategoryAsync(context, ct);
        var brand = await SeedBrandAsync(context, category, ct);

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        product.ClearDomainEvents();
        await context.Products.AddAsync(product, ct);
        await context.SaveChangesAsync(ct);
        context.ChangeTracker.Clear();

        return (brand, category, product);
    }
}
