using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategoryTree;

/// <summary>
/// ساختار درختی کامل دسته‌بندی‌ها برای منو
/// </summary>
public record GetCategoryTreeQuery : IRequest<ServiceResult<IReadOnlyList<CategoryTreeDto>>>;