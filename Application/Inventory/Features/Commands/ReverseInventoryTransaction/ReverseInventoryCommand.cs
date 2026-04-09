namespace Application.Inventory.Features.Commands.ReverseInventoryTransaction;

public record ReverseInventoryCommand(
    Guid VariantId,
    string IdempotencyKey,
    string Reason,
    Guid UserId) : IRequest<ServiceResult>;