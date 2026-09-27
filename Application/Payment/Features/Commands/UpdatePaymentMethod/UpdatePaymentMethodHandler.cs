using Application.Payment.Features.Shared;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Features.Commands.UpdatePaymentMethod;

public sealed class UpdatePaymentMethodHandler(
    IPaymentMethodRepository repository,
    IMapper mapper,
    ICacheService cacheService)
    : ICommandHandler<UpdatePaymentMethodCommand, PaymentMethodDto>
{
    public async Task<ServiceResult<PaymentMethodDto>> Handle(
        UpdatePaymentMethodCommand request,
        CancellationToken ct)
    {
        try
        {
            var id = PaymentMethodId.From(request.Id);
            var name = PaymentMethodName.Create(request.Name);
            var fee = PaymentMethodFee.Create(request.FeeAmount, request.FeePercentage);

            var methodResult = await (repository.GetByIdAsync(id, ct)).OrNotFoundAsync("روش پرداخت یافت نشد.");
            if (methodResult.IsFailure) return methodResult.Error;
            var method = methodResult.Value;

            if (await repository.ExistsByNameAsync(name, id, ct))
                return ServiceResult<PaymentMethodDto>.Conflict("روش پرداخت با این نام قبلاً ثبت شده است.");

            method.Update(name, fee, request.Description, request.IconUrl, request.SortOrder);

            repository.Update(method);
            await cacheService.RemoveByPrefixAsync("payment-methods:", ct);

            return ServiceResult<PaymentMethodDto>.Success(mapper.Map<PaymentMethodDto>(method));
        }
        catch (DomainException ex)
        {
            return ServiceResult<PaymentMethodDto>.Validation(ex.Message);
        }
    }
}