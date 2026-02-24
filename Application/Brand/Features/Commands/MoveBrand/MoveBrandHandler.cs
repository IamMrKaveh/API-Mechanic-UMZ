namespace Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandHandler : IRequestHandler<MoveBrandCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryDomainService _categoryDomainService;
    private readonly IUnitOfWork _unitOfWork;

    public MoveBrandHandler(
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
        MoveBrandCommand request,
        CancellationToken ct
        )
    {
        var sourceCategory = await _categoryRepository.GetByIdWithGroupsAsync(
            request.SourceCategoryId, ct);

        if (sourceCategory == null)
            return ServiceResult.Failure("دسته‌بندی مبدأ یافت نشد.", 404);

        var targetCategory = await _categoryRepository.GetByIdWithGroupsAsync(
            request.TargetCategoryId, ct);

        if (targetCategory == null)
            return ServiceResult.Failure("دسته‌بندی مقصد یافت نشد.", 404);

        try
        {
            _categoryDomainService.MoveGroup(sourceCategory, targetCategory, request.BrandId);

            _categoryRepository.Update(sourceCategory);
            _categoryRepository.Update(targetCategory);

            await _unitOfWork.SaveChangesAsync(ct);
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