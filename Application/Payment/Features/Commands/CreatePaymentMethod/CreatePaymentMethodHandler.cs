using Application.Payment.Features.Shared;
using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Features.Commands.CreatePaymentMethod;

public sealed class CreatePaymentMethodHandler(
    IPaymentMethodRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : ICommandHandler<CreatePaymentMethodCommand, PaymentMethodDto>
{
    public async Task<ServiceResult<PaymentMethodDto>> Handle(
        CreatePaymentMethodCommand request,
        CancellationToken ct)
    {
        try
        {
            var name = PaymentMethodName.Create(request.Name);
            var code = PaymentMethodCode.Create(request.Code);
            var fee = PaymentMethodFee.Create(request.FeeAmount, request.FeePercentage);

            if (await repository.ExistsByNameAsync(name, null, ct))
                return ServiceResult<PaymentMethodDto>.Conflict("روش پرداخت با این نام قبلاً ثبت شده است.");

            if (await repository.ExistsByCodeAsync(code, null, ct))
                return ServiceResult<PaymentMethodDto>.Conflict("روش پرداخت با این کد قبلاً ثبت شده است.");

            var method = PaymentMethod.Create(
                name,
                code,
                fee,
                request.Description,
                request.IconUrl,
                request.SortOrder);

            await repository.AddAsync(method, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<PaymentMethodDto>.Success(mapper.Map<PaymentMethodDto>(method));
        }
        catch (DomainException ex)
        {
            return ServiceResult<PaymentMethodDto>.Validation(ex.Message);
        }
    }
}