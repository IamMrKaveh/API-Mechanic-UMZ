namespace Application.Brand.Features.Shared;

public sealed record BrandInfoDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
}

public sealed record BrandSummaryInProductDto(
    int Id,
    string Name,
    string CategoryName
);

public record BrandSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? IconUrl { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public int ProductCount { get; init; }
    public int ActiveProductCount { get; init; }
}

public record BrandViewDto
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; }
    public string Name { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public string? IconUrl { get; init; }
    public int ActiveProductsCount { get; init; }
    public int TotalProductsCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public record BrandHierarchyDto
{
    public int Id { get; init; }
    public string Title { get; init; }
}

public record BrandTreeDto
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string? Slug { get; init; }
    public int SortOrder { get; init; }
    public int ProductCount { get; init; }
}

public record BrandDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? LogoPath { get; init; }
    public Guid CategoryId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record BrandDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? LogoPath { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
    public int ActiveProductCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record BrandListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
    public string? LogoPath { get; init; }
}