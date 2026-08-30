using Application.Wallet.Features.Commands.DismissFraudAlert;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Wallet.Features.Commands.DismissFraudAlert;

public sealed class DismissFraudAlertHandlerTests
{
    private readonly IWalletFraudAlertRepository _repository = Substitute.For<IWalletFraudAlertRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly DismissFraudAlertHandler _sut;

    public DismissFraudAlertHandlerTests()
    {
        _sut = new DismissFraudAlertHandler(_repository, _unitOfWork, _auditService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenAlertNotFound_ReturnsNotFound()
    {
        var adminId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
        _repository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>())
            .Returns((WalletFraudAlert?)null);

        var result = await _sut.Handle(new DismissFraudAlertCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenAlertOpen_DismissesAndReturnsSuccess()
    {
        var adminId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
        var alert = new WalletFraudAlertBuilder().Build();
        _repository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>()).Returns(alert);

        var result = await _sut.Handle(new DismissFraudAlertCommand(alert.Id.Value, "false positive"), CancellationToken.None);

        result.ShouldBeSuccess();
        alert.Status.ShouldBe(FraudAlertStatus.Dismissed);
        alert.ReviewedBy.ShouldBe(adminId);
        alert.ReviewNote.ShouldBe("false positive");
        _repository.Received(1).Update(alert);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "FraudAlertDismissed", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlertAlreadyReviewed_ReturnsFailure()
    {
        var adminId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
        var alert = new WalletFraudAlertBuilder().Build();
        alert.MarkAsReviewed(adminId, "already handled");
        _repository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>()).Returns(alert);

        var result = await _sut.Handle(new DismissFraudAlertCommand(alert.Id.Value, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}

