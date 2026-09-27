using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.MarkOrderAsShipped;

public class MarkOrderAsShippedHandler(
    IOrderRepository orderRepository)
    : ICommandHandler<MarkOrderAsShippedCommand>
{
    public async Task<ServiceResult> Handle(MarkOrderAsShippedCommand request, CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var orderResult = await (orderRepository.FindByIdAsync(orderId, ct)).OrNotFoundAsync("سفارش یافت نشد.");
        if (orderResult.IsFailure) return orderResult.Error;
        var order = orderResult.Value;

        byte[]? rowVersion = null;
        if (!string.IsNullOrEmpty(request.RowVersion))
        {
            try
            {
                rowVersion = Convert.FromBase64String(request.RowVersion);
            }
            catch (FormatException)
            {
                return ServiceResult.Validation("If-Match نامعتبر است.");
            }
        }

        try
        {
            order.MarkAsShipped();
            orderRepository.Update(order, rowVersion);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("این سفارش توسط کاربر دیگری تغییر کرده است.");
        }
    }
}
