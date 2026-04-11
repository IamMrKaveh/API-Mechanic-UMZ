namespace Application.Variant.Features.Commands.RemoveVariant;

public record RemoveVariantCommand(
    Guid ProductId,
    Guid VariantId,
    Guid UserId) : IRequest<ServiceResult>;