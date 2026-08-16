using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Media.Contracts;
using Domain.Media.Interfaces;
using Domain.Media.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Media.Services;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Medias = Domain.Media.Aggregates.Media;

namespace Tests.Infrastructure.Media.Services;

public class MediaServiceTests
{
    private readonly IMediaRepository _mediaRepository = Substitute.For<IMediaRepository>(); private readonly IStorageService _storageService = Substitute.For<IStorageService>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly MediaService _sut;

    public MediaServiceTests()
    {
        _sut = new MediaService(_mediaRepository, _storageService, _auditService, _unitOfWork);
    }

    private static Stream NonEmptyStream() => new MemoryStream(new byte[] { 0x1, 0x2, 0x3 });

    private static FilePath BuildFilePath(string directory = "uploads", string fileName = "photo.jpg")
        => FilePath.CreateForUpload(directory, fileName);

    private static FileSize BuildFileSize(long bytes = 1024) => FileSize.Create(bytes);

    private void ArrangeSuccessfulUpload(string storedPath, string publicUrl)
    {
        _mediaRepository
            .GetByEntityAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Medias>());
        _storageService
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(storedPath);
        _storageService.GetPublicUrl(Arg.Any<string>()).Returns(publicUrl);
    }

    [Fact]
    public async Task UploadAsync_WhenStorageSucceeds_PersistsMediaAndReturnsSuccessWithDto()
    {
        var entityId = Guid.NewGuid();
        var storedPath = "cdn/product/photo.jpg";
        ArrangeSuccessfulUpload(storedPath, "https://cdn.test/cdn/product/photo.jpg");

        var result = await _sut.UploadAsync(
            NonEmptyStream(), BuildFilePath("uploads", "photo.jpg"), BuildFileSize(2048),
            entityType: "Product", entityId: entityId,
            isPrimary: true, altText: "alt");

        result.ShouldBeSuccess();
        result.Value.EntityType.ShouldBe("Product");
        result.Value.EntityId.ShouldBe(entityId);
        result.Value.FileType.ShouldBe("image/jpeg");
        result.Value.FileSize.ShouldBe(2048);
        result.Value.SortOrder.ShouldBe(0);
        result.Value.IsPrimary.ShouldBeTrue();
        result.Value.AltText.ShouldBe("alt");
        result.Value.PublicUrl.ShouldBe("https://cdn.test/cdn/product/photo.jpg");
        result.Value.FilePath.ShouldBe(storedPath);
    }

    [Fact]
    public async Task UploadAsync_ComputesSortOrderFromExistingCount()
    {
        var entityId = Guid.NewGuid();
        var existing = new List<Medias>
    {
        new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(0).Build(),
        new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(1).Build(),
    };
        _mediaRepository
            .GetByEntityAsync("Product", entityId, Arg.Any<CancellationToken>())
            .Returns(existing);
        _storageService
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("stored/photo.jpg");
        _storageService.GetPublicUrl(Arg.Any<string>()).Returns("https://cdn.test/stored/photo.jpg");

        var result = await _sut.UploadAsync(
            NonEmptyStream(), BuildFilePath(), BuildFileSize(),
            entityType: "Product", entityId: entityId);

        result.ShouldBeSuccess();
        result.Value.SortOrder.ShouldBe(2);
    }

    [Theory]
    [InlineData("Product", "product")]
    [InlineData("  Brand  ", "brand")]
    [InlineData("Category/Sub", "category/sub")]
    public async Task UploadAsync_NormalizesEntityTypeIntoLowercaseFolderForStorage(string entityType, string expectedFolder)
    {
        var entityId = Guid.NewGuid();
        ArrangeSuccessfulUpload("stored/photo.jpg", "https://cdn.test/stored/photo.jpg");

        await _sut.UploadAsync(
            NonEmptyStream(), BuildFilePath(), BuildFileSize(),
            entityType: entityType, entityId: entityId);

        await _storageService.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            expectedFolder,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_InvokesRepositoryAddAndUnitOfWorkSaveChanges()
    {
        var entityId = Guid.NewGuid();
        ArrangeSuccessfulUpload("stored/photo.jpg", "https://cdn.test/stored/photo.jpg");

        await _sut.UploadAsync(
            NonEmptyStream(), BuildFilePath(), BuildFileSize(),
            entityType: "Product", entityId: entityId);

        await _mediaRepository.Received(1).AddAsync(Arg.Any<Medias>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenMediaNotFound_ReturnsNotFoundResult()
    {
        _mediaRepository
            .GetByIdAsync(Arg.Any<MediaId>(), Arg.Any<CancellationToken>())
            .Returns((Medias?)null);

        var result = await _sut.DeleteAsync(MediaId.NewId());

        result.ShouldFailWith(ErrorCode.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenMediaExistsAndWasNotPrimary_MarksMediaForDeletionWithoutPromotingAnotherPrimary()
    {
        var entityId = Guid.NewGuid();
        var media = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithIsPrimary(false).Build();
        _mediaRepository.GetByIdAsync(media.Id, Arg.Any<CancellationToken>()).Returns(media);

        var result = await _sut.DeleteAsync(media.Id, deletedBy: UserId.NewId());

        result.ShouldBeSuccess();
        media.IsDeleted.ShouldBeTrue();
        media.IsActive.ShouldBeFalse();
        _mediaRepository.Received(1).Update(media);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mediaRepository.DidNotReceive().GetByEntityAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenMediaWasPrimary_PromotesNextEligibleMediaAsPrimary()
    {
        var entityId = Guid.NewGuid();
        var primary = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).Build();
        primary.SetAsPrimary();

        var candidateA = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(1).Build();
        var candidateB = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(0).Build();

        _mediaRepository.GetByIdAsync(primary.Id, Arg.Any<CancellationToken>()).Returns(primary);
        _mediaRepository
            .GetByEntityAsync("Product", entityId, Arg.Any<CancellationToken>())
            .Returns([primary, candidateA, candidateB]);

        var result = await _sut.DeleteAsync(primary.Id);

        result.ShouldBeSuccess();
        primary.IsDeleted.ShouldBeTrue();
        candidateB.IsPrimary.ShouldBeTrue();
        candidateA.IsPrimary.ShouldBeFalse();
        _mediaRepository.Received(1).Update(candidateB);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenMediaWasPrimaryAndNoOtherActiveMediaRemain_DoesNotPromoteAnyPrimary()
    {
        var entityId = Guid.NewGuid();
        var primary = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).Build();
        primary.SetAsPrimary();

        _mediaRepository.GetByIdAsync(primary.Id, Arg.Any<CancellationToken>()).Returns(primary);
        _mediaRepository
            .GetByEntityAsync("Product", entityId, Arg.Any<CancellationToken>())
            .Returns([primary]);

        var result = await _sut.DeleteAsync(primary.Id);

        result.ShouldBeSuccess();
        primary.IsDeleted.ShouldBeTrue();
        primary.IsPrimary.ShouldBeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsPrimaryAsync_WhenMediaNotFound_ReturnsNotFound()
    {
        _mediaRepository.GetByIdAsync(Arg.Any<MediaId>(), Arg.Any<CancellationToken>()).Returns((Medias?)null);

        var result = await _sut.SetAsPrimaryAsync(MediaId.NewId());

        result.ShouldFailWith(ErrorCode.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsPrimaryAsync_WhenNoCurrentPrimary_MarksTargetAsPrimary()
    {
        var entityId = Guid.NewGuid();
        var target = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithIsPrimary(false).Build();
        _mediaRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _mediaRepository
            .GetPrimaryByEntityAsync("Product", entityId, Arg.Any<CancellationToken>())
            .Returns((Medias?)null);

        var result = await _sut.SetAsPrimaryAsync(target.Id);

        result.ShouldBeSuccess();
        target.IsPrimary.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsPrimaryAsync_WhenCurrentPrimaryIsDifferent_RemovesPreviousPrimaryAndPromotesTarget()
    {
        var entityId = Guid.NewGuid();
        var currentPrimary = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).Build();
        currentPrimary.SetAsPrimary();
        var target = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithIsPrimary(false).Build();

        _mediaRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _mediaRepository
            .GetPrimaryByEntityAsync("Product", entityId, Arg.Any<CancellationToken>())
            .Returns(currentPrimary);

        var result = await _sut.SetAsPrimaryAsync(target.Id);

        result.ShouldBeSuccess();
        currentPrimary.IsPrimary.ShouldBeFalse();
        target.IsPrimary.ShouldBeTrue();
        _mediaRepository.Received(1).Update(currentPrimary);
        _mediaRepository.Received(1).Update(target);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsPrimaryAsync_WhenTargetIsAlreadyTheCurrentPrimary_DoesNotRemovePrimaryFromItself()
    {
        var entityId = Guid.NewGuid();
        var target = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).Build();
        target.SetAsPrimary();

        _mediaRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _mediaRepository
            .GetPrimaryByEntityAsync("Product", entityId, Arg.Any<CancellationToken>())
            .Returns(target);

        var result = await _sut.SetAsPrimaryAsync(target.Id);

        result.ShouldBeSuccess();
        target.IsPrimary.ShouldBeTrue();
        _mediaRepository.Received(1).Update(target);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderAsync_UpdatesSortOrderForKnownMediasInProvidedOrderAndSkipsUnknownIds()
    {
        var entityId = Guid.NewGuid();
        var mediaA = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(5).Build();
        var mediaB = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(9).Build();
        var unknownId = Guid.NewGuid();

        _mediaRepository
            .GetByEntityAsync("Product", entityId, Arg.Any<CancellationToken>())
            .Returns([mediaA, mediaB]);

        var result = await _sut.ReorderAsync("Product", entityId, [mediaB.Id.Value, unknownId, mediaA.Id.Value]);

        result.ShouldBeSuccess();
        mediaB.SortOrder.ShouldBe(0);
        mediaA.SortOrder.ShouldBe(1);
        _mediaRepository.Received(1).Update(mediaA);
        _mediaRepository.Received(1).Update(mediaB);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderAsync_WithEmptyOrderedIds_LeavesSortOrderUntouchedButPersistsChanges()
    {
        var entityId = Guid.NewGuid();
        var mediaA = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(5).Build();

        _mediaRepository
            .GetByEntityAsync("Product", entityId, Arg.Any<CancellationToken>())
            .Returns([mediaA]);

        var result = await _sut.ReorderAsync("Product", entityId, Array.Empty<Guid>());

        result.ShouldBeSuccess();
        mediaA.SortOrder.ShouldBe(5);
        _mediaRepository.DidNotReceive().Update(Arg.Any<Medias>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
