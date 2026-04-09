using Application.Category.Features.Shared;

namespace Application.Category.Features.Commands.CreateCategory;

public record CreateCategoryCommand(
    string CategoryName,
    string? Slug,
    string? Description,
    int SortOrder = 0) : IRequest<ServiceResult<CategoryDto>>;