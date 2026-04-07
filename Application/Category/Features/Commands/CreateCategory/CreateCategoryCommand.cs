using Application.Category.Features.Shared;
using Application.Common.Results;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Category.Features.Commands.CreateCategory;

public record CreateCategoryCommand(
    CategoryName CategoryName,
    Slug? Slug,
    string? Description,
    CategoryId? ParentCategoryId,
    int SortOrder = 0) : IRequest<ServiceResult<CategoryDto>>;