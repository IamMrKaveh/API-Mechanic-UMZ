using Application.Common.Results;
using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Queries.GetProductDetails;

public record GetProductDetailsQuery(ProductId ProductId) : IRequest<ServiceResult<PublicProductDetailDto?>>;