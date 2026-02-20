namespace Application.Product.Features.Commands.CreateProduct;

public record CreateProductCommand(CreateProductInput Input) : IRequest<int>;