using Application.Common.Results;

namespace Application.Category.Features.Commands.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : IRequest<ServiceResult>;