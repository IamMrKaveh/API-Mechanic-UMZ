using Domain.Common.ValueObjects;
using Domain.Discount.Interfaces;

namespace Tests.ApplicationTest.Discount;

public class CancelDiscountUsageHandlerTests
{
    private readonly Mock<IDiscountRepository> _discountRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly CancelDiscountUsageHandler _handler;

    public CancelDiscountUsageHandlerTests()
    {
        _discountRepositoryMock = new Mock<IDiscountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditServiceMock = new Mock<IAuditService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(1);

        _handler = new CancelDiscountUsageHandler(
            _discountRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _auditServiceMock.Object,
            _currentUserServiceMock.Object
        );
    }

    [Fact]
    public async Task Handle_WhenDiscountExists_ShouldReturnSuccess()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.RecordUsage(userId: 1, orderId: 42, Money.FromDecimal(100));
        var command = new CancelDiscountUsageCommand(OrderId: 42, DiscountCodeId: discount.Id);

        _discountRepositoryMock
            .Setup(x => x.GetByIdWithUsagesAsync(discount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenDiscountNotFound_ShouldReturnFailure()
    {
        var command = new CancelDiscountUsageCommand(OrderId: 42, DiscountCodeId: 999);

        _discountRepositoryMock
            .Setup(x => x.GetByIdWithUsagesAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiscountCode?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WhenDiscountFound_ShouldCallCancelUsage()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.RecordUsage(userId: 1, orderId: 42, Money.FromDecimal(100));
        var command = new CancelDiscountUsageCommand(OrderId: 42, DiscountCodeId: discount.Id);

        _discountRepositoryMock
            .Setup(x => x.GetByIdWithUsagesAsync(discount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        await _handler.Handle(command, CancellationToken.None);

        _discountRepositoryMock.Verify(x => x.Update(discount), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDiscountFound_ShouldLogAuditEvent()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.RecordUsage(userId: 1, orderId: 42, Money.FromDecimal(100));
        var command = new CancelDiscountUsageCommand(OrderId: 42, DiscountCodeId: discount.Id);

        _discountRepositoryMock
            .Setup(x => x.GetByIdWithUsagesAsync(discount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        await _handler.Handle(command, CancellationToken.None);

        _auditServiceMock.Verify(
            x => x.LogOrderEventAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ShouldNotSave()
    {
        var command = new CancelDiscountUsageCommand(OrderId: 42, DiscountCodeId: 999);

        _discountRepositoryMock
            .Setup(x => x.GetByIdWithUsagesAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiscountCode?)null);

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}