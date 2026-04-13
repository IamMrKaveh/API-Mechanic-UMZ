using Application.Category.Adapters;
using Application.Category.Features.Shared;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;
using Mapster;

namespace Application.Category.Features.Commands.UpdateCategory;

public class UpdateCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<UpdateCategoryCommand, ServiceResult<CategoryDto>>
{
    public async Task<ServiceResult<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.Id);
        var category = await categoryRepository.GetByIdAsync(categoryId, ct);

        if (category is null)
            return ServiceResult<CategoryDto>.NotFound("دسته‌بندی یافت نشد.");

        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            var rowVersion = Convert.FromBase64String(request.RowVersion);
            categoryRepository.SetOriginalRowVersion(category, rowVersion);
        }

        var name = CategoryName.Create(request.Name);
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        var uniquenessChecker = new CategoryUniquenessCheckerAdapter(categoryRepository);
        category.UpdateDetails(name, slug, uniquenessChecker, request.Description, request.SortOrder);

        if (request.IsActive && !category.IsActive)
            category.Activate();
        else if (!request.IsActive && category.IsActive)
            category.Deactivate();

        categoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "Category",
            "UpdateCategory",
            IpAddress.Unknown,
            entityType: "Category",
            entityId: request.Id.ToString(),
            ct: ct);

        var dto = category.Adapt<CategoryDto>();
        return ServiceResult<CategoryDto>.Success(dto);
    }
}