using Application.Wallet.Features.Queries.GetWalletsOverview;

namespace Tests.Application.Wallet.Features.Queries.GetWalletsOverview;

public class GetWalletsOverviewValidatorTests
{
    private readonly GetWalletsOverviewValidator _sut = new();

    private static GetWalletsOverviewQuery Query(
        string? search = null,
        bool? isFrozen = null,
        decimal? minBalance = null,
        decimal? maxBalance = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        string? sortBy = null,
        int page = 1,
        int pageSize = 20) =>
        new(
            search,
            isFrozen,
            minBalance,
            maxBalance,
            createdFrom,
            createdTo,
            sortBy,
            page,
            pageSize);

    [Fact]
    public void Validate_WithDefaultQuery_IsValid()
    {
        var result = _sut.Validate(Query());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithPageLessThanOne_IsInvalid(int page)
    {
        var result = _sut.Validate(Query(page: page));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(GetWalletsOverviewQuery.Page)
            && e.ErrorMessage == "شماره صفحه باید بزرگ‌تر یا مساوی ۱ باشد.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(1_000)]
    public void Validate_WithPageAtOrAboveOne_IsValid(int page)
    {
        var result = _sut.Validate(Query(page: page));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(201)]
    [InlineData(1_000)]
    public void Validate_WithPageSizeOutsideAllowedRange_IsInvalid(int pageSize)
    {
        var result = _sut.Validate(Query(pageSize: pageSize));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(GetWalletsOverviewQuery.PageSize)
            && e.ErrorMessage == "اندازه صفحه باید بین ۱ تا ۲۰۰ باشد.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(100)]
    [InlineData(200)]
    public void Validate_WithPageSizeAtBoundary_IsValid(int pageSize)
    {
        var result = _sut.Validate(Query(pageSize: pageSize));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Validate_WithNegativeMinBalance_IsInvalid(decimal minBalance)
    {
        var result = _sut.Validate(Query(minBalance: minBalance));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(GetWalletsOverviewQuery.MinBalance)
            && e.ErrorMessage == "حداقل موجودی نمی‌تواند منفی باشد.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(1_000_000)]
    public void Validate_WithNonNegativeMinBalance_IsValid(decimal minBalance)
    {
        var result = _sut.Validate(Query(minBalance: minBalance));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Validate_WithNegativeMaxBalance_IsInvalid(decimal maxBalance)
    {
        var result = _sut.Validate(Query(maxBalance: maxBalance));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(GetWalletsOverviewQuery.MaxBalance)
            && e.ErrorMessage == "حداکثر موجودی نمی‌تواند منفی باشد.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(1_000_000)]
    public void Validate_WithNonNegativeMaxBalance_IsValid(decimal maxBalance)
    {
        var result = _sut.Validate(Query(maxBalance: maxBalance));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithMinBalanceGreaterThanMaxBalance_IsInvalid()
    {
        var result = _sut.Validate(Query(minBalance: 1000m, maxBalance: 500m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "حداقل موجودی نباید بیشتر از حداکثر باشد.");
    }

    [Fact]
    public void Validate_WithMinBalanceEqualToMaxBalance_IsValid()
    {
        var result = _sut.Validate(Query(minBalance: 500m, maxBalance: 500m));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithMinBalanceLessThanMaxBalance_IsValid()
    {
        var result = _sut.Validate(Query(minBalance: 100m, maxBalance: 500m));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyMinBalanceProvided_DoesNotEnforceBalanceRange()
    {
        var result = _sut.Validate(Query(minBalance: 1000m, maxBalance: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyMaxBalanceProvided_DoesNotEnforceBalanceRange()
    {
        var result = _sut.Validate(Query(minBalance: null, maxBalance: 500m));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithCreatedFromAfterCreatedTo_IsInvalid()
    {
        var from = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(Query(createdFrom: from, createdTo: to));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "تاریخ شروع نباید بعد از تاریخ پایان باشد.");
    }

    [Fact]
    public void Validate_WithCreatedFromEqualToCreatedTo_IsValid()
    {
        var date = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(Query(createdFrom: date, createdTo: date));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithCreatedFromBeforeCreatedTo_IsValid()
    {
        var from = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(Query(createdFrom: from, createdTo: to));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyCreatedFromProvided_DoesNotEnforceDateRange()
    {
        var from = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(Query(createdFrom: from, createdTo: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithOnlyCreatedToProvided_DoesNotEnforceDateRange()
    {
        var to = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(Query(createdFrom: null, createdTo: to));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithMultipleViolations_ReportsAllErrors()
    {
        var from = new DateTime(2026, 08, 10, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);

        var query = new GetWalletsOverviewQuery(
            Search: null,
            IsFrozen: null,
            MinBalance: -1m,
            MaxBalance: -2m,
            CreatedFrom: from,
            CreatedTo: to,
            SortBy: null,
            Page: 0,
            PageSize: 0);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetWalletsOverviewQuery.Page));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetWalletsOverviewQuery.PageSize));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetWalletsOverviewQuery.MinBalance));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetWalletsOverviewQuery.MaxBalance));
        result.Errors.ShouldContain(e => e.ErrorMessage == "تاریخ شروع نباید بعد از تاریخ پایان باشد.");
    }
}
