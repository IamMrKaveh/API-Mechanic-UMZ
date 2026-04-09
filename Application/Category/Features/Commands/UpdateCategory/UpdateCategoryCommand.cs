using Application.Category.Features.Shared;

namespace Application.Category.Features.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    bool IsActive,
    string? Slug,
    string? Description,
    int SortOrder,
    string RowVersion) : IRequest<ServiceResult<CategoryDto>>;