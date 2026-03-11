using Application.Common.Models;

namespace Tests.ApplicationTest.Inventory;

public class AdjustStockHandlerTests
{
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly AdjustStockHandler _handler;

    public AdjustStockHandlerTests()
    {
        _inventoryServiceMock = new Mock<IInventoryService>();
        _auditServiceMock = new Mock<IAuditService>();

        _handler = new AdjustStockHandler(
            _inventoryServiceMock.Object,
            _auditServiceMock.Object
        );
    }

    [Fact]
    public async Task Handle_WhenServiceSucceeds_ShouldReturnSuccess()
    {
        var command = CreateValidCommand();

        _inventoryServiceMock
            .Setup(x => x.AdjustStockAsync(
                command.VariantId,
                command.QuantityChange,
                command.UserId,
                command.Notes,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenServiceFails_ShouldReturnFailure()
    {
        var command = CreateValidCommand();

        _inventoryServiceMock
            .Setup(x => x.AdjustStockAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Failure("موجودی کافی نیست"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("موجودی کافی نیست");
    }

    [Fact]
    public async Task Handle_WhenServiceSucceeds_ShouldLogAuditEvent()
    {
        var command = CreateValidCommand();

        _inventoryServiceMock
            .Setup(x => x.AdjustStockAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        await _handler.Handle(command, CancellationToken.None);

        _auditServiceMock.Verify(
            x => x.LogInventoryEventAsync(
                command.VariantId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                command.UserId),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceFails_ShouldNotLogAuditEvent()
    {
        var command = CreateValidCommand();

        _inventoryServiceMock
            .Setup(x => x.AdjustStockAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Failure("خطا"));

        await _handler.Handle(command, CancellationToken.None);

        _auditServiceMock.Verify(
            x => x.LogInventoryEventAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPassCorrectParametersToService()
    {
        var command = new AdjustStockCommand
        {
            VariantId = 5,
            QuantityChange = -3,
            Notes = "کسری موجودی",
            UserId = 10
        };

        _inventoryServiceMock
            .Setup(x => x.AdjustStockAsync(5, -3, 10, "کسری موجودی", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        await _handler.Handle(command, CancellationToken.None);

        _inventoryServiceMock.Verify(
            x => x.AdjustStockAsync(5, -3, 10, "کسری موجودی", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AdjustStockCommand CreateValidCommand() => new()
    {
        VariantId = 1,
        QuantityChange = 10,
        Notes = "افزودن موجودی",
        UserId = 1
    };
}