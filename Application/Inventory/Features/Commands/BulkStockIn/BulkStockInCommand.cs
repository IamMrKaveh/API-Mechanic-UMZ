namespace Application.Inventory.Features.Commands.BulkStockIn;

public record BulkStockInCommand(
    ICollection<Guid> VariantIds,
    ICollection<int> Quantities,
    ICollection<string>? ReferenceNumbers,
    Guid? UserId,
    string Reason) : IRequest<ServiceResult>;