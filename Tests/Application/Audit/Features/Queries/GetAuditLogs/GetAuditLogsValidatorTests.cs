using Application.Audit.Features.Queries.GetAuditLogs;

namespace Tests.Application.Audit.Features.Queries.GetAuditLogs;

public class GetAuditLogsValidatorTests
{
    private readonly GetAuditLogsValidator _sut = new();

    private static GetAuditLogsQuery ValidQuery(
        Guid? userId = null,
        string? eventType = null,
        string? entityType = null,
        string? action = null,
        string? keyword = null,
        string? ipAddress = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 50,
        string sortBy = "CreatedAt",
        bool sortDesc = true) =>
        new(userId, eventType, entityType, action, keyword, ipAddress, from, to, page, pageSize, sortBy, sortDesc);

    [Fact]
    public void Validate_WithDefaultQuery_IsValid()
    {
        var result = _sut.Validate(ValidQuery());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void Validate_WithPageGreaterThanZero_IsValid(int page)
    {
        var result = _sut.Validate(ValidQuery(page: page));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithPageZeroOrLess_FailsOnPage(int page)
    {
        var result = _sut.Validate(ValidQuery(page: page));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.Page));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public void Validate_WithPageSizeWithinRange_IsValid(int pageSize)
    {
        var result = _sut.Validate(ValidQuery(pageSize: pageSize));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(201)]
    [InlineData(500)]
    [InlineData(int.MaxValue)]
    public void Validate_WithPageSizeOutsideRange_FailsOnPageSize(int pageSize)
    {
        var result = _sut.Validate(ValidQuery(pageSize: pageSize));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.PageSize));
    }

    [Theory]
    [InlineData("CreatedAt")]
    [InlineData("EventType")]
    [InlineData("Action")]
    [InlineData("createdat")]
    [InlineData("EVENTTYPE")]
    public void Validate_WithAllowedSortColumn_IsValid(string sortBy)
    {
        var result = _sut.Validate(ValidQuery(sortBy: sortBy));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("UserId")]
    [InlineData("Unknown")]
    [InlineData("Timestamp")]
    public void Validate_WithDisallowedSortColumn_FailsOnSortBy(string sortBy)
    {
        var result = _sut.Validate(ValidQuery(sortBy: sortBy));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.SortBy));
    }

    [Theory]
    [InlineData("Information")]
    [InlineData("Debug")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("SecurityEvent")]
    [InlineData("SystemEvent")]
    [InlineData("OrderEvent")]
    [InlineData("PaymentEvent")]
    [InlineData("InventoryEvent")]
    [InlineData("ProductEvent")]
    [InlineData("AdminEvent")]
    public void Validate_WithAllowedEventType_IsValid(string eventType)
    {
        var result = _sut.Validate(ValidQuery(eventType: eventType));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("Trace")]
    [InlineData("Fatal")]
    public void Validate_WithDisallowedEventType_FailsOnEventType(string eventType)
    {
        var result = _sut.Validate(ValidQuery(eventType: eventType));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.EventType));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrWhitespaceEventType_DoesNotEvaluateEventType(string? eventType)
    {
        var result = _sut.Validate(ValidQuery(eventType: eventType));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEntityTypeAtMaximumLength_IsValid()
    {
        var entityType = new string('a', 100);

        var result = _sut.Validate(ValidQuery(entityType: entityType));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEntityTypeLongerThanMaximumLength_FailsOnEntityType()
    {
        var entityType = new string('a', 101);

        var result = _sut.Validate(ValidQuery(entityType: entityType));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.EntityType));
    }

    [Fact]
    public void Validate_WithActionAtMaximumLength_IsValid()
    {
        var action = new string('a', 200);

        var result = _sut.Validate(ValidQuery(action: action));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithActionLongerThanMaximumLength_FailsOnAction()
    {
        var action = new string('a', 201);

        var result = _sut.Validate(ValidQuery(action: action));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.Action));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("search term")]
    public void Validate_WithKeywordAtOrAboveMinimumLength_IsValid(string keyword)
    {
        var result = _sut.Validate(ValidQuery(keyword: keyword));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    public void Validate_WithKeywordShorterThanMinimumLength_FailsOnKeyword(string keyword)
    {
        var result = _sut.Validate(ValidQuery(keyword: keyword));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.Keyword));
    }

    [Fact]
    public void Validate_WithKeywordAtMaximumLength_IsValid()
    {
        var keyword = new string('a', 200);

        var result = _sut.Validate(ValidQuery(keyword: keyword));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithKeywordLongerThanMaximumLength_FailsOnKeyword()
    {
        var keyword = new string('a', 201);

        var result = _sut.Validate(ValidQuery(keyword: keyword));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.Keyword));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrWhitespaceKeyword_DoesNotEvaluateKeyword(string? keyword)
    {
        var result = _sut.Validate(ValidQuery(keyword: keyword));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("0.0.0.0")]
    [InlineData("::1")]
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334")]
    public void Validate_WithValidIpAddress_IsValid(string ipAddress)
    {
        var result = _sut.Validate(ValidQuery(ipAddress: ipAddress));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("999.999.999.999")]
    [InlineData("not-an-ip")]
    [InlineData("256.0.0.1")]
    [InlineData("192.168.1")]
    [InlineData("abcd::xyz")]
    public void Validate_WithInvalidIpAddress_FailsOnIpAddress(string ipAddress)
    {
        var result = _sut.Validate(ValidQuery(ipAddress: ipAddress));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.IpAddress));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrWhitespaceIpAddress_DoesNotEvaluateIpAddress(string? ipAddress)
    {
        var result = _sut.Validate(ValidQuery(ipAddress: ipAddress));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUserId_FailsOnUserId()
    {
        var result = _sut.Validate(ValidQuery(userId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.UserId));
    }

    [Fact]
    public void Validate_WithValidUserId_IsValid()
    {
        var result = _sut.Validate(ValidQuery(userId: Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFromGreaterThanTo_FailsOnDateRange()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(-10);

        var result = _sut.Validate(ValidQuery(from: from, to: to));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "DateRange");
    }

    [Fact]
    public void Validate_WithFromEqualToTo_IsValid()
    {
        var timestamp = DateTime.UtcNow.AddDays(-1);

        var result = _sut.Validate(ValidQuery(from: timestamp, to: timestamp));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFromInTheFuture_FailsOnFrom()
    {
        var futureFrom = DateTime.UtcNow.AddDays(10);
        var futureTo = DateTime.UtcNow.AddDays(20);

        var result = _sut.Validate(ValidQuery(from: futureFrom, to: futureTo));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.From));
    }

    [Fact]
    public void Validate_WithToMoreThanOneDayInTheFuture_FailsOnTo()
    {
        var to = DateTime.UtcNow.AddDays(2);

        var result = _sut.Validate(ValidQuery(to: to));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogsQuery.To));
    }

    [Fact]
    public void Validate_WithToSlightlyInTheFutureWithinToleranceWindow_IsValid()
    {
        var to = DateTime.UtcNow.AddHours(12);

        var result = _sut.Validate(ValidQuery(to: to));

        result.IsValid.ShouldBeTrue();
    }
}
