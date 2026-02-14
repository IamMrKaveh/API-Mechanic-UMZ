namespace Application.Categories.Features.Commands.DeleteCategory;

public record DeleteCategoryCommand(int Id, int? DeletedBy = null) : IRequest<ServiceResult>;