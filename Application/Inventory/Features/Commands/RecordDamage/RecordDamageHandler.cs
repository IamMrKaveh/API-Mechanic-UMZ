namespace Application.Inventory.Features.Commands.RecordDamage;

public class RecordDamageHandler : IRequestHandler<RecordDamageCommand, ServiceResult>
{
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;

    public RecordDamageHandler(
        IInventoryService inventoryService,
        IAuditService auditService)
    {
        _inventoryService = inventoryService;
        _auditService = auditService;
    }

    public async Task<ServiceResult> Handle(RecordDamageCommand request, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.RecordDamageAsync(
            request.VariantId,
            request.Quantity,
            request.UserId,
            request.Notes,
            cancellationToken);

        if (result.IsSucceed)
        {
            await _auditService.LogInventoryEventAsync(
                request.VariantId,
                "DamageRecorded",
                $"ثبت خسارت: {request.Quantity} واحد. توضیحات: {request.Notes}",
                request.UserId);
        }

        return result;
    }
}