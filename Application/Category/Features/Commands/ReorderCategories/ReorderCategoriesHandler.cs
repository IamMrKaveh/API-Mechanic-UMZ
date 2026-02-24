namespace Application.Category.Features.Commands.ReorderCategories;

public class ReorderCategoriesHandler : IRequestHandler<ReorderCategoriesCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryDomainService _categoryDomainService;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderCategoriesHandler(
        ICategoryRepository categoryRepository,
        CategoryDomainService categoryDomainService,
        IUnitOfWork unitOfWork
        )
    {
        _categoryRepository = categoryRepository;
        _categoryDomainService = categoryDomainService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(
        ReorderCategoriesCommand request,
        CancellationToken ct
        )
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
            return ServiceResult.Failure(ex.Message);
        }
    }
}