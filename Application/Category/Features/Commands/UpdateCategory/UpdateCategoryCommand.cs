using Application.Category.Features.Shared;
using Application.Common.Results;

namespace Application.Category.Features.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Slug,
    string? Description,
    int SortOrder,
    bool IsActive,
    Guid? ParentCategoryId) : IRequest<ServiceResult<CategoryDto>>;