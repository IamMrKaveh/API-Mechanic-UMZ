using Application.Common.Models;

namespace Application.Inventory.Features.Commands.ReconcileStock;

public record ReconcileStockCommand(int VariantId, int UserId) : IRequest<ServiceResult<ReconcileResultDto>>;