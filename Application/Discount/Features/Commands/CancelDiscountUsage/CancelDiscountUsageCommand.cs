using Application.Common.Results;

namespace Application.Discount.Features.Commands.CancelDiscountUsage;

public record CancelDiscountUsageCommand(Guid DiscountCodeId, string OrderId) : IRequest<ServiceResult>;