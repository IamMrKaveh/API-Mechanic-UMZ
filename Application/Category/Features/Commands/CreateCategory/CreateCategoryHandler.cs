using Application.Category.Adapters;
using Application.Category.Features.Shared;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Mapster;

namespace Application.Category.Features.Commands.CreateCategory;

public class CreateCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<CreateCategoryCommand, ServiceResult<CategoryDto>>
{
    public async Task<ServiceResult<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var name = CategoryName.Create(request.CategoryName);
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.CategoryName)
            : Slug.FromString(request.Slug);

        var categoryId = CategoryId.NewId();
        var uniquenessChecker = new CategoryUniquenessCheckerAdapter(categoryRepository);
        var category = Domain.Category.Aggregates.Category.Create(categoryId, name, slug, uniquenessChecker, request.Description, request.SortOrder);

        await categoryRepository.AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "Category",
            "CreateCategory",
            IpAddress.Unknown,
            entityType: "Category",
            entityId: category.Id.Value.ToString(),
            ct: ct);

        var dto = category.Adapt<CategoryDto>();
        return ServiceResult<CategoryDto>.Success(dto);
    }
}