namespace Application.Product.Features.Queries.GetAdminProducts;

public record GetAdminProductsQuery(AdminProductSearchParams SearchParams)
    : IRequest<ServiceResult<PaginatedResult<AdminProductListItemDto>>>;