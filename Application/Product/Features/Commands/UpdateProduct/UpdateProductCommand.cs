using Application.Common.Models;

namespace Application.Product.Features.Commands.UpdateProduct;

public record UpdateProductCommand(
    UpdateProductInput UpdateProductInput) : IRequest<ServiceResult>;