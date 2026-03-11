using Domain.Discount.Interfaces;

namespace Tests.ApplicationTest.Discount;

public class CreateDiscountHandlerTests
{
    private readonly Mock<IDiscountRepository> _discountRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IHtmlSanitizer> _htmlSanitizerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CreateDiscountHandler _handler;

    public CreateDiscountHandlerTests()
    {
        _discountRepositoryMock = new Mock<IDiscountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _htmlSanitizerMock = new Mock<IHtmlSanitizer>();
        _auditServiceMock = new Mock<IAuditService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _mapperMock = new Mock<IMapper>();

        _htmlSanitizerMock
            .Setup(x => x.Sanitize(It.IsAny<string>()))
            .Returns<string>(s => s);

        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns(1);

        _handler = new CreateDiscountHandler(
            _discountRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _htmlSanitizerMock.Object,
            _auditServiceMock.Object,
            _currentUserServiceMock.Object,
            _mapperMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccessResult()
    {
        var command = CreateValidCommand();

        _discountRepositoryMock
            .Setup(x => x.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _discountRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DiscountCode>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mapperMock
            .Setup(x => x.Map<DiscountCodeDto>(It.IsAny<DiscountCode>()))
            .Returns(new DiscountCodeDto());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithDuplicateCode_ShouldReturnFailureResult()
    {
        var command = CreateValidCommand();

        _discountRepositoryMock
            .Setup(x => x.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithDuplicateCode_ShouldNotCallRepository_Save()
    {
        var command = CreateValidCommand();

        _discountRepositoryMock
            .Setup(x => x.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallRepositoryAdd()
    {
        var command = CreateValidCommand();

        _discountRepositoryMock
            .Setup(x => x.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mapperMock
            .Setup(x => x.Map<DiscountCodeDto>(It.IsAny<DiscountCode>()))
            .Returns(new DiscountCodeDto());

        await _handler.Handle(command, CancellationToken.None);

        _discountRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<DiscountCode>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallSaveChanges()
    {
        var command = CreateValidCommand();

        _discountRepositoryMock
            .Setup(x => x.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mapperMock
            .Setup(x => x.Map<DiscountCodeDto>(It.IsAny<DiscountCode>()))
            .Returns(new DiscountCodeDto());

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallAuditLog()
    {
        var command = CreateValidCommand();

        _discountRepositoryMock
            .Setup(x => x.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mapperMock
            .Setup(x => x.Map<DiscountCodeDto>(It.IsAny<DiscountCode>()))
            .Returns(new DiscountCodeDto());

        await _handler.Handle(command, CancellationToken.None);

        _auditServiceMock.Verify(
            x => x.LogAdminEventAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithRestrictions_ShouldAddRestrictionsToDiscount()
    {
        var command = CreateValidCommand() with
        {
            Restrictions = new List<CreateDiscountRestrictionDto>
            {
                new() { RestrictionType = "Category", EntityId = 1 }
            }
        };

        _discountRepositoryMock
            .Setup(x => x.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        DiscountCode? capturedDiscount = null;
        _discountRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DiscountCode>(), It.IsAny<CancellationToken>()))
            .Callback<DiscountCode, CancellationToken>((d, _) => capturedDiscount = d)
            .Returns(Task.CompletedTask);

        _mapperMock
            .Setup(x => x.Map<DiscountCodeDto>(It.IsAny<DiscountCode>()))
            .Returns(new DiscountCodeDto());

        await _handler.Handle(command, CancellationToken.None);

        capturedDiscount.Should().NotBeNull();
        capturedDiscount!.Restrictions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldSanitizeCode()
    {
        var command = CreateValidCommand();

        _htmlSanitizerMock
            .Setup(x => x.Sanitize("TESTCODE"))
            .Returns("TESTCODE");

        _discountRepositoryMock
            .Setup(x => x.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mapperMock
            .Setup(x => x.Map<DiscountCodeDto>(It.IsAny<DiscountCode>()))
            .Returns(new DiscountCodeDto());

        await _handler.Handle(command, CancellationToken.None);

        _htmlSanitizerMock.Verify(x => x.Sanitize(It.IsAny<string>()), Times.Once);
    }

    private static CreateDiscountCommand CreateValidCommand()
    {
        return new CreateDiscountCommand
        {
            Code = "TESTCODE",
            Percentage = 10,
            MaxDiscountAmount = null,
            MinOrderAmount = null,
            UsageLimit = null,
            MaxUsagePerUser = null,
            ExpiresAt = null,
            StartsAt = null,
            Restrictions = null
        };
    }
}