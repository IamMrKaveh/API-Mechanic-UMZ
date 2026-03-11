using Domain.Discount.Interfaces;

namespace Tests.ApplicationTest.Discount;

public class DeleteDiscountHandlerTests
{
    private readonly Mock<IDiscountRepository> _discountRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly DeleteDiscountHandler _handler;

    public DeleteDiscountHandlerTests()
    {
        _discountRepositoryMock = new Mock<IDiscountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditServiceMock = new Mock<IAuditService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(1);

        _handler = new DeleteDiscountHandler(
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
        var command = new DeleteDiscountCommand(discount.Id);

        _discountRepositoryMock
            .Setup(x => x.GetByIdAsync(discount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenDiscountNotFound_ShouldReturnFailure()
    {
        var command = new DeleteDiscountCommand(999);

        _discountRepositoryMock
            .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiscountCode?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WhenDiscountExists_ShouldMarkDiscountAsDeleted()
    {
        var discount = new DiscountCodeBuilder().Build();
        var command = new DeleteDiscountCommand(discount.Id);

        _discountRepositoryMock
            .Setup(x => x.GetByIdAsync(discount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        await _handler.Handle(command, CancellationToken.None);

        discount.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenDiscountExists_ShouldCallRepositoryUpdate()
    {
        var discount = new DiscountCodeBuilder().Build();
        var command = new DeleteDiscountCommand(discount.Id);

        _discountRepositoryMock
            .Setup(x => x.GetByIdAsync(discount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        await _handler.Handle(command, CancellationToken.None);

        _discountRepositoryMock.Verify(x => x.Update(discount), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDiscountExists_ShouldCallSaveChanges()
    {
        var discount = new DiscountCodeBuilder().Build();
        var command = new DeleteDiscountCommand(discount.Id);

        _discountRepositoryMock
            .Setup(x => x.GetByIdAsync(discount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDiscountExists_ShouldCallAuditLog()
    {
        var discount = new DiscountCodeBuilder().Build();
        var command = new DeleteDiscountCommand(discount.Id);

        _discountRepositoryMock
            .Setup(x => x.GetByIdAsync(discount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(discount);

        await _handler.Handle(command, CancellationToken.None);

        _auditServiceMock.Verify(
            x => x.LogAdminEventAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ShouldNotCallUpdate()
    {
        var command = new DeleteDiscountCommand(999);

        _discountRepositoryMock
            .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiscountCode?)null);

        await _handler.Handle(command, CancellationToken.None);

        _discountRepositoryMock.Verify(x => x.Update(It.IsAny<DiscountCode>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}