namespace Application.Variant.Features.Commands.RemoveVariant;

public record RemoveVariantCommand(int ProductId, int VariantId) : IRequest<ServiceResult>;