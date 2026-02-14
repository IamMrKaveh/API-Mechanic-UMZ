namespace Application.Categories.Features.Commands.DeleteCategoryGroup;

public record DeleteCategoryGroupCommand(
    int CategoryId,
    int GroupId,
    int? DeletedBy = null) : IRequest<ServiceResult>;