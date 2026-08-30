using Application.Wallet.Features.Queries.GetWalletLedger;

namespace Tests.Application.Wallet.Features.Queries.GetWalletLedger;

public class GetWalletLedgerValidatorTests
{
    private readonly GetWalletLedgerValidator _sut = new();

    private static GetWalletLedgerQuery Query(
        Guid? userId = null,
        int page = 1,
        int pageSize = 10,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? transactionType = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        string? searchTerm = null,
        bool includeInactiveUsers = false) =>
        new(
            userId,
            page,
            pageSize,
            fromDate,
            toDate,
            transactionType,
            minAmount,
            maxAmount,
            searchTerm,
            includeInactiveUsers);

    [Fact]
    public void Validate_WithDefaultQuery_IsValid()
    {
        var result = _sut.Validate(Query());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNullUserId_IsValid()
    {
        var result = _sut.Validate(Query(userId: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUserIdGuid_IsValid()
    {
        var result = _sut.Validate(Query(userId: Guid.Empty));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithPageNotGreaterThanZero_IsInvalid(int page)
    {
        var result = _sut.Validate(Query(page: page));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetWalletLedgerQuery.Page));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(1_000)]
    public void Validate_WithPageGreaterThanZero_IsValid(int page)
    {
        var result = _sut.Validate(Query(page: page));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(201)]
    [InlineData(500)]
    public void Validate_WithPageSizeOutsideAllowedRange_IsInvalid(int pageSize)
    {
        var result = _sut.Validate(Query(pageSize: pageSize));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetWalletLedgerQuery.PageSize));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(200)]
    public void Validate_WithPageSizeAtBoundary_IsValid(int pageSize)
    {
        var result = _sut.Validate(Query(pageSize: pageSize));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFromDateAfterToDate_IsInvalid()
    {
        var from = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(Query(fromDate: from, toDate: to));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "FromDate.Value"
            && e.ErrorMessage == "FromDate must be less than or equal to ToDate.");
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
        result.Errors.ShouldContain(e =>
            e.PropertyName == "MinAmount.Value"
            && e.ErrorMessage == "MinAmount must be less than or equal to MaxAmount.");
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

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Validate_WithNegativeMinAmount_IsInvalid(decimal minAmount)
    {
        var result = _sut.Validate(Query(minAmount: minAmount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "MinAmount.Value");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(1000)]
    public void Validate_WithNonNegativeMinAmount_IsValid(decimal minAmount)
    {
        var result = _sut.Validate(Query(minAmount: minAmount));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Validate_WithNegativeMaxAmount_IsInvalid(decimal maxAmount)
    {
        var result = _sut.Validate(Query(maxAmount: maxAmount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "MaxAmount.Value");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(1000)]
    public void Validate_WithNonNegativeMaxAmount_IsValid(decimal maxAmount)
    {
        var result = _sut.Validate(Query(maxAmount: maxAmount));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNullSearchTerm_IsValid()
    {
        var result = _sut.Validate(Query(searchTerm: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptySearchTerm_IsValid()
    {
        var result = _sut.Validate(Query(searchTerm: string.Empty));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithSearchTermAtMaximumLength_IsValid()
    {
        var searchTerm = new string('a', 200);

        var result = _sut.Validate(Query(searchTerm: searchTerm));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithSearchTermLongerThanMaximumLength_IsInvalid()
    {
        var searchTerm = new string('a', 201);

        var result = _sut.Validate(Query(searchTerm: searchTerm));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetWalletLedgerQuery.SearchTerm));
    }
}
