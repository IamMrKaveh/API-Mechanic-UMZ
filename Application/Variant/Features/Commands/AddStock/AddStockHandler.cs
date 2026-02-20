namespace Application.Variant.Features.Commands.AddStock;

/// <summary>
/// به‌جای دستکاری مستقیم variant، از IInventoryService.AdjustStockAsync عبور می‌کند
/// تا تراکنش موجودی همیشه در لجر ثبت شود و لجر انبار کامل بماند.
/// </summary>
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

    public async Task<ServiceResult> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.BulkStockInAsync(
            new[]
            {
                new BulkStockInItemDto
                {
                    VariantId = request.VariantId,
                    Quantity = request.Quantity,
                    Notes = request.Notes ?? "افزایش موجودی"
                }
            },
            request.UserId,
            ct: cancellationToken);

        if (result.IsFailed)
            return ServiceResult.Failure(result.Error!);

        var itemResult = result.Data!.Results.FirstOrDefault();
        if (itemResult?.IsSuccess == false)
            return ServiceResult.Failure(itemResult.Error ?? "خطا در افزایش موجودی.");

        await _auditService.LogInventoryEventAsync(
            request.VariantId,
            "AddStock",
            $"Added {request.Quantity} units via AddStockCommand.",
            request.UserId);

        return ServiceResult.Success();
    }
}