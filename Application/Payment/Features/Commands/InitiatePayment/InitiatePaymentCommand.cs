using Application.Common.Results;
using Application.Payment.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Payment.Features.Commands.InitiatePayment;

public record InitiatePaymentCommand(
    PaymentInitiationDto Dto,
    UserId UserId,
    string IpAddress) : IRequest<ServiceResult<PaymentResultDto>>;