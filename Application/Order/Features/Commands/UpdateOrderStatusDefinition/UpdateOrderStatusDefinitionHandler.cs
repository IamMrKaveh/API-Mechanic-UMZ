using Application.Common.Models;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public class UpdateOrderStatusDefinitionHandler(IOrderStatusRepository repo, IUnitOfWork unitOfWork) : IRequestHandler<UpdateOrderStatusDefinitionCommand, ServiceResult>
{
    private readonly IOrderStatusRepository _repo = repo;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        UpdateOrderStatusDefinitionCommand request,
        CancellationToken ct)
    {
        var status = await _repo.GetByIdAsync(request.Id, ct);
        if (status == null) return ServiceResult.Failure("وضعیت یافت نشد.", 404);

        status.Update(
            request.Dto.DisplayName ?? status.DisplayName,
            request.Dto.Icon ?? status.Icon,
            request.Dto.Color ?? status.Color,
            request.Dto.SortOrder ?? status.SortOrder,
            request.Dto.AllowCancel ?? status.AllowCancel,
            request.Dto.AllowEdit ?? status.AllowEdit
        );

        _repo.Update(status);
        await _unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}