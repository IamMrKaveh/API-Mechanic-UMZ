namespace Application.Variant.Features.Commands.AddStock;

public class AddStockHandler : IRequestHandler<AddStockCommand, ServiceResult>
{
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;

    public AddStockHandler(
        IInventoryService inventoryService,
        IAuditService auditService)
    {
        _inventoryService = inventoryService;
        _auditService = auditService;
    }

    public async Task<ServiceResult> Handle(
        AddStockCommand request,
        CancellationToken ct
        )
    {
        var result = await _inventoryService.BulkStockInAsync(
            [(
            request.VariantId,
            request.Quantity,
            Notes: (string?)(request.Notes ?? "افزایش موجودی"))],
            request.UserId,
            ct: ct);

        if (result.IsFailed)
            return ServiceResult.Failure(result.Error!);

        var (_, IsSuccess, Error, NewStock) = result.Data!.Results.FirstOrDefault();

        if (!IsSuccess)
            return ServiceResult.Failure(Error ?? "خطا در افزایش موجودی.");

        await _auditService.LogInventoryEventAsync(
            request.VariantId,
            "AddStock",
            $"Added {request.Quantity} units via AddStockCommand.",
            request.UserId);

        return ServiceResult.Success();
    }
}