namespace Application.Category.Features.Commands.DeleteCategory;

public record DeleteCategoryCommand(
    int Id,
    int? DeletedBy = null
    ) : IRequest<ServiceResult>;