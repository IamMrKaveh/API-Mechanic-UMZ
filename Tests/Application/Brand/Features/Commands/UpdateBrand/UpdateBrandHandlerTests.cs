using Application.Brand.Contracts;
using Application.Brand.Features.Commands.UpdateBrand;
using Application.Brand.Features.Shared;
using Application.Cache.Contracts;
using Application.Common.Interfaces;
using Application.Media.Contracts;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Brands = Domain.Brand.Aggregates.Brand;

namespace Tests.Application.Brand.Features.Commands.UpdateBrand;

public class UpdateBrandHandlerTests
{
    private readonly IBrandRepository _brandRepository = Substitute.For<IBrandRepository>(); private readonly IBrandQueryService _brandQueryService = Substitute.For<IBrandQueryService>(); private readonly IBrandUniquenessChecker _uniquenessChecker = Substitute.For<IBrandUniquenessChecker>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IStorageService _storageService = Substitute.For<IStorageService>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly UpdateBrandHandler _sut;

    public UpdateBrandHandlerTests()
    {
        _uniquenessChecker
            .IsUniqueAsync(Arg.Any<BrandName>(), Arg.Any<BrandSlug>(), Arg.Any<CategoryId>(), Arg.Any<BrandId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _sut = new UpdateBrandHandler(
            _brandRepository,
            _brandQueryService,
            _uniquenessChecker,
            _unitOfWork,
            _storageService,
            _cacheService);
    }

    private async Task<Brands> BuildBrandAsync()
    {
        return await new BrandBuilder()
            .WithName("Original")
            .WithSlug("original")
            .BuildAsync();
    }

    private async Task<Brands> ConfigureExistingBrandAsync()
    {
        var brand = await BuildBrandAsync();
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(brand);
        _brandQueryService
            .GetBrandDetailAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(new BrandDetailDto { Id = brand.Id.Value });
        return brand;
    }

    [Fact]
    public async Task Handle_WhenBrandNotFound_ReturnsNotFound()
    {
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns((Brands?)null);

        var command = new UpdateBrandCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Renamed", null, null, null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _brandRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenLogoFileSizeExceedsLimit_ReturnsValidation()
    {
        await ConfigureExistingBrandAsync();
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var command = new UpdateBrandCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Renamed", null, null,
            stream, "logo.png", "image/png", (2 * 1024 * 1024) + 1, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _storageService.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default, default);
        _brandRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    public async Task Handle_WhenLogoContentTypeNotAllowed_ReturnsValidation(string contentType)
    {
        await ConfigureExistingBrandAsync();
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var command = new UpdateBrandCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Renamed", null, null,
            stream, "logo.bin", contentType, 3, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _storageService.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default, default);
        _brandRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
    }

    [Fact]
    public async Task Handle_WithValidCommandAndNoLogo_ReturnsSuccessAndPersistsChanges()
    {
        var brand = await ConfigureExistingBrandAsync();

        var command = new UpdateBrandCommand(
            brand.Id.Value, Guid.NewGuid(), "Renamed", "renamed", "desc",
            null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        brand.Name.Value.ShouldBe("Renamed");
        brand.Slug.Value.ShouldBe("renamed");
        brand.Description.ShouldBe("desc");
        _brandRepository.Received(1).Update(brand, Arg.Any<byte[]?>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveByPrefixAsync("brands:", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidBase64RowVersion_PassesDecodedRowVersionToUpdate()
    {
        var brand = await ConfigureExistingBrandAsync();
        var expected = new byte[] { 1, 2, 3, 4 };
        var rowVersion = Convert.ToBase64String(expected);

        var command = new UpdateBrandCommand(
            brand.Id.Value, Guid.NewGuid(), "Renamed", "renamed", null,
            null, null, null, null, rowVersion);

        await _sut.Handle(command, CancellationToken.None);

        _brandRepository.Received(1).Update(
            brand,
            Arg.Is<byte[]>(rv => rv != null && rv.SequenceEqual(expected)));
    }

    [Fact]
    public async Task Handle_WithNullRowVersion_PassesNullRowVersionToUpdate()
    {
        var brand = await ConfigureExistingBrandAsync();

        var command = new UpdateBrandCommand(
            brand.Id.Value, Guid.NewGuid(), "Renamed", "renamed", null,
            null, null, null, null, null);

        await _sut.Handle(command, CancellationToken.None);

        _brandRepository.Received(1).Update(
            brand,
            Arg.Is<byte[]?>(rv => rv == null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenSlugMissing_GeneratesSlugFromName(string? slug)
    {
        var brand = await ConfigureExistingBrandAsync();

        var command = new UpdateBrandCommand(
            brand.Id.Value, Guid.NewGuid(), "Home Appliances", slug, null,
            null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        brand.Slug.Value.ShouldBe("home-appliances");
    }

    [Fact]
    public async Task Handle_WithValidLogo_UploadsThroughStorageServiceAndAssignsLogoPath()
    {
        var brand = await ConfigureExistingBrandAsync();
        _storageService
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("brands/uploaded.png");

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UpdateBrandCommand(
            brand.Id.Value, Guid.NewGuid(), "Renamed", "renamed", null,
            stream, "logo.png", "image/png", 3, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _storageService.Received(1).UploadAsync(
            stream,
            Arg.Is<string>(f => f!.StartsWith("brands/") && f!.EndsWith(".png")),
            "image/png",
            "brands",
            Arg.Any<CancellationToken>());
        brand.LogoPath.ShouldBe("brands/uploaded.png");
    }

    [Fact]
    public async Task Handle_WithEmptyLogoPlaceholder_DoesNotUploadLogo()
    {
        var brand = await ConfigureExistingBrandAsync();
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var command = new UpdateBrandCommand(
            brand.Id.Value, Guid.NewGuid(), "Renamed", "renamed", null,
            stream, "__EMPTY__", "image/png", 3, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _storageService.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default!, default, default);
        brand.LogoPath.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WhenBrandDetailNotFoundAfterSave_ReturnsNotFound()
    {
        var brand = await BuildBrandAsync();
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(brand);
        _brandQueryService
            .GetBrandDetailAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns((BrandDetailDto?)null);

        var command = new UpdateBrandCommand(
            brand.Id.Value, Guid.NewGuid(), "Renamed", "renamed", null,
            null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccess_ReturnsDtoFromBrandQueryService()
    {
        var brand = await BuildBrandAsync();
        _brandRepository
            .GetByIdAsync(Arg.Any<BrandId>(), Arg.Any<CancellationToken>())
            .Returns(brand);
        var expected = new BrandDetailDto { Id = brand.Id.Value, Name = "Renamed" };
        _brandQueryService
            .GetBrandDetailAsync(brand.Id, Arg.Any<CancellationToken>())
            .Returns(expected);

        var command = new UpdateBrandCommand(
            brand.Id.Value, Guid.NewGuid(), "Renamed", "renamed", null,
            null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }
}
