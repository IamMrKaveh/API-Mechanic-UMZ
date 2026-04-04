using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public class UpdateOrderStatusDefinitionHandler(IOrderStatusRepository orderStatusRepository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateOrderStatusDefinitionCommand, ServiceResult>
{
    private readonly IOrderStatusRepository _orderStatusRepository = orderStatusRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        UpdateOrderStatusDefinitionCommand request,
        CancellationToken ct)
    {
        var status = await _orderStatusRepository.GetByIdAsync(request.Id, ct);
        if (status == null)
            return ServiceResult.NotFound("وضعیت یافت نشد.");

        status.Update(
            request.Dto.DisplayName ?? status.DisplayName,
            request.Dto.Icon ?? status.Icon,
            request.Dto.Color ?? status.Color,
            request.Dto.SortOrder ?? status.SortOrder,
            request.Dto.AllowCancel ?? status.AllowCancel,
            request.Dto.AllowEdit ?? status.AllowEdit
        );

        _orderStatusRepository.Update(status);
        await _unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}