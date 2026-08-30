using Application.Wallet.Features.Commands.CancelWalletTransfer;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using MediatR;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Wallet.Features.Commands.CancelWalletTransfer;

public sealed class CancelWalletTransferHandlerTests
{
    private readonly IWalletTransferRepository _transferRepository = Substitute.For<IWalletTransferRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly CancelWalletTransferHandler _sut;

    public CancelWalletTransferHandlerTests()
    {
        _sut = new CancelWalletTransferHandler(_transferRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenTransferNotFound_ReturnsNotFound()
    {
        var fromUser = UserId.NewId();
        _currentUserService.UserId.Returns(fromUser.Value);
        _transferRepository.GetByIdForUpdateAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>())
            .Returns((WalletTransfer?)null);

        var result = await _sut.Handle(new CancelWalletTransferCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotSender_ReturnsForbidden()
    {
        var fromUser = UserId.NewId();
        var otherUser = UserId.NewId();
        _currentUserService.UserId.Returns(otherUser.Value);
        var transfer = new WalletTransferBuilder().FromUser(fromUser).ToUser(UserId.NewId()).WithAmount(50_000m).Build();
        _transferRepository.GetByIdForUpdateAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>())
            .Returns(transfer);

        var result = await _sut.Handle(new CancelWalletTransferCommand(transfer.Id.Value), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenValidPendingTransfer_CancelsAndReturnsSuccess()
    {
        var fromUser = UserId.NewId();
        _currentUserService.UserId.Returns(fromUser.Value);
        var transfer = new WalletTransferBuilder().FromUser(fromUser).ToUser(UserId.NewId()).WithAmount(50_000m).Build();
        _transferRepository.GetByIdForUpdateAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>())
            .Returns(transfer);

        var result = await _sut.Handle(new CancelWalletTransferCommand(transfer.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        transfer.Status.ShouldBe(WalletTransferStatus.Cancelled);
        transfer.CancelledAt.ShouldNotBeNull();
        _transferRepository.Received(1).Update(transfer);
    }

    [Fact]
    public async Task Handle_WhenTransferAlreadyCompleted_ReturnsFailure()
    {
        var fromUser = UserId.NewId();
        _currentUserService.UserId.Returns(fromUser.Value);
        var transfer = new WalletTransferBuilder().FromUser(fromUser).ToUser(UserId.NewId()).WithAmount(50_000m).Build();
        transfer.MarkCompleted();
        _transferRepository.GetByIdForUpdateAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>())
            .Returns(transfer);

        var result = await _sut.Handle(new CancelWalletTransferCommand(transfer.Id.Value), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}

