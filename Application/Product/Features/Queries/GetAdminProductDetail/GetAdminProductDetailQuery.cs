using Application.Common.Models;

namespace Application.Product.Features.Queries.GetAdminProductDetail;

public record GetAdminProductDetailQuery(int ProductId)
    : IRequest<ServiceResult<AdminProductDetailDto?>>;