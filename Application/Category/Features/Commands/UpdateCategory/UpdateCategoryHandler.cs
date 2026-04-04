using Application.Common.Exceptions;
using Application.Common.Results;
using Application.Media.Contracts;
using Domain.Category.Interfaces;
using Domain.Common.Interfaces;

namespace Application.Category.Features.Commands.UpdateCategory;

public class UpdateCategoryHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMediaService mediaService,
    IMediaQueryService mediaQueryService,
    ILogger<UpdateCategoryHandler> logger) : IRequestHandler<UpdateCategoryCommand, ServiceResult>
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMediaService _mediaService = mediaService;
    private readonly IMediaQueryService _mediaQueryService = mediaQueryService;
    private readonly ILogger<UpdateCategoryHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        UpdateCategoryCommand request,
        CancellationToken ct)
    {
        var category = await _categoryRepository.GetByIdWithGroupsAsync(request.Id, ct);
        if (category == null)
            return ServiceResult.NotFound("دسته‌بندی یافت نشد.");

        _categoryRepository.SetOriginalRowVersion(category, Convert.FromBase64String(request.RowVersion));

        if (await _categoryRepository.ExistsByNameAsync(request.Name.Trim(), request.Id, ct))
        {
            return ServiceResult.Conflict("دسته‌بندی با این نام قبلاً وجود دارد.");
        }

        category.Update(request.Name, request.Description, request.SortOrder);

        Domain.Media.Aggregates.Media? newMedia = null;
        if (request.IconFile != null)
        {
            var existingMedia = (await _mediaQueryService.GetEntityMediaAsync("Category", request.Id))
                .FirstOrDefault(m => m.IsPrimary);

            if (existingMedia != null)
                await _mediaService.DeleteMediaAsync(existingMedia.Id);

            newMedia = await _mediaService.AttachFileToEntityAsync(
                request.IconFile.Content,
                request.IconFile.FileName,
                request.IconFile.ContentType,
                request.IconFile.Length,
                "Category",
                request.Id,
                isPrimary: true);
        }

        _categoryRepository.Update(category);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            if (newMedia != null)
                await _mediaService.DeleteMediaAsync(newMedia.Id);

            return ServiceResult.Conflict("این رکورد توسط کاربر دیگری تغییر یافته است. لطفاً مجدداً تلاش کنید.");
        }
    }
}