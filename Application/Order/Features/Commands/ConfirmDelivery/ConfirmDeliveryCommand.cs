namespace Application.Order.Features.Commands.ConfirmDelivery;

public record ConfirmDeliveryCommand : IRequest<ServiceResult>
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}