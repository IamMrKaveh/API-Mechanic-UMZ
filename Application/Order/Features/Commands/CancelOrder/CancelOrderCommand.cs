namespace Application.Order.Features.Commands.CancelOrder;

public record CancelOrderCommand : IRequest<ServiceResult>
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public bool IsAdmin { get; init; }
    public string Reason { get; init; } = string.Empty;
}