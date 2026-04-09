using Application.Brand.Features.Shared;
using SharedKernel.Models;

namespace Application.Brand.Features.Queries.GetAdminBrands;

public record GetAdminBrandsQuery(
    Guid? CategoryId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<BrandListItemDto>>>;