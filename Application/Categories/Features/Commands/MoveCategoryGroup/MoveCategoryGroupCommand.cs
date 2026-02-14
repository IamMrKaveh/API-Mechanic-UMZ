namespace Application.Categories.Features.Commands.MoveCategoryGroup;

public record MoveCategoryGroupCommand(
    int SourceCategoryId,
    int TargetCategoryId,
    int GroupId) : IRequest<ServiceResult>;