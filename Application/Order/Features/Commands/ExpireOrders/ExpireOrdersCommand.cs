namespace Application.Order.Features.Commands.ExpireOrders;

public record ExpireOrdersCommand
    : ICommand<int>;