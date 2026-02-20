using Application.Category.Contracts;

namespace Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandHandler : IRequestHandler<MoveBrandCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryDomainService _categoryDomainService;
    private readonly IUnitOfWork _unitOfWork;

    public MoveBrandHandler(
        ICategoryRepository categoryRepository,
        CategoryDomainService categoryDomainService,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _categoryDomainService = categoryDomainService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(
        MoveBrandCommand request, CancellationToken cancellationToken)
    {
        // بارگذاری هر دو Aggregate
        var sourceCategory = await _categoryRepository.GetByIdWithGroupsAsync(
            request.SourceCategoryId, cancellationToken);

        if (sourceCategory == null)
            return ServiceResult.Failure("دسته‌بندی مبدأ یافت نشد.", 404);

        var targetCategory = await _categoryRepository.GetByIdWithGroupsAsync(
            request.TargetCategoryId, cancellationToken);

        if (targetCategory == null)
            return ServiceResult.Failure("دسته‌بندی مقصد یافت نشد.", 404);

        try
        {
            // انتقال از طریق Domain Service
            _categoryDomainService.MoveGroup(sourceCategory, targetCategory, request.GroupId);

            _categoryRepository.Update(sourceCategory);
            _categoryRepository.Update(targetCategory);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }
        catch (BrandNotFoundException)
        {
            return ServiceResult.Failure("گروه یافت نشد.", 404);
        }
        catch (DuplicateBrandNameException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}