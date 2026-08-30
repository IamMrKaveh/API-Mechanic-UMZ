using Application.Wallet.Features.Commands.MarkFraudAlertReviewed;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Wallet.Features.Commands.MarkFraudAlertReviewed;

public sealed class MarkFraudAlertReviewedHandlerTests
{
    private readonly IWalletFraudAlertRepository _repository = Substitute.For<IWalletFraudAlertRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly MarkFraudAlertReviewedHandler _sut;

    public MarkFraudAlertReviewedHandlerTests()
    {
        _sut = new MarkFraudAlertReviewedHandler(_repository, _unitOfWork, _auditService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenAlertNotFound_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _repository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>())
            .Returns((WalletFraudAlert?)null);

        var result = await _sut.Handle(new MarkFraudAlertReviewedCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenAlertOpen_MarksReviewedAndReturnsSuccess()
    {
        var adminId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
        var alert = new WalletFraudAlertBuilder().Build();
        _repository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>()).Returns(alert);

        var result = await _sut.Handle(new MarkFraudAlertReviewedCommand(alert.Id.Value, "checked"), CancellationToken.None);

        result.ShouldBeSuccess();
        alert.Status.ShouldBe(FraudAlertStatus.Reviewed);
        alert.ReviewNote.ShouldBe("checked");
        alert.ReviewedBy.ShouldBe(adminId);
        _repository.Received(1).Update(alert);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "FraudAlertReviewed", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlertAlreadyDismissed_ReturnsFailure()
    {
        var adminId = UserId.NewId();
        _currentUserService.UserId.Returns(adminId.Value);
        var alert = new WalletFraudAlertBuilder().Build();
        alert.Dismiss(adminId, null);
        _repository.GetByIdAsync(Arg.Any<WalletFraudAlertId>(), Arg.Any<CancellationToken>()).Returns(alert);

        var result = await _sut.Handle(new MarkFraudAlertReviewedCommand(alert.Id.Value, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}

