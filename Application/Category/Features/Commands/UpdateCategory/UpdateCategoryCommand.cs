using Application.Category.Features.Shared;
using Application.Common.Results;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Category.Features.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    CategoryId Id,
    CategoryName Name,
    bool IsActive,
    Slug? Slug,
    string? Description,
    int SortOrder,
    string RowVersion) : IRequest<ServiceResult<CategoryDto>>;