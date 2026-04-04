using Application.Common.Results;
using Domain.Attribute.Interfaces;

namespace Application.Attribute.Features.Commands.DeleteAttributeValue;

public class DeleteAttributeValueHandler : IRequestHandler<DeleteAttributeValueCommand, ServiceResult>
{
    private readonly IAttributeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAttributeValueHandler(
        IAttributeRepository repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(
        DeleteAttributeValueCommand request,
        CancellationToken ct)
    {
        var attributeValue = await _repository.GetAttributeValueByIdAsync(request.Id);
        if (attributeValue == null)
            return ServiceResult.NotFound("Attribute value not found.");

        await _repository.DeleteAttributeValueAsync(attributeValue.Id);
        await _unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}