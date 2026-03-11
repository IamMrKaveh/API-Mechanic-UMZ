using Application.Common.Models;

namespace Application.Inventory.Features.Commands.CommitStockForOrder;

public record CommitStockForOrderCommand(int OrderId) : IRequest<ServiceResult>;