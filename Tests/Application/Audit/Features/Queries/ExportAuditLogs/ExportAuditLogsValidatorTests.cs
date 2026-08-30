using Application.Audit.Features.Queries.ExportAuditLogs;

namespace Tests.Application.Audit.Features.Queries.ExportAuditLogs;

public class ExportAuditLogsValidatorTests
{
    private readonly ExportAuditLogsValidator _sut = new();

    private static ExportAuditLogsQuery ValidQuery(
        Guid? userId = null,
        string? eventType = null,
        string? entityType = null,
        DateTime? from = null,
        DateTime? to = null,
        string format = "csv",
        int? maxRows = null) =>
        new(userId, eventType, entityType, from, to, format, maxRows);

    [Fact]
    public void Validate_WithMinimalValidQuery_IsValid()
    {
        var result = _sut.Validate(ValidQuery());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("json")]
    [InlineData("CSV")]
    [InlineData("Json")]
    [InlineData("JSON")]
    public void Validate_WithAllowedFormatCaseInsensitive_IsValid(string format)
    {
        var result = _sut.Validate(ValidQuery(format: format));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyFormat_FailsOnFormat()
    {
        var result = _sut.Validate(ValidQuery(format: string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportAuditLogsQuery.Format));
    }

    [Theory]
    [InlineData("xml")]
    [InlineData("pdf")]
    [InlineData("xlsx")]
    [InlineData("txt")]
    [InlineData("yaml")]
    public void Validate_WithDisallowedFormat_FailsOnFormat(string format)
    {
        var result = _sut.Validate(ValidQuery(format: format));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportAuditLogsQuery.Format));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(50_000)]
    [InlineData(100_000)]
    public void Validate_WithMaxRowsWithinAllowedRange_IsValid(int maxRows)
    {
        var result = _sut.Validate(ValidQuery(maxRows: maxRows));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(100_001)]
    [InlineData(1_000_000)]
    public void Validate_WithMaxRowsOutsideAllowedRange_FailsOnMaxRows(int maxRows)
    {
        var result = _sut.Validate(ValidQuery(maxRows: maxRows));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportAuditLogsQuery.MaxRows) + ".Value"
                                        || e.PropertyName == nameof(ExportAuditLogsQuery.MaxRows));
    }

    [Fact]
    public void Validate_WithNullMaxRows_DoesNotEvaluateRange()
    {
        var result = _sut.Validate(ValidQuery(maxRows: null));

        result.IsValid.ShouldBeTrue();
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
    [InlineData("information")]
    [InlineData("SECURITYEVENT")]
    public void Validate_WithAllowedEventType_IsValid(string eventType)
    {
        var result = _sut.Validate(ValidQuery(eventType: eventType));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("Trace")]
    [InlineData("Critical")]
    [InlineData("Fatal")]
    public void Validate_WithDisallowedEventType_FailsOnEventType(string eventType)
    {
        var result = _sut.Validate(ValidQuery(eventType: eventType));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportAuditLogsQuery.EventType));
    }

    [Fact]
    public void Validate_WithEventTypeLongerThanMaximumLength_FailsOnEventType()
    {
        var longEventType = new string('a', 101);

        var result = _sut.Validate(ValidQuery(eventType: longEventType));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportAuditLogsQuery.EventType));
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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportAuditLogsQuery.EntityType));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrWhitespaceEntityType_DoesNotEvaluateEntityType(string? entityType)
    {
        var result = _sut.Validate(ValidQuery(entityType: entityType));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFromGreaterThanTo_FailsOnDateRange()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(-5);

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
    public void Validate_WithOnlyFromSpecified_IsValid()
    {
        var result = _sut.Validate(ValidQuery(from: DateTime.UtcNow.AddDays(-10)));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyToSpecified_IsValid()
    {
        var result = _sut.Validate(ValidQuery(to: DateTime.UtcNow));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFromInTheFuture_FailsOnFrom()
    {
        var futureDate = DateTime.UtcNow.AddDays(30);

        var result = _sut.Validate(ValidQuery(from: futureDate, to: futureDate.AddDays(1)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportAuditLogsQuery.From));
    }

    [Fact]
    public void Validate_WithEmptyUserId_FailsOnUserId()
    {
        var result = _sut.Validate(ValidQuery(userId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportAuditLogsQuery.UserId));
    }

    [Fact]
    public void Validate_WithValidUserId_IsValid()
    {
        var result = _sut.Validate(ValidQuery(userId: Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNullUserId_DoesNotEvaluateUserId()
    {
        var result = _sut.Validate(ValidQuery(userId: null));

        result.IsValid.ShouldBeTrue();
    }
}
