using Application.Discount.Features.Commands.ApplyDiscount;
using Application.Discount.Features.Shared;
using Domain.Discount.Aggregates;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Application.Discount.Features.Commands.ApplyDiscount;

public class ApplyDiscountHandlerTests
{
    private readonly IDiscountRepository _repository = Substitute.For<IDiscountRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ApplyDiscountHandler _sut;
    private readonly DateTime _now = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userGuid = Guid.NewGuid();

    public ApplyDiscountHandlerTests()
    {
        _sut = new ApplyDiscountHandler(_repository, _unitOfWork, _auditService, _currentUser, _clock);
        _clock.UtcNow.Returns(_now);
        _currentUser.UserId.Returns((Guid?)_userGuid);
        _unitOfWork
            .ExecuteStrategyAsync(
                Arg.Any<Func<CancellationToken, Task<ServiceResult>>>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var op = ci.Arg<Func<CancellationToken, Task<ServiceResult>>>();
                return op!(ci.Arg<CancellationToken>());
            });
    }

    private DiscountCode RedeemableCode(string code = "SAVE10")
        => new DiscountCodeBuilder().WithCode(code).WithValue(DiscountValue.Percentage(10m)).Build();

    [Fact]
    public async Task Handle_WhenCodeNotFound_ReturnsNotFound()
    {
        _repository.GetByCodeAsync("MISSING", Arg.Any<CancellationToken>()).Returns((DiscountCode?)null);

        var result = await _sut.Handle(new ApplyDiscountCommand("MISSING", 100_000m, Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        _repository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDiscountInvalid_ReturnsFailureWithoutRecordingUsage()
    {
        var expired = new DiscountCodeBuilder()
            .WithCode("OLD")
            .WithValue(DiscountValue.Percentage(10m))
            .WithStartsAt(_now.AddDays(-10))
            .WithExpiresAt(_now.AddDays(-1))
            .Build();
        _repository.GetByCodeAsync("OLD", Arg.Any<CancellationToken>()).Returns(expired);

        var result = await _sut.Handle(new ApplyDiscountCommand("OLD", 100_000m, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        expired.UsageCount.ShouldBe(0);
        _repository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenValid_RecordsUsageUpdatesAndAuditsOrderEvent()
    {
        var discount = RedeemableCode("SAVE10");
        var orderId = Guid.NewGuid();
        _repository.GetByCodeAsync("SAVE10", Arg.Any<CancellationToken>()).Returns(discount);

        var result = await _sut.Handle(new ApplyDiscountCommand("SAVE10", 200_000m, orderId), CancellationToken.None);

        result.ShouldBeSuccess();
        var typed = (ServiceResult<DiscountApplicationResult>)result;
        typed.Value!.IsSuccess.ShouldBeTrue();
        typed.Value!.DiscountAmount.ShouldBe(20_000m);
        typed.Value!.FinalAmount.ShouldBe(180_000m);
        discount.UsageCount.ShouldBe(1);
        _repository.Received(1).Update(discount);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogOrderEventAsync(
            Arg.Is<OrderId>(o => o != null && o.Value == orderId),
            "DiscountApplied",
            Arg.Any<IpAddress>(),
            Arg.Is<UserId>(u => u != null && u.Value == _userGuid),
            Arg.Is<string>(s => s != null && s.Contains("SAVE10")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_LogsSystemEventAndReturnsFailure()
    {
        _repository.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await _sut.Handle(new ApplyDiscountCommand("SAVE10", 100m, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _auditService.Received(1).LogSystemEventAsync(
            "ApplyDiscountError",
            Arg.Is<string>(s => s != null && s.Contains("db down")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenIntoStrategy()
    {
        using var cts = new CancellationTokenSource();
        _repository.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((DiscountCode?)null);

        await _sut.Handle(new ApplyDiscountCommand("X", 10m, Guid.NewGuid()), cts.Token);

        await _unitOfWork.Received(1).ExecuteStrategyAsync(
            Arg.Any<Func<CancellationToken, Task<ServiceResult>>>(),
            cts.Token);
    }
}
