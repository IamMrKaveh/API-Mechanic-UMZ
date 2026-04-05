using Application.Common.Results;
using Application.Product.Features.Shared;
using SharedKernel.Models;

namespace Application.Product.Features.Queries.GetProductCatalog;

public record GetProductCatalogQuery(ProductCatalogSearchParams SearchParams) : IRequest<ServiceResult<PaginatedResult<ProductCatalogItemDto>>>;