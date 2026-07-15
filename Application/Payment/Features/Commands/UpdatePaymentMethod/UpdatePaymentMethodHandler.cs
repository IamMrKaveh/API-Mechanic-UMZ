using Application.Payment.Features.Shared;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Features.Commands.UpdatePaymentMethod;

public sealed class UpdatePaymentMethodHandler(
    IPaymentMethodRepository repository,
    IMapper mapper)
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

            var method = await repository.GetByIdAsync(id, ct);
            if (method is null)
                return ServiceResult<PaymentMethodDto>.NotFound("روش پرداخت یافت نشد.");

            if (await repository.ExistsByNameAsync(name, id, ct))
                return ServiceResult<PaymentMethodDto>.Conflict("روش پرداخت با این نام قبلاً ثبت شده است.");

            method.Update(name, fee, request.Description, request.IconUrl, request.SortOrder);

            repository.Update(method);

            return ServiceResult<PaymentMethodDto>.Success(mapper.Map<PaymentMethodDto>(method));
        }
        catch (DomainException ex)
        {
            return ServiceResult<PaymentMethodDto>.Validation(ex.Message);
        }
    }
}