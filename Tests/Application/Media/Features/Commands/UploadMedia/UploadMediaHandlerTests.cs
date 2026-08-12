using Application.Media.Contracts;
using Application.Media.Features.Commands.UploadMedia;
using Application.Media.Features.Shared;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Media.Features.Commands.UploadMedia;

public class UploadMediaHandlerTests
{
    private readonly IMediaService _mediaService = Substitute.For<IMediaService>(); private readonly UploadMediaHandler _sut;

    public UploadMediaHandlerTests()
    {
        _sut = new UploadMediaHandler(_mediaService);
    }

    private static UploadMediaCommand BuildCommand(
        string fileName = "photo.jpg",
        string entityType = "Product",
        Guid? entityId = null,
        long fileSize = 2048,
        bool isPrimary = false,
        string? altText = null)
    {
        return new UploadMediaCommand(
            Stream.Null,
            fileName,
            "image/jpeg",
            fileSize,
            entityType,
            entityId ?? Guid.NewGuid(),
            isPrimary,
            altText);
    }

    [Fact]
    public async Task Handle_WhenFileTypeIsValidForEntity_DelegatesToMediaServiceWithConstructedValueObjects()
    {
        var command = BuildCommand(
            fileName: "photo.jpg",
            entityType: "Product",
            fileSize: 4096,
            isPrimary: true,
            altText: "alt-text");

        FilePath? capturedPath = null;
        FileSize? capturedSize = null;

        _mediaService
            .UploadAsync(
                Arg.Any<Stream>(),
                Arg.Do<FilePath>(p => capturedPath = p),
                Arg.Do<FileSize>(s => capturedSize = s),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult<MediaDto>.Failure(Error.Conflict("Media.Test.Delegated", "delegated")));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith("Media.Test.Delegated");

        capturedPath.ShouldNotBeNull();
        capturedPath!.FileName.ShouldBe("photo.jpg");
        capturedPath.Directory.ShouldBe("Product");

        capturedSize.ShouldNotBeNull();
        capturedSize!.Bytes.ShouldBe(4096);

        await _mediaService.Received(1).UploadAsync(
            command.FileStream,
            Arg.Any<FilePath>(),
            Arg.Any<FileSize>(),
            "Product",
            command.EntityId,
            true,
            "alt-text",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFileTypeIsNotImageForProductEntity_ReturnsValidationFailureAndDoesNotUpload()
    {
        var command = BuildCommand(fileName: "brochure.pdf", entityType: "Product");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);

        await _mediaService.DidNotReceiveWithAnyArgs().UploadAsync(
            default!,
            default!,
            default!,
            default!,
            default,
            default,
            default,
            default);
    }

    [Fact]
    public async Task Handle_WhenFileTypeIsUnsupportedForNonRestrictedEntity_ReturnsValidationFailureAndDoesNotUpload()
    {
        var command = BuildCommand(fileName: "installer.exe", entityType: "Other");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);

        await _mediaService.DidNotReceiveWithAnyArgs().UploadAsync(
            default!,
            default!,
            default!,
            default!,
            default,
            default,
            default,
            default);
    }

    [Fact]
    public async Task Handle_ReturnsWhateverMediaServiceReturns()
    {
        var command = BuildCommand();
        var expected = ServiceResult<MediaDto>.Failure(Error.Conflict("Media.Upload.Test", "any"));

        _mediaService
            .UploadAsync(
                Arg.Any<Stream>(),
                Arg.Any<FilePath>(),
                Arg.Any<FileSize>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToMediaService()
    {
        using var cts = new CancellationTokenSource();
        var command = BuildCommand();

        _mediaService
            .UploadAsync(
                Arg.Any<Stream>(),
                Arg.Any<FilePath>(),
                Arg.Any<FileSize>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult<MediaDto>.Failure(Error.Conflict("x", "y")));

        _ = await _sut.Handle(command, cts.Token);

        await _mediaService.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            Arg.Any<FilePath>(),
            Arg.Any<FileSize>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            cts.Token);
    }
}
