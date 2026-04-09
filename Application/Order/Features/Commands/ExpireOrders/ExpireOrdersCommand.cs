namespace Application.Order.Features.Commands.ExpireOrders;

public record ExpireOrdersCommand : IRequest<ServiceResult<int>>;