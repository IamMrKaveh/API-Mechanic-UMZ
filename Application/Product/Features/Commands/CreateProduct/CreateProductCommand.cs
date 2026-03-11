namespace Application.Product.Features.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    int CategoryId,
    int BrandId) : IRequest<ServiceResult<int>>;