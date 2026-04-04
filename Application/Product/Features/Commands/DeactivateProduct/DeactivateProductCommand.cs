using Application.Common.Results;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Commands.DeactivateProduct;

public record DeactivateProductCommand(ProductId ProductId) : IRequest<ServiceResult>;