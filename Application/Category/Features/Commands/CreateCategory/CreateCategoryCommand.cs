using Application.Category.Features.Shared;
using Application.Common.Results;

namespace Application.Category.Features.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string? Slug,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder) : IRequest<ServiceResult<CategoryDto>>;