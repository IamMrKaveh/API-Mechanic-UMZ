using Application.Order.Features.Commands.CheckoutFromCart;
using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;

namespace Infrastructure.Order.Services;

public class CheckoutOrchestrationService(
    ICheckoutAddressResolverService addressResolver,
    ICheckoutCartItemBuilderService cartItemBuilder,
    ICheckoutShippingValidatorService shippingValidator,
    ICheckoutDiscountApplicatorService discountApplicator,
    ICheckoutStockValidatorService stockValidator,
    ICheckoutPriceValidatorService priceValidator,
    ICheckoutOrderCreationService orderCreation,
    ICheckoutPaymentProcessorService paymentProcessor,
    ICartRepository cartRepository) : ICheckoutOrchestrationService
{
    public async Task<ServiceResult<CheckoutResultDto>> ProcessCheckoutAsync(
        CheckoutFromCartCommand command, CancellationToken ct)
    {
        var addressResult = await addressResolver.ResolveAsync(command.UserId, command.AddressId, ct);
        if (!addressResult.IsSuccess)
            return ServiceResult<CheckoutResultDto>.Failure(addressResult.Error!);

        var cartItemsResult = await cartItemBuilder.BuildAsync(command.CartId, command.UserId, ct);
        if (!cartItemsResult.IsSuccess)
            return ServiceResult<CheckoutResultDto>.Failure(cartItemsResult.Error!);

        var stockResult = await stockValidator.ValidateAsync(cartItemsResult.Value!.Items, ct);
        if (!stockResult.IsSuccess)
            return ServiceResult<CheckoutResultDto>.Failure(stockResult.Error!);

        var priceResult = await priceValidator.ValidateAsync(cartItemsResult.Value!.Items, ct);
        if (!priceResult.IsSuccess)
            return ServiceResult<CheckoutResultDto>.Failure(priceResult.Error!);

        var shippingResult = await shippingValidator.ValidateAndCalculateCostAsync(
            command.ShippingId, cartItemsResult.Value!.SubTotal, ct);
        if (!shippingResult.IsSuccess)
            return ServiceResult<CheckoutResultDto>.Failure(shippingResult.Error!);

        var discountResult = await discountApplicator.ApplyAsync(
            command.DiscountCode, Money.FromDecimal(cartItemsResult.Value!.SubTotal), command.UserId, ct);
        if (!discountResult.IsSuccess)
            return ServiceResult<CheckoutResultDto>.Failure(discountResult.Error!);

        var (receiverInfo, deliveryAddress) = addressResult.Value!;
        var (discountAmount, discountCodeId) = discountResult.Value!;

        var orderResult = await orderCreation.CreateAsync(
            command.UserId,
            receiverInfo,
            deliveryAddress,
            cartItemsResult.Value!.Items,
            shippingResult.Value!,
            discountAmount,
            discountCodeId,
            command.IdempotencyKey,
            ct);

        if (!orderResult.IsSuccess)
            return ServiceResult<CheckoutResultDto>.Failure(orderResult.Error!);

        var cart = await cartRepository.FindByIdAsync(CartId.From(command.CartId), ct);
        if (cart is not null)
        {
            cart.Checkout();
            cartRepository.Update(cart);
        }

        return await paymentProcessor.ProcessAsync(
            orderResult.Value!,
            command.PaymentMethod,
            command.IpAddress,
            command.UserAgent,
            ct);
    }
}