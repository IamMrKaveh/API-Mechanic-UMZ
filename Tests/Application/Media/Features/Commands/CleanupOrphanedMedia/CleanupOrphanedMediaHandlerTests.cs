using Application.Audit.Contracts;
using Application.Media.Contracts;
using Application.Media.Features.Commands.CleanupOrphanedMedia;
using Domain.Media.Interfaces;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Medias = Domain.Media.Aggregates.Media;

namespace Tests.Application.Media.Features.Commands.CleanupOrphanedMedia;

public class CleanupOrphanedMediaHandlerTests
{
    private readonly IMediaRepository _mediaRepository = Substitute.For<IMediaRepository>(); private readonly IStorageService _storageService = Substitute.For<IStorageService>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly CleanupOrphanedMediaHandler _sut;

    public CleanupOrphanedMediaHandlerTests()
    {
        _sut = new CleanupOrphanedMediaHandler(_mediaRepository, _storageService, _auditService);
    }

    [Fact]
    public async Task Handle_WhenNoFilePathsExist_ReturnsZeroAndDoesNotAudit()
    {
        _mediaRepository
            .GetAllFilePathsAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());

        var result = await _sut.Handle(new CleanupOrphanedMediaCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(0);

        await _storageService.DidNotReceiveWithAnyArgs().ExistsAsync(default!, default);
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
        _mediaRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenAllPathsExistInStorage_ReturnsZeroAndDoesNotAudit()
    {
        var paths = new HashSet<string> { "uploads/a/x.png", "uploads/b/y.png" };
        _mediaRepository
            .GetAllFilePathsAsync(Arg.Any<CancellationToken>())
            .Returns(paths);

        _storageService
            .ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(new CleanupOrphanedMediaCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(0);

        _mediaRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _mediaRepository.DidNotReceiveWithAnyArgs().GetByPathAsync(default!, default);
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenPathIsMissingInStorage_MarksMediaForDeletionAndUpdatesAndAudits()
    {
        var orphan = new MediaBuilder()
            .WithFilePath("uploads/orphan/photo.png")
            .WithFileName("photo.png")
            .WithFileType("image/png")
            .WithFileSize(1234)
            .WithEntityType("Product")
            .Build();

        var orphanPath = orphan.FilePath;

        _mediaRepository
            .GetAllFilePathsAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { orphanPath });

        _storageService
            .ExistsAsync(orphanPath, Arg.Any<CancellationToken>())
            .Returns(false);

        _mediaRepository
            .GetByPathAsync(orphanPath, Arg.Any<CancellationToken>())
            .Returns(new List<Medias> { orphan });

        var result = await _sut.Handle(new CleanupOrphanedMediaCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(1);

        orphan.IsDeleted.ShouldBeTrue();

        _mediaRepository.Received(1).Update(orphan);
        await _auditService.Received(1).LogSystemEventAsync(
            "OrphanedMediaCleanup",
            "1 orphaned media record(s) marked for deletion.",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMultipleOrphansShareTheSameMissingPath_MarksAllForDeletionAndAuditsAggregateCount()
    {
        var first = new MediaBuilder()
            .WithFilePath("uploads/orphan/pic.png")
            .WithFileName("pic.png")
            .WithFileType("image/png")
            .Build();
        var second = new MediaBuilder()
            .WithFilePath("uploads/orphan/pic.png")
            .WithFileName("pic.png")
            .WithFileType("image/png")
            .Build();

        var missingPath = first.FilePath;

        _mediaRepository
            .GetAllFilePathsAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { missingPath });

        _storageService
            .ExistsAsync(missingPath, Arg.Any<CancellationToken>())
            .Returns(false);

        _mediaRepository
            .GetByPathAsync(missingPath, Arg.Any<CancellationToken>())
            .Returns(new List<Medias> { first, second });

        var result = await _sut.Handle(new CleanupOrphanedMediaCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(2);

        first.IsDeleted.ShouldBeTrue();
        second.IsDeleted.ShouldBeTrue();

        _mediaRepository.Received(1).Update(first);
        _mediaRepository.Received(1).Update(second);

        await _auditService.Received(1).LogSystemEventAsync(
            "OrphanedMediaCleanup",
            "2 orphaned media record(s) marked for deletion.",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCancellationRequestedBeforeIteration_StopsWithoutFurtherStorageChecks()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mediaRepository
            .GetAllFilePathsAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "uploads/a/x.png", "uploads/b/y.png" });

        var result = await _sut.Handle(new CleanupOrphanedMediaCommand(), cts.Token);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(0);

        await _storageService.DidNotReceiveWithAnyArgs().ExistsAsync(default!, default);
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }
}
