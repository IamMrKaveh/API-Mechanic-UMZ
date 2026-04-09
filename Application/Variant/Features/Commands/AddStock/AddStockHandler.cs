namespace Application.Variant.Features.Commands.AddStock;

public class AddStockHandler(
    IInventoryService inventoryService,
    IAuditService auditService) : IRequestHandler<AddStockCommand, ServiceResult>
{
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly IAuditService _auditService = auditService;

    public async Task<ServiceResult> Handle(
        AddStockCommand request,
        CancellationToken ct)
    {
        var result = await _inventoryService.BulkStockInAsync(
            [(
            request.VariantId,
            request.Quantity,
            Notes: (string?)(request.Notes ?? "افزایش موجودی"))],
            request.UserId,
            ct: ct);

        if (result.IsFailure)
            return ServiceResult.Unexpected(result.Error.Message!);

        var (_, IsSuccess, Error, NewStock) = result.Value!.Results.FirstOrDefault();

        if (!IsSuccess)
            return ServiceResult.Unexpected(Error ?? "خطا در افزایش موجودی.");

        await _auditService.LogInventoryEventAsync(
            request.VariantId,
            "AddStock",
            $"Added {request.Quantity} units via AddStockCommand.",
            request.UserId);

        return ServiceResult.Success();
    }
}