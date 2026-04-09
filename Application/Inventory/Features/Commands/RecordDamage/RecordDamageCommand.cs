namespace Application.Inventory.Features.Commands.RecordDamage;

public record RecordDamageCommand(
    Guid VariantId,
    int Quantity,
    Guid UserId,
    string Reason) : IRequest<ServiceResult>;