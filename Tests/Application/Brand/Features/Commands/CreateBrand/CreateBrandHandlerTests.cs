using Application.Brand.Features.Commands.CreateBrand;
using Application.Brand.Features.Shared;
using Application.Media.Contracts;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Brands = Domain.Brand.Aggregates.Brand;
using Categories = Domain.Category.Aggregates.Category;

namespace Tests.Application.Brand.Features.Commands.CreateBrand;

public class CreateBrandHandlerTests
{
    private readonly IBrandRepository _brandRepository = Substitute.For<IBrandRepository>(); private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>(); private readonly IBrandUniquenessChecker _uniquenessChecker = Substitute.For<IBrandUniquenessChecker>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly IStorageService _storageService = Substitute.For<IStorageService>(); private readonly CreateBrandHandler _sut;

    public CreateBrandHandlerTests()
    {
        _uniquenessChecker
            .IsUniqueAsync(Arg.Any<BrandName>(), Arg.Any<BrandSlug>(), Arg.Any<CategoryId>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _brandRepository
            .ExistsByNameInCategoryAsync(Arg.Any<BrandName>(), Arg.Any<CategoryId>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _brandRepository
            .ExistsBySlugAsync(Arg.Any<BrandSlug>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _mapper.Map<BrandDetailDto>(Arg.Any<Brands>()).Returns(_ => new BrandDetailDto());

        _sut = new CreateBrandHandler(
            _brandRepository,
            _categoryRepository,
            _uniquenessChecker,
            _mapper,
            _storageService);
    }

    private async Task<Categories> ConfigureExistingCategoryAsync()
    {
        var category = await new CategoryBuilder().BuildAsync();
        _categoryRepository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(category);
        return category;
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ReturnsNotFound()
    {
        _categoryRepository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((Categories?)null);

        var command = new CreateBrandCommand(
            Guid.NewGuid(), "Sony", null, null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _brandRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenBrandNameExistsInCategory_ReturnsConflict()
    {
        await ConfigureExistingCategoryAsync();
        _brandRepository
            .ExistsByNameInCategoryAsync(Arg.Any<BrandName>(), Arg.Any<CategoryId>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateBrandCommand(
            Guid.NewGuid(), "Sony", null, null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _brandRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenSlugExists_ReturnsConflict()
    {
        await ConfigureExistingCategoryAsync();
        _brandRepository
            .ExistsBySlugAsync(Arg.Any<BrandSlug>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateBrandCommand(
            Guid.NewGuid(), "Sony", "sony", null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _brandRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenLogoFileSizeExceedsLimit_ReturnsValidation()
    {
        await ConfigureExistingCategoryAsync();
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var command = new CreateBrandCommand(
            Guid.NewGuid(), "Sony", null, null,
            stream, "logo.png", "image/png", (2 * 1024 * 1024) + 1);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _brandRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _storageService.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default, default);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData("image/bmp")]
    public async Task Handle_WhenLogoContentTypeNotAllowed_ReturnsValidation(string contentType)
    {
        await ConfigureExistingCategoryAsync();
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var command = new CreateBrandCommand(
            Guid.NewGuid(), "Sony", null, null,
            stream, "logo.bin", contentType, 512);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _storageService.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default, default);
        await _brandRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WithValidCommandAndNoLogo_ReturnsSuccessAndAddsBrand()
    {
        await ConfigureExistingCategoryAsync();
        var expected = new BrandDetailDto { Name = "Sony" };
        _mapper.Map<BrandDetailDto>(Arg.Any<Brands>()).Returns(expected);

        var command = new CreateBrandCommand(
            Guid.NewGuid(), "Sony", null, "Description", null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        await _brandRepository.Received(1).AddAsync(Arg.Any<Brands>(), Arg.Any<CancellationToken>());
        await _storageService.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default, default);
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    public async Task Handle_WithValidLogo_UploadsThroughStorageServiceAndSetsLogoPath(string contentType, string extension)
    {
        await ConfigureExistingCategoryAsync();
        var uploadedPath = $"brands/logo{extension}";
        _storageService
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(uploadedPath);

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new CreateBrandCommand(
            Guid.NewGuid(), "Sony", null, null,
            stream, $"logo{extension}", contentType, 3);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _storageService.Received(1).UploadAsync(
            stream,
            Arg.Is<string>(f => f.StartsWith("brands/") && f.EndsWith(extension)),
            contentType,
            "brands",
            Arg.Any<CancellationToken>());
        await _brandRepository.Received(1).AddAsync(
            Arg.Is<Brands>(b => b.LogoPath == uploadedPath),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyLogoPlaceholder_DoesNotUploadLogo()
    {
        await ConfigureExistingCategoryAsync();
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var command = new CreateBrandCommand(
            Guid.NewGuid(), "Sony", null, null,
            stream, "__EMPTY__", "image/png", 3);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _storageService.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default, default);
        await _brandRepository.Received(1).AddAsync(
            Arg.Is<Brands>(b => b.LogoPath == null),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenSlugMissing_GeneratesSlugFromName(string? slug)
    {
        await ConfigureExistingCategoryAsync();

        var command = new CreateBrandCommand(
            Guid.NewGuid(), "Home Appliances", slug, null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _brandRepository.Received(1).AddAsync(
            Arg.Is<Brands>(b => b.Slug.Value == "home-appliances"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProvidedSlug_UsesSlugFromCommand()
    {
        await ConfigureExistingCategoryAsync();

        var command = new CreateBrandCommand(
            Guid.NewGuid(), "Sony", "my-sony", null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _brandRepository.Received(1).AddAsync(
            Arg.Is<Brands>(b => b.Slug.Value == "my-sony"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWhitespaceDescription_StoresNullDescription()
    {
        await ConfigureExistingCategoryAsync();

        var command = new CreateBrandCommand(
            Guid.NewGuid(), "Sony", null, "   ", null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _brandRepository.Received(1).AddAsync(
            Arg.Is<Brands>(b => b.Description == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCategoryIdBuiltFromRequestToCategoryRepository()
    {
        await ConfigureExistingCategoryAsync();
        var categoryGuid = Guid.NewGuid();
        CategoryId? captured = null;
        _categoryRepository
            .GetByIdAsync(Arg.Do<CategoryId>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(await new CategoryBuilder().BuildAsync());

        var command = new CreateBrandCommand(
            categoryGuid, "Sony", null, null, null, null, null, null);

        _ = await _sut.Handle(command, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(categoryGuid);
    }
}
