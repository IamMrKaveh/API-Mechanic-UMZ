using Application.User.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Security.Enums;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.User.QueryServices;
using Orders = Domain.Order.Aggregates.Order;
using ProductAggregate = Domain.Product.Aggregates.Product;
using Reviews = Domain.Review.Aggregates.ProductReview;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.User.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class UserQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private UserQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new UserQueryService(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable) return;
        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Users> SeedUserAsync(
        string firstName = "Ali",
        string lastName = "Rezaei",
        string? email = null,
        string? phone = "09121234567",
        bool isAdmin = false,
        bool emailVerified = false)
    {
        var user = new UserBuilder()
            .WithFullName(FullName.Create(firstName, lastName))
            .WithEmail(Email.Create(email ?? $"user-{Guid.NewGuid():N}@example.com"))
            .WithPhoneNumber(phone is null ? null : PhoneNumber.Create(phone))
            .Build();

        if (isAdmin) user.PromoteToAdmin();
        if (emailVerified) user.VerifyEmail();

        user.ClearDomainEvents();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<ProductAggregate> SeedProductAsync()
    {
        var category = await new CategoryBuilder().BuildAsync();
        category.ClearDomainEvents();
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var brand = await new BrandBuilder().WithCategoryId(category.Id).BuildAsync();
        brand.ClearDomainEvents();
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    private async Task<(ProductAggregate product, global::Domain.Variant.Aggregates.ProductVariant variant)> SeedProductAndVariantAsync()
    {
        var product = await SeedProductAsync();
        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSku($"SKU-{Guid.NewGuid():N}"[..20])
            .Build();
        variant.ClearDomainEvents();
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        return (product, variant);
    }

    private async Task<VariantId> SeedProductVariantIdAsync()
    {
        var (_, variant) = await SeedProductAndVariantAsync();
        return variant.Id;
    }

    private async Task<Orders> SeedDeliveredOrderAsync(UserId userId, decimal finalAmount)
    {
        var (product, variant) = await SeedProductAndVariantAsync();

        var order = new OrderBuilder()
            .WithUserId(userId)
            .WithShippingCost(finalAmount, "IRT")
            .WithItemSnapshots(new OrderItemSnapshotBuilder()
                .WithVariantId(variant.Id)
                .WithProductId(product.Id)
                .WithProductName(product.Name)
                .WithSku(variant.Sku)
                .WithQuantity(1)
                .WithUnitPrice(0m, "IRT")
                .Build())
            .Build();
        order.ClearDomainEvents();

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(userId)
            .WithAmount(finalAmount == 0 ? 1m : finalAmount)
            .Build();
        transaction.ClearDomainEvents();

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        order.MoveToPending();
        order.MarkAsPaid(transaction.Id);
        order.StartProcessing();
        order.MarkAsShipped();
        order.MarkAsDelivered();
        order.ClearDomainEvents();

        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    private async Task<Orders> SeedCreatedOrderAsync(UserId userId)
    {
        var (product, variant) = await SeedProductAndVariantAsync();

        var order = new OrderBuilder()
            .WithUserId(userId)
            .WithItemSnapshots(new OrderItemSnapshotBuilder()
                .WithVariantId(variant.Id)
                .WithProductId(product.Id)
                .WithProductName(product.Name)
                .WithSku(variant.Sku)
                .WithQuantity(1)
                .WithUnitPrice(50_000m, "IRT")
                .Build())
            .Build();
        order.ClearDomainEvents();
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task GetUserProfileAsync_WhenUserExists_ReturnsMappedProfile()
    {
        var user = await SeedUserAsync(firstName: "Reza", lastName: "Ahmadi", phone: "09121110000", emailVerified: true);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUserProfileAsync(user.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(user.Id.Value);
        result.FirstName.ShouldBe("Reza");
        result.LastName.ShouldBe("Ahmadi");
        result.PhoneNumber.ShouldBe("09121110000");
        result.IsActive.ShouldBeTrue();
        result.IsAdmin.ShouldBeFalse();
        result.IsEmailVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task GetUserProfileAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetUserProfileAsync(UserId.NewId(), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetUserProfileAsync_WhenUserIsDeactivated_ReturnsNullDueToGlobalQueryFilter()
    {
        var user = await SeedUserAsync();
        user.Deactivate();
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUserProfileAsync(user.Id, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetUserProfileAsync_WhenPhoneNumberIsNull_ReturnsEmptyString()
    {
        var user = await SeedUserAsync(phone: null);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUserProfileAsync(user.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.PhoneNumber.ShouldBe("");
    }

    [Fact]
    public async Task GetUserDashboardAsync_WhenUserExists_AggregatesOrdersWishlistsAddressesAndTickets()
    {
        var user = await SeedUserAsync();
        await SeedDeliveredOrderAsync(user.Id, 200_000m);
        await SeedDeliveredOrderAsync(user.Id, 150_000m);
        await SeedCreatedOrderAsync(user.Id);
        new UserAddressParametersBuilder().AddTo(user);
        new UserAddressParametersBuilder().WithTitle("Office").AddTo(user);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUserDashboardAsync(user.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.TotalOrders.ShouldBe(3);
        result.CompletedOrders.ShouldBe(2);
        result.DeliveredOrders.ShouldBe(2);
        result.TotalSpent.ShouldBe(350_000m);
        result.ActiveAddresses.ShouldBe(2);
        result.WishlistCount.ShouldBe(0);
        result.OpenTickets.ShouldBe(0);
    }

    [Fact]
    public async Task GetUserDashboardAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetUserDashboardAsync(UserId.NewId(), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetUsersPagedAsync_WithMultipleUsers_ReturnsPagedAndOrderedByCreatedAtDescending()
    {
        var older = await SeedUserAsync(email: $"older-{Guid.NewGuid():N}@example.com", phone: "09121000001");
        await Task.Delay(20);
        var middle = await SeedUserAsync(email: $"middle-{Guid.NewGuid():N}@example.com", phone: "09121000002");
        await Task.Delay(20);
        var newest = await SeedUserAsync(email: $"newest-{Guid.NewGuid():N}@example.com", phone: "09121000003");
        _context.ChangeTracker.Clear();

        var page1 = await _sut.GetUsersPagedAsync(null, null, null, false, 1, 2, CancellationToken.None);
        var page2 = await _sut.GetUsersPagedAsync(null, null, null, false, 2, 2, CancellationToken.None);

        page1.TotalCount.ShouldBe(3);
        page1.Items.Count.ShouldBe(2);
        page1.Items[0].Id.ShouldBe(newest.Id.Value);
        page1.Items[1].Id.ShouldBe(middle.Id.Value);
        page2.Items.Count.ShouldBe(1);
        page2.Items[0].Id.ShouldBe(older.Id.Value);
    }

    [Fact]
    public async Task GetUsersPagedAsync_WithSearchTermMatchingEmail_ReturnsMatchingUsersOnly()
    {
        var target = await SeedUserAsync(email: $"unique-search-{Guid.NewGuid():N}@example.com", phone: "09122220001");
        await SeedUserAsync(email: $"other-{Guid.NewGuid():N}@example.com", phone: "09122220002");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUsersPagedAsync("unique-search", null, null, false, 1, 10, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(target.Id.Value);
    }

    [Fact]
    public async Task GetUsersPagedAsync_WithSearchTermMatchingFirstNameCaseInsensitive_ReturnsMatchingUsers()
    {
        var target = await SeedUserAsync(firstName: "Mohammad", lastName: "Bagheri", phone: "09122220011");
        await SeedUserAsync(firstName: "Sara", lastName: "Kazemi", phone: "09122220012");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUsersPagedAsync("MOHAMMAD", null, null, false, 1, 10, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(target.Id.Value);
    }

    [Fact]
    public async Task GetUsersPagedAsync_WithIsAdminFilter_ReturnsOnlyMatchingUsers()
    {
        var admin = await SeedUserAsync(isAdmin: true, phone: "09123330001");
        await SeedUserAsync(isAdmin: false, phone: "09123330002");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUsersPagedAsync(null, null, true, false, 1, 10, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(admin.Id.Value);
        result.Items[0].IsAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAdminUsersPagedAsync_WithRoleAdminFilter_ReturnsOnlyAdmins()
    {
        var admin = await SeedUserAsync(isAdmin: true, phone: "09124440001");
        await SeedUserAsync(isAdmin: false, phone: "09124440002");
        _context.ChangeTracker.Clear();

        var filter = new AdminUserFilterParams(null, "Admin", null, null, null, null, false, 1, 10);

        var result = await _sut.GetAdminUsersPagedAsync(filter, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(admin.Id.Value);
        result.Items[0].Roles.ShouldContain("Admin");
    }

    [Fact]
    public async Task GetAdminUsersPagedAsync_AggregatesOrderStatsAndWalletBalanceAndAddressStats()
    {
        var user = await SeedUserAsync(firstName: "Hassan", lastName: "Karimi", phone: "09125550001");
        await SeedDeliveredOrderAsync(user.Id, 300_000m);
        await SeedDeliveredOrderAsync(user.Id, 100_000m);
        await SeedCreatedOrderAsync(user.Id);

        new UserAddressParametersBuilder().WithProvince("Tehran").WithCity("Tehran").AddTo(user);
        var defaultAddress = new UserAddressParametersBuilder().WithProvince("Fars").WithCity("Shiraz");
        defaultAddress.AddTo(user);
        user.SetDefaultAddress(defaultAddress.AddressId);
        _context.Users.Update(user);

        var wallet = new WalletBuilder().WithOwnerId(user.Id).Build();
        wallet.Credit(Money.Create(250_000m, "IRT"), "seed", Guid.NewGuid().ToString());
        wallet.ClearDomainEvents();
        _context.Wallets.Add(wallet);

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var filter = new AdminUserFilterParams(null, null, null, null, null, null, false, 1, 10);
        var result = await _sut.GetAdminUsersPagedAsync(filter, CancellationToken.None);

        var item = result.Items.Single(i => i.Id == user.Id.Value);
        item.FullName.ShouldBe("Hassan Karimi");
        item.OrderCount.ShouldBe(3);
        item.CompletedOrderCount.ShouldBe(2);
        item.TotalSpent.ShouldBe(400_000m);
        item.WalletBalance.ShouldBe(250_000m);
        item.AddressCount.ShouldBe(2);
        item.DefaultAddressSummary.ShouldBe("Fars - Shiraz");
    }

    [Fact]
    public async Task GetAdminUsersPagedAsync_WithMinTotalSpentFilter_ReturnsOnlyUsersMeetingSpendThreshold()
    {
        var highSpender = await SeedUserAsync(phone: "09126660001");
        await SeedDeliveredOrderAsync(highSpender.Id, 500_000m);
        var lowSpender = await SeedUserAsync(phone: "09126660002");
        await SeedDeliveredOrderAsync(lowSpender.Id, 100_000m);
        _context.ChangeTracker.Clear();

        var filter = new AdminUserFilterParams(null, null, null, null, 300_000m, null, false, 1, 10);
        var result = await _sut.GetAdminUsersPagedAsync(filter, CancellationToken.None);

        result.Items.ShouldContain(i => i.Id == highSpender.Id.Value);
        result.Items.ShouldNotContain(i => i.Id == lowSpender.Id.Value);
    }

    [Fact]
    public async Task GetUserAddressesAsync_ReturnsAllAddressesForUser()
    {
        var user = await SeedUserAsync();
        var home = new UserAddressParametersBuilder().WithTitle("Home").WithCity("Tehran");
        home.AddTo(user);
        var office = new UserAddressParametersBuilder().WithTitle("Office").WithCity("Karaj");
        office.AddTo(user);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = (await _sut.GetUserAddressesAsync(user.Id, CancellationToken.None)).ToList();

        result.Count.ShouldBe(2);
        result.ShouldContain(a => a.Title == "Home" && a.City == "Tehran");
        result.ShouldContain(a => a.Title == "Office" && a.City == "Karaj");
    }

    [Fact]
    public async Task GetUserAddressesAsync_WhenUserHasNoAddresses_ReturnsEmpty()
    {
        var user = await SeedUserAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUserAddressesAsync(user.Id, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ReturnsOnlyActiveSessionsOrderedByCreatedAtDescending()
    {
        var user = await SeedUserAsync();

        var older = new UserSessionBuilder()
            .WithUserId(user.Id)
            .WithDeviceInfo("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0) Safari")
            .WithIpAddress("10.0.0.1")
            .WithExpiresAt(DateTime.UtcNow.AddDays(1))
            .Build();
        older.ClearDomainEvents();

        await Task.Delay(20);
        var newer = new UserSessionBuilder()
            .WithUserId(user.Id)
            .WithDeviceInfo("Mozilla/5.0 (Windows NT 10.0) Chrome/120")
            .WithIpAddress("10.0.0.2")
            .WithExpiresAt(DateTime.UtcNow.AddDays(3))
            .Build();
        newer.ClearDomainEvents();

        var revoked = new UserSessionBuilder()
            .WithUserId(user.Id)
            .WithExpiresAt(DateTime.UtcNow.AddDays(1))
            .Build();
        revoked.Revoke(SessionRevocationReason.UserRequested);
        revoked.ClearDomainEvents();

        _context.UserSessions.AddRange(older, newer, revoked);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = (await _sut.GetActiveSessionsAsync(user.Id, currentSessionId: newer.Id.Value, CancellationToken.None)).ToList();

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(newer.Id.Value);
        result[1].Id.ShouldBe(older.Id.Value);
        result[0].IsCurrent.ShouldBeTrue();
        result[1].IsCurrent.ShouldBeFalse();
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ExcludesExpiredSessions()
    {
        var user = await SeedUserAsync();

        var active = new UserSessionBuilder()
            .WithUserId(user.Id)
            .WithExpiresAt(DateTime.UtcNow.AddDays(2))
            .Build();
        active.ClearDomainEvents();

        _context.UserSessions.Add(active);
        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"UserSessions\" SET \"ExpiresAt\" = {0} WHERE \"UserId\" = {1}",
            DateTime.UtcNow.AddDays(-1), user.Id.Value);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetActiveSessionsAsync(user.Id, null, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetActiveSessionsAsync_MarksSessionExpiringWithin24HoursAsExpiringSoon()
    {
        var user = await SeedUserAsync();

        var session = new UserSessionBuilder()
            .WithUserId(user.Id)
            .WithExpiresAt(DateTime.UtcNow.AddDays(3))
            .Build();
        session.ClearDomainEvents();
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"UserSessions\" SET \"ExpiresAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddHours(12), session.Id.Value);
        _context.ChangeTracker.Clear();

        var result = (await _sut.GetActiveSessionsAsync(user.Id, null, CancellationToken.None)).ToList();

        result.Count.ShouldBe(1);
        result[0].IsExpiringSoon.ShouldBeTrue();
    }

    [Fact]
    public async Task GetUserReviewsPagedAsync_ReturnsUsersReviewsExcludingSoftDeleted()
    {
        var user = await SeedUserAsync();
        var product = await SeedProductAsync();

        var visible = await SeedReviewAsync(user.Id, product.Id, "Visible Review");
        var deleted = await SeedReviewAsync(user.Id, product.Id, "Deleted Review");

        deleted.MarkAsDeleted();
        _context.ProductReviews.Update(deleted);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUserReviewsPagedAsync(user.Id, 1, 10, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items.Single().Id.ShouldBe(visible.Id.Value);
        result.Items.Any(i => i.Id == deleted.Id.Value).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, -5)]
    public async Task GetUserReviewsPagedAsync_WithInvalidPaging_UsesDefaults(int page, int pageSize)
    {
        var user = await SeedUserAsync();
        var product = await SeedProductAsync();
        await SeedReviewAsync(user.Id, product.Id, "Test Review");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUserReviewsPagedAsync(user.Id, page, pageSize, CancellationToken.None);

        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
        result.TotalCount.ShouldBe(1);
    }

    private async Task<Reviews> SeedReviewAsync(
        UserId userId,
        ProductId productId,
        string title)
    {
        var review = new ProductReviewBuilder()
            .WithUserId(userId)
            .WithProductId(productId)
            .WithTitle(title)
            .WithRating(5)
            .WithoutOrderId()
            .Build();

        review.ClearDomainEvents();
        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }
}
