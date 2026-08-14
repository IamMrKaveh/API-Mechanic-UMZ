using Domain.Discount.Aggregates;
using Domain.Discount.Enums;
using Domain.Discount.ValueObjects;
using Infrastructure.Discount.QueryServices;
using Infrastructure.Discount.Repositories;
using Infrastructure.Persistence.Context;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Attributes;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Discount.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class DiscountQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private DiscountQueryService _sut = null!; private DiscountRepository _repository = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new DiscountQueryService(_context);
        _repository = new DiscountRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_ReturnsAllActiveDiscountsWithCorrectTotal()
    {
        await SeedAsync(new DiscountCodeBuilder().WithCode("A").Build());
        await SeedAsync(new DiscountCodeBuilder().WithCode("B").Build());
        await SeedAsync(new DiscountCodeBuilder().WithCode("C").Build());

        var (items, total) = await _sut.GetPagedAsync(
            includeExpired: true,
            includeDeleted: false,
            page: 1,
            pageSize: 10,
            ct: CancellationToken.None);

        total.ShouldBe(3);
        items.Count.ShouldBe(3);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_WithIncludeExpiredFalse_FiltersOutExpiredDiscounts()
    {
        await SeedAsync(new DiscountCodeBuilder().WithCode("LIVE").Build());
        await SeedAsync(new DiscountCodeBuilder().WithCode("PAST")
            .WithExpiresAt(DateTime.UtcNow.AddDays(-1)).Build());
        await SeedAsync(new DiscountCodeBuilder().WithCode("FUTURE")
            .WithExpiresAt(DateTime.UtcNow.AddDays(30)).Build());

        var (items, total) = await _sut.GetPagedAsync(
            includeExpired: false,
            includeDeleted: false,
            page: 1,
            pageSize: 10,
            ct: CancellationToken.None);

        total.ShouldBe(2);
        items.Select(i => i.Code).ShouldNotContain("PAST");
        items.Select(i => i.Code).ShouldContain("LIVE");
        items.Select(i => i.Code).ShouldContain("FUTURE");
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_WithIncludeExpiredTrue_IncludesExpiredDiscounts()
    {
        await SeedAsync(new DiscountCodeBuilder().WithCode("LIVE").Build());
        await SeedAsync(new DiscountCodeBuilder().WithCode("PAST")
            .WithExpiresAt(DateTime.UtcNow.AddDays(-1)).Build());

        var (items, total) = await _sut.GetPagedAsync(
            includeExpired: true,
            includeDeleted: false,
            page: 1,
            pageSize: 10,
            ct: CancellationToken.None);

        total.ShouldBe(2);
        items.Select(i => i.Code).ShouldContain("PAST");
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_AppliesPaginationAndOrdersByCreatedAtDescending()
    {
        var first = new DiscountCodeBuilder().WithCode("FIRST").Build();
        await SeedAsync(first);
        await Task.Delay(10);
        var second = new DiscountCodeBuilder().WithCode("SECOND").Build();
        await SeedAsync(second);
        await Task.Delay(10);
        var third = new DiscountCodeBuilder().WithCode("THIRD").Build();
        await SeedAsync(third);

        var (page1Items, page1Total) = await _sut.GetPagedAsync(
            includeExpired: true,
            includeDeleted: false,
            page: 1,
            pageSize: 2,
            ct: CancellationToken.None);

        page1Total.ShouldBe(3);
        page1Items.Count.ShouldBe(2);
        page1Items.First().Code.ShouldBe("THIRD");

        var (page2Items, page2Total) = await _sut.GetPagedAsync(
            includeExpired: true,
            includeDeleted: false,
            page: 2,
            pageSize: 2,
            ct: CancellationToken.None);

        page2Total.ShouldBe(3);
        page2Items.Count.ShouldBe(1);
        page2Items.Single().Code.ShouldBe("FIRST");
    }

    [RequiresDockerFact]
    public async Task GetDetailByIdAsync_WhenDiscountExists_ReturnsFullDetail()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("DETAIL")
            .WithValue(DiscountValue.Percentage(15m))
            .WithMaximumDiscountAmount(300m, "IRT")
            .WithUsageLimit(50)
            .Build();

        await SeedAsync(discount);

        var dto = await _sut.GetDetailByIdAsync(discount.Id, CancellationToken.None);

        dto.ShouldNotBeNull();
        dto!.Id.ShouldBe(discount.Id.Value);
        dto.Code.ShouldBe("DETAIL");
        dto.DiscountType.ShouldBe(DiscountType.Percentage.ToString());
        dto.DiscountValue.ShouldBe(15m);
        dto.MaximumDiscountAmount.ShouldBe(300m);
        dto.UsageLimit.ShouldBe(50);
        dto.UsageCount.ShouldBe(0);
        dto.IsActive.ShouldBeTrue();
        dto.IsExpired.ShouldBeFalse();
        dto.IsRedeemable.ShouldBeTrue();
        dto.Restrictions.ShouldBeEmpty();
    }

    [RequiresDockerFact]
    public async Task GetDetailByIdAsync_WhenIdDoesNotExist_ReturnsNull()
    {
        var dto = await _sut.GetDetailByIdAsync(DiscountCodeId.NewId(), CancellationToken.None);

        dto.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetDiscountInfoByCodeAsync_NormalizesInputCodeAndReturnsInfo()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("INFO")
            .WithValue(DiscountValue.Fixed(50m))
            .WithMaximumDiscountAmount(100m, "IRT")
            .Build();

        await SeedAsync(discount);

        var dto = await _sut.GetDiscountInfoByCodeAsync("  info  ", CancellationToken.None);

        dto.ShouldNotBeNull();
        dto!.Code.ShouldBe("INFO");
        dto.DiscountType.ShouldBe(DiscountType.FixedAmount.ToString());
        dto.DiscountValue.ShouldBe(50m);
        dto.MaximumDiscountAmount.ShouldBe(100m);
        dto.IsRedeemable.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task GetDiscountInfoByCodeAsync_WhenCodeDoesNotExist_ReturnsNull()
    {
        var dto = await _sut.GetDiscountInfoByCodeAsync("MISSING", CancellationToken.None);

        dto.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task ValidateDiscountAsync_WhenCodeDoesNotExist_ReturnsInvalid()
    {
        var result = await _sut.ValidateDiscountAsync(
            "MISSING",
            Money.Create(1000m, "IRT"),
            Guid.NewGuid(),
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsValid.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
    }

    [RequiresDockerFact]
    public async Task ValidateDiscountAsync_OnRedeemableCode_ReturnsValidWithComputedAmounts()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("VALID10")
            .WithValue(DiscountValue.Percentage(10m))
            .Build();

        await SeedAsync(discount);

        var result = await _sut.ValidateDiscountAsync(
            "valid10",
            Money.Create(1000m, "IRT"),
            Guid.NewGuid(),
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsValid.ShouldBeTrue();
        result.Code.ShouldBe("VALID10");
        result.DiscountCodeId.ShouldBe(discount.Id.Value);
        result.DiscountAmount.ShouldBe(100m);
        result.FinalAmount.ShouldBe(900m);
        result.DiscountType.ShouldBe(DiscountType.Percentage.ToString());
        result.DiscountValue.ShouldBe(10m);
    }

    [RequiresDockerFact]
    public async Task ValidateDiscountAsync_OnExpiredCode_ReturnsInvalid()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("EXP")
            .WithExpiresAt(DateTime.UtcNow.AddMinutes(-1))
            .Build();

        await SeedAsync(discount);

        var result = await _sut.ValidateDiscountAsync(
            "EXP",
            Money.Create(1000m, "IRT"),
            Guid.NewGuid(),
            CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
    }

    [RequiresDockerFact]
    public async Task ValidateDiscountAsync_OnInactiveCode_ReturnsInvalid()
    {
        var discount = new DiscountCodeBuilder().WithCode("OFFCODE").Build();
        discount.Deactivate();

        await SeedAsync(discount);

        var result = await _sut.ValidateDiscountAsync(
            "OFFCODE",
            Money.Create(500m, "IRT"),
            Guid.NewGuid(),
            CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
    }

    [RequiresDockerFact]
    public async Task GetUsageReportByIdAsync_WhenDiscountHasNoUsages_ReturnsEmptyReport()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("REPORT")
            .WithUsageLimit(20)
            .Build();

        await SeedAsync(discount);

        var report = await _sut.GetUsageReportByIdAsync(discount.Id, CancellationToken.None);

        report.ShouldNotBeNull();
        report!.DiscountCodeId.ShouldBe(discount.Id.Value);
        report.Code.ShouldBe("REPORT");
        report.UsageLimit.ShouldBe(20);
        report.TotalUsages.ShouldBe(0);
        report.TotalDiscountedAmount.ShouldBe(0m);
        report.Usages.ShouldBeEmpty();
    }

    [RequiresDockerFact]
    public async Task GetUsageReportByIdAsync_WhenIdDoesNotExist_ReturnsNull()
    {
        var report = await _sut.GetUsageReportByIdAsync(DiscountCodeId.NewId(), CancellationToken.None);

        report.ShouldBeNull();
    }

    private async Task SeedAsync(DiscountCode discount)
    {
        await _repository.AddAsync(discount, CancellationToken.None);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }
}
