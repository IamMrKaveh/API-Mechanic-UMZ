namespace Application.Attribute.Features.Commands.UpdateAttributeType;

public class UpdateAttributeTypeHandler : IRequestHandler<UpdateAttributeTypeCommand, ServiceResult>
{
    private readonly IAttributeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAttributeTypeHandler(
        IAttributeRepository repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(
        UpdateAttributeTypeCommand request,
        CancellationToken cancellationToken
        )
    {
        var attributeType = await _repository.GetAttributeTypeByIdAsync(request.Id);
        if (attributeType == null)
            return ServiceResult.Failure("Attribute type not found.");

        if (request.Name != null && await _repository.AttributeTypeExistsAsync(request.Name, request.Id))
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("Concurrency conflict occurred.");
        }
    }
}