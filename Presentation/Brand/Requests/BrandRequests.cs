namespace Presentation.Brand.Requests;

public record CreateBrandRequest
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public IFormFile? LogoFile { get; init; }
}

public record UpdateBrandRequest
{
    public string Name { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public IFormFile? LogoFile { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public record MoveBrandRequest(
    Guid BrandId,
    Guid TargetCategoryId
);

public record GetAdminBrandsRequest
{
    public Guid? CategoryId { get; init; }
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public bool IncludeDeleted { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public record GetPublicBrandsRequest(Guid? CategoryId);