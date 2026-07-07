using Application.Wallet.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Commands.InitiateTopUp;

public sealed class InitiateWalletTopUpHandler(
    IWalletTopUpRepository topUpRepository,
    IPaymentGatewayFactory gatewayFactory,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : IRequestHandler<InitiateWalletTopUpCommand, ServiceResult<InitiateTopUpResultDto>>
{
    public async Task<ServiceResult<InitiateTopUpResultDto>> Handle(
        InitiateWalletTopUpCommand request,
        CancellationToken ct)
    {
        try
        {
            var userId = UserId.From(request.UserId);
            var amount = Money.Create(request.Amount);
            var topUp = WalletTopUp.Initiate(userId, amount, request.Gateway);

            await topUpRepository.AddAsync(topUp, ct);

            var callbackUrl = BuildCallbackUrl();
            var description = $"شارژ کیف پول - {request.Amount:N0} تومان";

            var gateway = gatewayFactory.GetGateway(request.Gateway);
            var syntheticOrderId = OrderId.From(topUp.Id.Value);

            var initResult = await gateway.InitiateAsync(
                syntheticOrderId,
                amount,
                description,
                callbackUrl,
                ct: ct);

            if (initResult.IsFailed || initResult.Value is null)
            {
                topUp.MarkFailed(initResult.Error ?? "خطا در ارتباط با درگاه.");
                topUpRepository.Update(topUp);
                await unitOfWork.SaveChangesAsync(ct);

                await auditService.LogErrorAsync(
                    $"WalletTopUp initiation failed. TopUpId={topUp.Id}, Reason={initResult.Error}",
                    ct);

                return ServiceResult<InitiateTopUpResultDto>.Failure(
                    initResult.Error ?? "امکان ایجاد درخواست پرداخت وجود ندارد.");
            }

            topUp.MarkAuthorityIssued(initResult.Value.Authority);
            topUpRepository.Update(topUp);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<InitiateTopUpResultDto>.Success(new InitiateTopUpResultDto
            {
                TopUpId = topUp.Id.Value,
                PaymentUrl = initResult.Value.PaymentUrl,
                Authority = initResult.Value.Authority,
                Gateway = topUp.Gateway,
                Amount = amount.Amount
            });
        }
        catch (DomainException ex)
        {
            return ServiceResult<InitiateTopUpResultDto>.Failure(ex.Message);
        }
    }

    private string BuildCallbackUrl()
    {
        var frontendBase = currentUserService.FrontendBaseUrl?.TrimEnd('/') ?? string.Empty;
        return $"{frontendBase}/api/v1/wallet/topup/callback";
    }
}