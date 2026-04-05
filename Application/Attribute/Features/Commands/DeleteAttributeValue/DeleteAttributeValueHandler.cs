using Application.Common.Results;
using Domain.Attribute.Interfaces;
using Domain.Common.Interfaces;

namespace Application.Attribute.Features.Commands.DeleteAttributeValue;

public class DeleteAttributeValueHandler(
    IAttributeRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteAttributeValueCommand, ServiceResult>
{
    private readonly IAttributeRepository _repository = repository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        DeleteAttributeValueCommand request,
        CancellationToken ct)
    {
        var attributeValue = await _repository.GetAttributeValueByIdAsync(request.Id, ct);
        if (attributeValue == null)
            return ServiceResult.NotFound("Attribute value not found.");

        await _repository.DeleteAttributeValueAsync(attributeValue.Id);
        await _unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}