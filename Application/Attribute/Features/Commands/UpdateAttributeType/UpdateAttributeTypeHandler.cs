using Application.Common.Exceptions;
using Application.Common.Results;
using Domain.Attribute.Interfaces;
using Domain.Common.Interfaces;

namespace Application.Attribute.Features.Commands.UpdateAttributeType;

public class UpdateAttributeTypeHandler(
    IAttributeRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateAttributeTypeCommand, ServiceResult>
{
    private readonly IAttributeRepository _repository = repository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        UpdateAttributeTypeCommand request,
        CancellationToken ct)
    {
        var attributeType = await _repository.GetAttributeTypeByIdAsync(request.Id);
        if (attributeType is null)
            return ServiceResult.Failure("Attribute type not found.");

        if (request.Name is not null && await _repository.AttributeTypeExistsAsync(request.Name, request.Id))
        {
            return ServiceResult.Failure("Attribute type name already exists.");
        }

        attributeType.Update(
                    request.Name ?? attributeType.Name,
                    request.DisplayName ?? attributeType.DisplayName,
                    request.SortOrder ?? attributeType.SortOrder,
                    request.IsActive ?? attributeType.IsActive
                );

        try
        {
            await _repository.UpdateAttributeTypeAsync(attributeType);
            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("Concurrency conflict occurred.");
        }
    }
}