using Application.Category.Adapters;
using Application.Category.Features.Shared;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Application.Category.Features.Commands.CreateCategory;

public sealed class CreateCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<ServiceResult<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var name = CategoryName.Create(request.CategoryName);
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? CategorySlug.GenerateFrom(request.CategoryName)
            : CategorySlug.FromString(request.Slug);

        var categoryId = CategoryId.NewId();
        var uniquenessChecker = new CategoryUniquenessCheckerAdapter(categoryRepository);

        var category = await Domain.Category.Aggregates.Category.Create(
            categoryId,
            name,
            slug,
            uniquenessChecker,
            request.Description,
            request.SortOrder,
            ct);

        await categoryRepository.AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = category.Adapt<CategoryDto>();
        return ServiceResult<CategoryDto>.Success(dto);
    }
}