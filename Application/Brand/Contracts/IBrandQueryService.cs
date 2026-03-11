namespace Application.Brand.Contracts;

public interface IBrandQueryService
{
    Task<IReadOnlyList<BrandDto>> GetPublicBrandsAsync(
        int? categoryId = null,
        CancellationToken ct = default);

    Task<PaginatedResult<BrandDto>> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);
}