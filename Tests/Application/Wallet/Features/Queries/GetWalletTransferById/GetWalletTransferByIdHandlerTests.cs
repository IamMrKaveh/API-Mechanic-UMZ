using Application.Wallet.Contracts;
using Application.Wallet.Features.Queries.GetWalletTransferById;
using Application.Wallet.Features.Shared;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Wallet.Features.Queries.GetWalletTransferById;

public class GetWalletTransferByIdHandlerTests
{
    private readonly IWalletTransferQueryService _queryService = Substitute.For<IWalletTransferQueryService>();
    private readonly GetWalletTransferByIdHandler _sut;

    public GetWalletTransferByIdHandlerTests()
    {
        _sut = new GetWalletTransferByIdHandler(_queryService);
    }

    private static WalletTransferDto SampleDto(Guid id) => new(
        id,
        Guid.NewGuid(),
        "Ali",
        Guid.NewGuid(),
        "Sara",
        50_000m,
        "IRT",
        "test-desc",
        "Completed",
        0,
        DateTime.UtcNow.AddMinutes(5),
        Guid.NewGuid().ToString("N"),
        DateTime.UtcNow,
        DateTime.UtcNow,
        null,
        null);

    [Fact]
    public async Task Handle_WhenTransferNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _queryService.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((WalletTransferDto?)null);

        var result = await _sut.Handle(new GetWalletTransferByIdQuery(id), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenTransferFound_ReturnsSuccessWithDto()
    {
        var id = Guid.NewGuid();
        var dto = SampleDto(id);
        _queryService.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.Handle(new GetWalletTransferByIdQuery(id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(dto);
        result.Value.Id.ShouldBe(id);
    }

    [Fact]
    public async Task Handle_PassesRequestIdToQueryServiceVerbatim()
    {
        var id = Guid.NewGuid();
        Guid capturedId = Guid.Empty;
        _queryService
            .GetByIdAsync(Arg.Do<Guid>(g => capturedId = g), Arg.Any<CancellationToken>())
            .Returns((WalletTransferDto?)null);

        await _sut.Handle(new GetWalletTransferByIdQuery(id), CancellationToken.None);

        capturedId.ShouldBe(id);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToQueryService()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken capturedToken = default;
        _queryService
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Do<CancellationToken>(t => capturedToken = t))
            .Returns((WalletTransferDto?)null);

        await _sut.Handle(new GetWalletTransferByIdQuery(Guid.NewGuid()), cts.Token);

        capturedToken.ShouldBe(cts.Token);
    }
}
