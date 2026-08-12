using Application.Common.Interfaces;
using Application.Media.Contracts;
using Application.Media.Features.Commands.DeleteMedia;
using Domain.Media.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Media.Features.Commands.DeleteMedia;

public class DeleteMediaHandlerTests
{
    private readonly IMediaService _mediaService = Substitute.For<IMediaService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly DeleteMediaHandler _sut;

    public DeleteMediaHandlerTests()
    {
        _sut = new DeleteMediaHandler(_mediaService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsAuthenticated_DelegatesWithMediaIdAndUserIdBuiltFromCurrentUser()
    {
        var mediaGuid = Guid.NewGuid();
        var userGuid = Guid.NewGuid();
        _currentUserService.UserId.Returns(userGuid);

        MediaId? capturedMediaId = null;
        UserId? capturedDeletedBy = null;
        var deletedByCaptured = false;

        _mediaService
            .DeleteAsync(
                Arg.Do<MediaId>(x => capturedMediaId = x),
                Arg.Do<UserId?>(x => { capturedDeletedBy = x; deletedByCaptured = true; }),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Handle(new DeleteMediaCommand(mediaGuid), CancellationToken.None);

        result.ShouldBeSuccess();

        capturedMediaId.ShouldNotBeNull();
        capturedMediaId!.Value.ShouldBe(mediaGuid);

        deletedByCaptured.ShouldBeTrue();
        capturedDeletedBy.ShouldNotBeNull();
        capturedDeletedBy!.Value.ShouldBe(userGuid);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsNotAuthenticated_DelegatesWithNullDeletedBy()
    {
        var mediaGuid = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)null);

        UserId? capturedDeletedBy = null;
        var deletedByCaptured = false;

        _mediaService
            .DeleteAsync(
                Arg.Any<MediaId>(),
                Arg.Do<UserId?>(x => { capturedDeletedBy = x; deletedByCaptured = true; }),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Handle(new DeleteMediaCommand(mediaGuid), CancellationToken.None);

        result.ShouldBeSuccess();
        deletedByCaptured.ShouldBeTrue();
        capturedDeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsWhateverMediaServiceReturns()
    {
        _currentUserService.UserId.Returns((Guid?)null);
        var expected = ServiceResult.Failure(Error.NotFound("Media.NotFound", "not-found"));

        _mediaService
            .DeleteAsync(Arg.Any<MediaId>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new DeleteMediaCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToMediaService()
    {
        using var cts = new CancellationTokenSource();
        _currentUserService.UserId.Returns((Guid?)null);
        _mediaService
            .DeleteAsync(Arg.Any<MediaId>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        _ = await _sut.Handle(new DeleteMediaCommand(Guid.NewGuid()), cts.Token);

        await _mediaService.Received(1).DeleteAsync(Arg.Any<MediaId>(), Arg.Any<UserId?>(), cts.Token);
    }
}
