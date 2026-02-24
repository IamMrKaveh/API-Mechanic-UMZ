namespace Application.Order.Features.Commands.RequestReturn;

public record RequestReturnCommand : IRequest<ServiceResult>
{
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string RowVersion { get; init; } = string.Empty;
}