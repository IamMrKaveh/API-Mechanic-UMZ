namespace Application.Variant.Features.Commands.RemoveVariant;

public record RemoveVariantCommand(Guid ProductId, Guid VariantId) : IRequest<ServiceResult>;