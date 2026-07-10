using Application.Common.Options;
using Application.Payment.Features.Shared;
using Application.Wallet.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Interfaces;
using Microsoft.Extensions.Options;

namespace Application.Wallet.Features.Commands.InitiateWalletTopUp;

public sealed class InitiateWalletTopUpHandler(
    IWalletTopUpRepository topUpRepository,
    IPaymentGatewayFactory gatewayFactory,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDistributedLock distributedLock,
    IOptions<ApiBaseUrlOptions> apiOptions)
    : IRequestHandler<InitiateWalletTopUpCommand, ServiceResult<InitiateTopUpResultDto>>
{
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);
    private readonly ApiBaseUrlOptions _apiOptions = apiOptions.Value;

    public async Task<ServiceResult<InitiateTopUpResultDto>> Handle(
        InitiateWalletTopUpCommand request,
        CancellationToken ct)
    {
        var lockKey = $"wallet:topup:initiate:{currentUserService.UserId}";
        await using var lockHandle = await distributedLock.AcquireAsync(lockKey, LockExpiry, ct);
        if (lockHandle is null || !lockHandle.IsAcquired)
        {
            return ServiceResult<InitiateTopUpResultDto>.Failure(
                "درخواست شارژ قبلی هنوز در حال پردازش است. لطفاً چند لحظه بعد تلاش کنید.");
        }

        try
        {
            var userId = UserId.From(currentUserService.UserId.Value);
            var amount = Money.Create(request.Amount);
            var gatewayName = string.IsNullOrWhiteSpace(request.Gateway) ? "zarinpal" : request.Gateway;

            var topUp = WalletTopUp.Initiate(userId, amount, gatewayName);
            await topUpRepository.AddAsync(topUp, ct);
            await unitOfWork.SaveChangesAsync(ct);

            var callbackUrl = BuildCallbackUrl();
            var description = $"شارژ کیف پول - {request.Amount:N0} تومان";
            var gateway = gatewayFactory.GetGateway(gatewayName);
            var syntheticOrderId = OrderId.From(topUp.Id.Value);

            PaymentInitiationResult? initValue = null;
            string? initError = null;

            try
            {
                initValue = await gateway.InitiateAsync(
                    syntheticOrderId, amount, description, callbackUrl, ct: ct);
            }
            catch (ExternalServiceException ex)
            {
                initError = ex.Message;
            }
            catch (Exception ex)
            {
                initError = ex.Message;
            }

            if (initValue is null)
            {
                topUp.MarkFailed(initError ?? "خطا در ارتباط با درگاه.");
                topUpRepository.Update(topUp);
                await unitOfWork.SaveChangesAsync(ct);

                await auditService.LogErrorAsync(
                    $"WalletTopUp initiation failed. TopUpId={topUp.Id.Value}, Reason={initError}", ct);

                return ServiceResult<InitiateTopUpResultDto>.Failure(
                    initError ?? "امکان ایجاد درخواست پرداخت وجود ندارد.");
            }

            topUp.MarkAuthorityIssued(initValue.Authority);
            topUpRepository.Update(topUp);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<InitiateTopUpResultDto>.Success(new InitiateTopUpResultDto
            {
                TopUpId = topUp.Id.Value,
                PaymentUrl = initValue.PaymentUrl,
                Authority = initValue.Authority,
                Gateway = gateway.GatewayName,
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
        var apiBase = string.IsNullOrWhiteSpace(_apiOptions.PublicBaseUrl)
            ? "https://localhost:44318"
            : _apiOptions.PublicBaseUrl;
        return $"{apiBase.TrimEnd('/')}/api/v1/wallet/topup/callback";
    }
}