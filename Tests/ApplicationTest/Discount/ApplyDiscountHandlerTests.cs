using Domain.Discount.Interfaces;

namespace Tests.ApplicationTest.Discount;

public class ApplyDiscountHandlerTests
{
    private readonly Mock<IDiscountRepository> _discountRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ApplyDiscountHandler _handler;

    public ApplyDiscountHandlerTests()
    {
        _discountRepositoryMock = new Mock<IDiscountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock
            .Setup(x => x.ExecuteStrategyAsync(It.IsAny<Func<Task<ServiceResult<DiscountApplyResultDto>>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<ServiceResult<DiscountApplyResultDto>>>, CancellationToken>((fn, _) => fn());

        _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IDisposable>());

        _handler = new ApplyDiscountHandler(
            _discountRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCode_ShouldReturnSuccessWithDiscountAmount()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("SAVE10")
            .WithPercentage(10)
            .Build();

        var command = new ApplyDiscountCommand("SAVE10", OrderTotal: 1000, UserId: 1);

        _discountRepositoryMock
            .Setup(x => x.GetByCodeAsync("SAVE10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        _discountRepositoryMock
            .Setup(x => x.CountUserUsageAsync(discount.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.DiscountAmount.Should().Be(100);
    }

    [Fact]
    public async Task Handle_WithNonExistentCode_ShouldReturnFailure()
    {
        var command = new ApplyDiscountCommand("INVALID", OrderTotal: 1000, UserId: 1);

        _discountRepositoryMock
            .Setup(x => x.GetByCodeAsync("INVALID", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiscountCode?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithInactiveDiscount_ShouldReturnFailure()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate();

        var command = new ApplyDiscountCommand("TESTCODE10", OrderTotal: 1000, UserId: 1);

        _discountRepositoryMock
            .Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        _discountRepositoryMock
            .Setup(x => x.CountUserUsageAsync(discount.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithExpiredDiscount_ShouldReturnFailure()
    {
        var discount = new DiscountCodeBuilder()
            .AlreadyExpired()
            .Build();

        var command = new ApplyDiscountCommand("TESTCODE10", OrderTotal: 1000, UserId: 1);

        _discountRepositoryMock
            .Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        _discountRepositoryMock
            .Setup(x => x.CountUserUsageAsync(discount.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldIncrementUsageAndSave()
    {
        var discount = new DiscountCodeBuilder().Build();
        var command = new ApplyDiscountCommand("TESTCODE10", OrderTotal: 1000, UserId: 1);

        _discountRepositoryMock
            .Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        _discountRepositoryMock
            .Setup(x => x.CountUserUsageAsync(discount.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _handler.Handle(command, CancellationToken.None);

        _discountRepositoryMock.Verify(x => x.Update(discount), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCommitTransaction()
    {
        var discount = new DiscountCodeBuilder().Build();
        var command = new ApplyDiscountCommand("TESTCODE10", OrderTotal: 1000, UserId: 1);

        _discountRepositoryMock
            .Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        _discountRepositoryMock
            .Setup(x => x.CountUserUsageAsync(discount.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUserExceedingLimit_ShouldReturnFailure()
    {
        var discount = new DiscountCodeBuilder()
            .WithMaxUsagePerUser(2)
            .Build();

        var command = new ApplyDiscountCommand("TESTCODE10", OrderTotal: 1000, UserId: 1);

        _discountRepositoryMock
            .Setup(x => x.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        _discountRepositoryMock
            .Setup(x => x.CountUserUsageAsync(discount.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidApplication_ShouldReturnCodeInResult()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("SAVE20")
            .WithPercentage(20)
            .Build();

        var command = new ApplyDiscountCommand("SAVE20", OrderTotal: 1000, UserId: 1);

        _discountRepositoryMock
            .Setup(x => x.GetByCodeAsync("SAVE20", It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        _discountRepositoryMock
            .Setup(x => x.CountUserUsageAsync(discount.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Code.Should().Be("SAVE20");
    }
}