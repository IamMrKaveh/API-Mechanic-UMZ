namespace Application.Category.Features.Commands.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : IRequest<ServiceResult>;