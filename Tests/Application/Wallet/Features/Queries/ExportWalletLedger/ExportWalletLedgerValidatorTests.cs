using Application.Wallet.Features.Queries.ExportWalletLedger;

namespace Tests.Application.Wallet.Features.Queries.ExportWalletLedger;

public class ExportWalletLedgerValidatorTests
{
    private readonly ExportWalletLedgerValidator _sut = new();

    private static ExportWalletLedgerQuery Query(
        Guid? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? transactionType = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        string? searchTerm = null,
        string format = "csv",
        int maxRows = 10_000) =>
        new(
            userId ?? Guid.NewGuid(),
            fromDate,
            toDate,
            transactionType,
            minAmount,
            maxAmount,
            searchTerm,
            format,
            maxRows);

    [Fact]
    public void Validate_WithValidQueryUsingDefaults_IsValid()
    {
        var result = _sut.Validate(Query());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUserId_IsInvalid()
    {
        var result = _sut.Validate(Query(userId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportWalletLedgerQuery.UserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(100_001)]
    [InlineData(1_000_000)]
    public void Validate_WithMaxRowsOutsideAllowedRange_IsInvalid(int maxRows)
    {
        var result = _sut.Validate(Query(maxRows: maxRows));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportWalletLedgerQuery.MaxRows));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10_000)]
    [InlineData(50_000)]
    [InlineData(100_000)]
    public void Validate_WithMaxRowsAtBoundary_IsValid(int maxRows)
    {
        var result = _sut.Validate(Query(maxRows: maxRows));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespaceFormat_IsInvalid(string format)
    {
        var result = _sut.Validate(Query(format: format));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportWalletLedgerQuery.Format));
    }

    [Theory]
    [InlineData("xml")]
    [InlineData("xlsx")]
    [InlineData("pdf")]
    [InlineData("txt")]
    public void Validate_WithUnsupportedFormat_IsInvalid(string format)
    {
        var result = _sut.Validate(Query(format: format));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ExportWalletLedgerQuery.Format)
            && e.ErrorMessage == "Format must be either 'csv' or 'json'.");
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("json")]
    [InlineData("CSV")]
    [InlineData("Json")]
    [InlineData("JSON")]
    [InlineData("Csv")]
    public void Validate_WithSupportedFormatIgnoringCase_IsValid(string format)
    {
        var result = _sut.Validate(Query(format: format));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFromDateAfterToDate_IsInvalid()
    {
        var from = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(Query(fromDate: from, toDate: to));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "FromDate.Value");
    }

    [Fact]
    public void Validate_WithFromDateEqualToToDate_IsValid()
    {
        var date = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(Query(fromDate: date, toDate: date));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFromDateBeforeToDate_IsValid()
    {
        var from = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(Query(fromDate: from, toDate: to));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyFromDateProvided_DoesNotEnforceDateRange()
    {
        var from = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(Query(fromDate: from, toDate: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyToDateProvided_DoesNotEnforceDateRange()
    {
        var to = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(Query(fromDate: null, toDate: to));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithMinAmountGreaterThanMaxAmount_IsInvalid()
    {
        var result = _sut.Validate(Query(minAmount: 1000m, maxAmount: 500m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "MinAmount.Value");
    }

    [Fact]
    public void Validate_WithMinAmountEqualToMaxAmount_IsValid()
    {
        var result = _sut.Validate(Query(minAmount: 500m, maxAmount: 500m));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithMinAmountLessThanMaxAmount_IsValid()
    {
        var result = _sut.Validate(Query(minAmount: 100m, maxAmount: 500m));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyMinAmountProvided_DoesNotEnforceAmountRange()
    {
        var result = _sut.Validate(Query(minAmount: 1000m, maxAmount: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyMaxAmountProvided_DoesNotEnforceAmountRange()
    {
        var result = _sut.Validate(Query(minAmount: null, maxAmount: 500m));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithMultipleViolations_ReportsAllErrors()
    {
        var from = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);

        var query = new ExportWalletLedgerQuery(
            Guid.Empty,
            from,
            to,
            null,
            1000m,
            500m,
            null,
            "xml",
            0);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportWalletLedgerQuery.UserId));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportWalletLedgerQuery.MaxRows));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ExportWalletLedgerQuery.Format));
        result.Errors.ShouldContain(e => e.PropertyName == "FromDate.Value");
        result.Errors.ShouldContain(e => e.PropertyName == "MinAmount.Value");
    }
}
