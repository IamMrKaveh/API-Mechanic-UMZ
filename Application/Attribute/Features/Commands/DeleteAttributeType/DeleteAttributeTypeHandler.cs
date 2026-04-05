using Application.Common.Results;
using Domain.Attribute.Interfaces;
using Domain.Common.Interfaces;

namespace Application.Attribute.Features.Commands.DeleteAttributeType;

public class DeleteAttributeTypeHandler(
    IAttributeRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteAttributeTypeCommand, ServiceResult>
{
    private readonly IAttributeRepository _repository = repository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        DeleteAttributeTypeCommand request,
        CancellationToken ct)
    {
        var attributeType = await _repository.GetAttributeTypeByIdAsync(request.Id, ct);
        if (attributeType == null)
            return ServiceResult.NotFound("Attribute type not found.");

        await _repository.DeleteAttributeTypeAsync(attributeType.Id);
        await _unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}