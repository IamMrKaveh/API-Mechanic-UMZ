using Application.Common.Results;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
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
        var attributeTypeId = AttributeTypeId.From(request.Id);

        var attributeType = await _repository.GetAttributeTypeByIdAsync(attributeTypeId, ct);
        if (attributeType is null)
            return ServiceResult.NotFound("Attribute type not found.");

        await _repository.DeleteAttributeTypeAsync(attributeType.Id, null, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}