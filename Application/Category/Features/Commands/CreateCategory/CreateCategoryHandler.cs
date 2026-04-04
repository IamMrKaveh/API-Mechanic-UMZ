using Application.Common.Results;
using Application.Media.Contracts;
using Domain.Category.Interfaces;
using Domain.Common.Interfaces;

namespace Application.Category.Features.Commands.CreateCategory;

public class CreateCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMediaService mediaService,
    ILogger<CreateCategoryHandler> logger) : IRequestHandler<CreateCategoryCommand, ServiceResult<int>>
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMediaService _mediaService = mediaService;
    private readonly ILogger<CreateCategoryHandler> _logger = logger;

    public async Task<ServiceResult<int>> Handle(
        CreateCategoryCommand request,
        CancellationToken ct)
    {
        if (await _categoryRepository.ExistsByNameAsync(request.Name.Trim(), ct: ct))
        {
            return ServiceResult<int>.Conflict("دسته‌بندی با این نام قبلاً وجود دارد.");
        }

        var category = Domain.Category.Aggregates.Category.Create(request.Name, request.Description);

        await _categoryRepository.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        if (request.IconFile != null)
        {
            try
            {
                await _mediaService.AttachFileToEntityAsync(
                    request.IconFile.Content,
                    request.IconFile.FileName,
                    request.IconFile.ContentType,
                    request.IconFile.Length,
                    "Category",
                    category.Id,
                    isPrimary: true);

                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در آپلود آیکون دسته‌بندی {CategoryId}", category.Id);
            }
        }

        return ServiceResult<int>.Success(category.Id);
    }
}