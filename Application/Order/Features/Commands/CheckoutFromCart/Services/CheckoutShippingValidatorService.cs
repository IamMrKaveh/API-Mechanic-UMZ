using Domain.Shipping.Interfaces;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public sealed class CheckoutShippingValidatorService(IShippingRepository shippingRepository) : ICheckoutShippingValidatorService
{
    private readonly IShippingRepository _shippingRepository = shippingRepository;

    public async Task<ServiceResult<Domain.Shipping.Aggregates.Shipping>> ValidateAsync(int shippingId, CancellationToken ct)
    {
        var shippingMethod = await _shippingRepository.GetByIdAsync(shippingId, ct);
        if (shippingMethod == null || !shippingMethod.IsActive)
            return ServiceResult<Domain.Shipping.Aggregates.Shipping>.Failure("روش ارسال انتخاب شده معتبر نیست.");
        return ServiceResult<Domain.Shipping.Aggregates.Shipping>.Success(shippingMethod);
    }
}