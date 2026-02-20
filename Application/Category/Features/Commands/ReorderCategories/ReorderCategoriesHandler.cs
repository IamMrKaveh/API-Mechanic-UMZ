namespace Application.Category.Features.Commands.ReorderCategories;

public class ReorderCategoriesHandler : IRequestHandler<ReorderCategoriesCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryDomainService _categoryDomainService;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderCategoriesHandler(
        ICategoryRepository categoryRepository,
        CategoryDomainService categoryDomainService,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _categoryDomainService = categoryDomainService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(
        ReorderCategoriesCommand request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllActiveAsync(cancellationToken);

        try
        {
            // تغییر ترتیب از طریق Domain Service
            _categoryDomainService.ReorderCategories(categories, request.OrderedCategoryIds);

            foreach (var category in categories)
            {
                _categoryRepository.Update(category);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}