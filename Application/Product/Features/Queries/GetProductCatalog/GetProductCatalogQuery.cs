namespace Application.Product.Features.Queries.GetProductCatalog;

public record GetProductCatalogQuery(ProductCatalogSearchParams SearchParams)
    : IRequest<ServiceResult<PaginatedResult<ProductCatalogItemDto>>>;