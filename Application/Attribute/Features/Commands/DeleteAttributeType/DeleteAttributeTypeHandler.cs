namespace Application.Attribute.Features.Commands.DeleteAttributeType;

public class DeleteAttributeTypeHandler : IRequestHandler<DeleteAttributeTypeCommand, ServiceResult>
{
    private readonly IAttributeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAttributeTypeHandler(
        IAttributeRepository repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(
        DeleteAttributeTypeCommand request,
        CancellationToken cancellationToken
        )
    {
        var attributeType = await _repository.GetAttributeTypeByIdAsync(request.Id);
        if (attributeType == null)
            return ServiceResult.Failure("Attribute type not found.");

        await _repository.DeleteAttributeTypeAsync(attributeType.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}