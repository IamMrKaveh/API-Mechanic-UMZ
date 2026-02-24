namespace Application.Order.Features.Commands.MarkOrderAsShipped;

public record MarkOrderAsShippedCommand : IRequest<ServiceResult>
{
    public int OrderId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public int UpdatedByUserId { get; init; }
}