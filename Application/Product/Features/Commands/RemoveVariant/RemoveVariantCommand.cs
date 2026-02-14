namespace Application.Product.Features.Commands.RemoveVariant;

public record RemoveVariantCommand(int ProductId, int VariantId) : IRequest<ServiceResult>;