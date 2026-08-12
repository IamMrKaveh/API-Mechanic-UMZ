using Application.Media.Contracts;
using Application.Media.Features.Commands.SetPrimaryMedia;
using Domain.Media.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Media.Features.Commands.SetPrimaryMedia;

public class SetPrimaryMediaHandlerTests
{
    private readonly IMediaService _mediaService = Substitute.For<IMediaService>(); private readonly SetPrimaryMediaHandler _sut;

    public SetPrimaryMediaHandlerTests()
    {
        _sut = new SetPrimaryMediaHandler(_mediaService);
    }

    [Fact]
    public async Task Handle_DelegatesToMediaServiceWithMediaIdBuiltFromRequestId()
    {
        var mediaGuid = Guid.NewGuid();
        MediaId? captured = null;

        _mediaService
            .SetAsPrimaryAsync(Arg.Do<MediaId>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Handle(new SetPrimaryMediaCommand(mediaGuid), CancellationToken.None);

        result.ShouldBeSuccess();
        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(mediaGuid);
    }

    [Fact]
    public async Task Handle_ReturnsWhateverMediaServiceReturns()
    {
        var expected = ServiceResult.Failure(Error.NotFound("Media.SetPrimary.NotFound", "not-found"));

        _mediaService
            .SetAsPrimaryAsync(Arg.Any<MediaId>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new SetPrimaryMediaCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToMediaService()
    {
        using var cts = new CancellationTokenSource();
        _mediaService
            .SetAsPrimaryAsync(Arg.Any<MediaId>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        _ = await _sut.Handle(new SetPrimaryMediaCommand(Guid.NewGuid()), cts.Token);

        await _mediaService.Received(1).SetAsPrimaryAsync(Arg.Any<MediaId>(), cts.Token);
    }
}
