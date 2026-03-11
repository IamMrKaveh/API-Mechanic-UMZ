using Application.Common.Models;

namespace Application.Product.Features.Commands.ActivateProduct;

public record ActivateProductCommand(int ProductId) : IRequest<ServiceResult>;