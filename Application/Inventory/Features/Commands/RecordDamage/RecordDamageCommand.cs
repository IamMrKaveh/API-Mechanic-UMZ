namespace Application.Inventory.Features.Commands.RecordDamage;

public record RecordDamageCommand(
    Guid VariantId,
    int Quantity,
    string Reason) : IRequest<ServiceResult>;