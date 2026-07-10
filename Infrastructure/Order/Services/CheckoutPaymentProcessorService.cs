using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;
using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.Exceptions;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;

namespace Infrastructure.Order.Services;

public sealed class CheckoutPaymentProcessorService(IPaymentService paymentService)
    : ICheckoutPaymentProcessorService
{
    public async Task<ServiceResult<CheckoutResultDto>> ProcessAsync(
        CheckoutResultDto orderResult,
        string? paymentMethod,
        string ipAddress,
        string? userAgent,
        Guid userId,
        CancellationToken ct)
    {
        if (orderResult.FinalAmount <= 0)
            return ServiceResult<CheckoutResultDto>.Success(orderResult);

        PaymentInitiationResult paymentResult;
        try
        {
            paymentResult = await paymentService.InitiatePaymentAsync(
                OrderId.From(orderResult.OrderId),
                Money.Create(orderResult.FinalAmount),
                IpAddress.Create(ipAddress),
                UserId.From(userId),
                "",
                ct);
        }
        catch (OrderNotFoundException ex)
        {
            return ServiceResult<CheckoutResultDto>.NotFound(ex.Message);
        }
        catch (OrderAlreadyPaidException ex)
        {
            return ServiceResult<CheckoutResultDto>.Conflict(ex.Message);
        }
        catch (ExternalServiceException ex)
        {
            return ServiceResult<CheckoutResultDto>.Failure(ex.Message);
        }

        return ServiceResult<CheckoutResultDto>.Success(orderResult with
        {
            PaymentUrl = paymentResult.PaymentUrl,
            PaymentAuthority = paymentResult.Authority
        });
    }
}