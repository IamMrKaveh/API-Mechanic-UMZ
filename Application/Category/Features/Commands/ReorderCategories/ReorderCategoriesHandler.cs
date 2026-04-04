using Application.Common.Results;
using Domain.Category.Interfaces;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;

namespace Application.Category.Features.Commands.ReorderCategories;

public class ReorderCategoriesHandler(
    ICategoryRepository categoryRepository,
    CategoryDomainService categoryDomainService,
    IUnitOfWork unitOfWork) : IRequestHandler<ReorderCategoriesCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly CategoryDomainService _categoryDomainService = categoryDomainService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        ReorderCategoriesCommand request,
        CancellationToken ct)
    {
        var categories = await _categoryRepository.GetAllActiveAsync(ct);

        try
        {
            _categoryDomainService.ReorderCategories(categories, request.OrderedCategoryIds);

            foreach (var category in categories)
            {
                _categoryRepository.Update(category);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }
    }
}