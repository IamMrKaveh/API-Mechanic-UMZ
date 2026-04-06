using Application.Brand.Features.Shared;
using Application.Common.Results;
using SharedKernel.Models;

namespace Application.Brand.Features.Queries.GetBrands;

public record GetBrandsQuery(
    int? CategoryId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<BrandListItemDto>>>;