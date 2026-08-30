using Domain.Order.Entities;
using Infrastructure.Order.QueryServices;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Http;

namespace Tests.Infrastructure.Order.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrderStatusQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IHttpContextAccessor _httpContextAccessor = null!;
    private DefaultHttpContext _httpContext = null!;
    private OrderStatusQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _httpContext = new DefaultHttpContext();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _httpContextAccessor.HttpContext.Returns(_httpContext);

        _sut = new OrderStatusQueryService(_context, _httpContextAccessor);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<OrderStatus> SeedStatusAsync(
        string name,
        string displayName,
        int sortOrder,
        bool activate = true,
        bool setAsDefault = false,
        bool allowCancel = false,
        bool allowEdit = false,
        string? icon = "icon-name",
        string? color = "#ffffff")
    {
        var status = OrderStatus.Create(name, displayName, icon, color, sortOrder, allowCancel, allowEdit);
        _context.OrderStatuses.Add(status);
        await _context.SaveChangesAsync();

        if (!activate)
        {
            if (status.IsDefault)
                status.UnsetAsDefault();
            status.Deactivate();
            await _context.SaveChangesAsync();
        }
        if (setAsDefault)
        {
            status.SetAsDefault();
            await _context.SaveChangesAsync();
        }
        return status;
    }

    [Fact]
    public async Task GetAllAsync_WithNoStatuses_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync(onlyActive: null, CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithoutFilter_ReturnsAllStatusesIncludingInactive()
    {
        await SeedStatusAsync("Draft", "پیش‌نویس", 1);
        await SeedStatusAsync("Confirmed", "تایید", 2, activate: false);

        var result = await _sut.GetAllAsync(onlyActive: null, CancellationToken.None);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllAsync_WithOnlyActiveTrue_FiltersOutInactive()
    {
        await SeedStatusAsync("Draft", "پیش‌نویس", 1);
        await SeedStatusAsync("Inactive", "غیرفعال", 2, activate: false);

        var result = await _sut.GetAllAsync(onlyActive: true, CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Draft");
        result[0].IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllAsync_WithOnlyActiveFalse_ReturnsAllStatuses()
    {
        await SeedStatusAsync("Draft", "پیش‌نویس", 1);
        await SeedStatusAsync("Inactive", "غیرفعال", 2, activate: false);

        var result = await _sut.GetAllAsync(onlyActive: false, CancellationToken.None);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersBySortOrderAscending()
    {
        await SeedStatusAsync("Third", "سوم", 30);
        await SeedStatusAsync("First", "اول", 10);
        await SeedStatusAsync("Second", "دوم", 20);

        var result = await _sut.GetAllAsync(onlyActive: null, CancellationToken.None);

        result.Select(s => s.Name).ToList().ShouldBe(new[] { "First", "Second", "Third" });
    }

    [Fact]
    public async Task GetAllAsync_MapsAllPropertiesFromEntityToDto()
    {
        var seeded = await SeedStatusAsync(
            "Reviewing", "در حال بررسی", 5,
            allowCancel: true, allowEdit: true,
            icon: "check", color: "#00ff00");

        var result = await _sut.GetAllAsync(onlyActive: null, CancellationToken.None);

        result.Count.ShouldBe(1);
        var dto = result[0];
        dto.Id.ShouldBe(seeded.Id.Value);
        dto.Name.ShouldBe(seeded.Name);
        dto.DisplayName.ShouldBe(seeded.DisplayName);
        dto.SortOrder.ShouldBe(seeded.SortOrder);
        dto.IsActive.ShouldBe(seeded.IsActive);
        dto.IsDefault.ShouldBe(seeded.IsDefault);
        dto.AllowCancel.ShouldBe(seeded.AllowCancel);
        dto.AllowEdit.ShouldBe(seeded.AllowEdit);
        dto.Icon.ShouldBe(seeded.Icon);
        dto.Color.ShouldBe(seeded.Color);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeNull();
        _httpContext.Response.Headers.ETag.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsMappedDto()
    {
        var seeded = await SeedStatusAsync(
            "Approved", "تایید‌شده", 7,
            setAsDefault: true, allowCancel: true, allowEdit: false);

        var result = await _sut.GetByIdAsync(seeded.Id.Value, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(seeded.Id.Value);
        result.Name.ShouldBe(seeded.Name);
        result.DisplayName.ShouldBe(seeded.DisplayName);
        result.SortOrder.ShouldBe(seeded.SortOrder);
        result.IsActive.ShouldBeTrue();
        result.IsDefault.ShouldBeTrue();
        result.AllowCancel.ShouldBeTrue();
        result.AllowEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_SetsETagHeaderFromRowVersion()
    {
        var seeded = await SeedStatusAsync("WithETag", "با ETag", 8);
        var expectedEtag = $"\"{Convert.ToBase64String(seeded.RowVersion)}\"";

        var result = await _sut.GetByIdAsync(seeded.Id.Value, CancellationToken.None);

        result.ShouldNotBeNull();
        _httpContext.Response.Headers.ETag.Count.ShouldBe(1);
        _httpContext.Response.Headers.ETag[0]!.ShouldBe(expectedEtag);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsInactiveStatus()
    {
        var seeded = await SeedStatusAsync("HiddenStatus", "پنهان", 9, activate: false);

        var result = await _sut.GetByIdAsync(seeded.Id.Value, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WithHttpContextNull_DoesNotThrow()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var seeded = await SeedStatusAsync("NoHttp", "بدون HTTP", 10);

        var result = await _sut.GetByIdAsync(seeded.Id.Value, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(seeded.Id.Value);
    }
}
