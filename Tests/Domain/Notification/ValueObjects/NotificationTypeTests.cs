
using Domain.Notification.Enums;
using Domain.Notification.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Notification.ValueObjects;

public class NotificationTypeTests
{
    public static IEnumerable<object[]> CatalogEntries()
    {
        yield return new object[] { NotificationType.OrderCreated, "OrderCreated", "ثبت سفارش", "shopping-cart", "green", NotificationCategory.Order };
        yield return new object[] { NotificationType.OrderPaid, "OrderPaid", "پرداخت سفارش", "credit-card", "green", NotificationCategory.Order };
        yield return new object[] { NotificationType.OrderShipped, "OrderShipped", "ارسال سفارش", "truck", "blue", NotificationCategory.Order };
        yield return new object[] { NotificationType.OrderDelivered, "OrderDelivered", "تحویل سفارش", "check-circle", "green", NotificationCategory.Order };
        yield return new object[] { NotificationType.OrderCancelled, "OrderCancelled", "لغو سفارش", "x-circle", "red", NotificationCategory.Order };
        yield return new object[] { NotificationType.TicketReply, "TicketReply", "پاسخ تیکت", "message-circle", "blue", NotificationCategory.Support };
        yield return new object[] { NotificationType.PriceDropAlert, "PriceDropAlert", "کاهش قیمت", "trending-down", "orange", NotificationCategory.Product };
        yield return new object[] { NotificationType.StockAlert, "StockAlert", "موجود شدن محصول", "package", "green", NotificationCategory.Product };
        yield return new object[] { NotificationType.DiscountCode, "DiscountCode", "کد تخفیف", "tag", "purple", NotificationCategory.Marketing };
        yield return new object[] { NotificationType.SystemAlert, "SystemAlert", "اطلاعیه سیستم", "bell", "gray", NotificationCategory.System };
        yield return new object[] { NotificationType.SecurityAlert, "SecurityAlert", "هشدار امنیتی", "shield", "red", NotificationCategory.System };
        yield return new object[] { NotificationType.AccountUpdate, "AccountUpdate", "به‌روزرسانی حساب", "user", "blue", NotificationCategory.System };
    }

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void CatalogEntry_ExposesExpectedValueDisplayNameIconColorAndCategory(
        NotificationType sut,
        string expectedValue,
        string expectedDisplayName,
        string expectedIcon,
        string expectedColor,
        NotificationCategory expectedCategory)
    {
        sut.Value.ShouldBe(expectedValue);
        sut.DisplayName.ShouldBe(expectedDisplayName);
        sut.Icon.ShouldBe(expectedIcon);
        sut.Color.ShouldBe(expectedColor);
        sut.Category.ShouldBe(expectedCategory);
    }

    [Theory]
    [InlineData("OrderCreated", "OrderCreated")]
    [InlineData("ordercreated", "OrderCreated")]
    [InlineData("ORDERCREATED", "OrderCreated")]
    [InlineData("OrderPaid", "OrderPaid")]
    [InlineData("OrderShipped", "OrderShipped")]
    [InlineData("OrderDelivered", "OrderDelivered")]
    [InlineData("OrderCancelled", "OrderCancelled")]
    [InlineData("TicketReply", "TicketReply")]
    [InlineData("PriceDropAlert", "PriceDropAlert")]
    [InlineData("StockAlert", "StockAlert")]
    [InlineData("DiscountCode", "DiscountCode")]
    [InlineData("SecurityAlert", "SecurityAlert")]
    [InlineData("AccountUpdate", "AccountUpdate")]
    public void FromString_WithKnownValueIgnoringCase_ReturnsMatchingCatalogEntry(string input, string expectedValue)
    {
        NotificationType.FromString(input).Value.ShouldBe(expectedValue);
    }

    [Fact]
    public void FromString_WithSystemAlert_ReturnsSystemAlert()
    {
        NotificationType.FromString("SystemAlert").Value.ShouldBe(NotificationType.SystemAlert.Value);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("random-string")]
    [InlineData("OrderFoo")]
    public void FromString_WithUnknownValue_FallsBackToSystemAlert(string input)
    {
        NotificationType.FromString(input).Value.ShouldBe(NotificationType.SystemAlert.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FromString_WithNullOrWhitespace_ThrowsDomainException(string? input)
    {
        var ex = Should.Throw<DomainException>(() => NotificationType.FromString(input!));
        ex.Message.ShouldBe("نوع اعلان نمی‌تواند خالی باشد.");
    }

    [Fact]
    public void Custom_WithValidArgs_TrimsValueAndDisplayNameAndAssignsCustomCategory()
    {
        var sut = NotificationType.Custom("  Promo  ", "  اعلان تبلیغاتی  ", "gift", "pink");

        sut.Value.ShouldBe("Promo");
        sut.DisplayName.ShouldBe("اعلان تبلیغاتی");
        sut.Icon.ShouldBe("gift");
        sut.Color.ShouldBe("pink");
        sut.Category.ShouldBe(NotificationCategory.Custom);
    }

    [Fact]
    public void Custom_WithDefaults_UsesBellIconAndGrayColor()
    {
        var sut = NotificationType.Custom("Promo", "اعلان");

        sut.Icon.ShouldBe("bell");
        sut.Color.ShouldBe("gray");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Custom_WithNullOrWhitespaceValue_ThrowsDomainException(string? value)
    {
        var ex = Should.Throw<DomainException>(() => NotificationType.Custom(value!, "display"));
        ex.Message.ShouldBe("مقدار نوع اعلان الزامی است.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Custom_WithNullOrWhitespaceDisplayName_ThrowsDomainException(string? displayName)
    {
        var ex = Should.Throw<DomainException>(() => NotificationType.Custom("Promo", displayName!));
        ex.Message.ShouldBe("نام نمایشی نوع اعلان الزامی است.");
    }

    [Fact]
    public void GetAll_ReturnsAllTwelveCatalogEntries()
    {
        var all = NotificationType.GetAll().ToList();

        all.Count.ShouldBe(12);
        all.Select(t => t.Value).ShouldBe(new[]
        {
            "OrderCreated",
            "OrderPaid",
            "OrderShipped",
            "OrderDelivered",
            "OrderCancelled",
            "TicketReply",
            "PriceDropAlert",
            "StockAlert",
            "DiscountCode",
            "SystemAlert",
            "SecurityAlert",
            "AccountUpdate"
        }, ignoreOrder: false);
    }

    [Fact]
    public void Equality_ForTwoInstancesWithSameValueIgnoringCase_TreatsInstancesAsEqual()
    {
        var lower = NotificationType.Custom("promo", "الف");
        var upper = NotificationType.Custom("PROMO", "ب");

        lower.ShouldBe(upper);
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsNotEqual()
    {
        NotificationType.OrderCreated.ShouldNotBe(NotificationType.OrderPaid);
    }

    [Fact]
    public void ToString_ReturnsDisplayName()
    {
        NotificationType.OrderCreated.ToString().ShouldBe("ثبت سفارش");
    }

    [Fact]
    public void ImplicitConversionToString_ReturnsValueNotDisplayName()
    {
        string asString = NotificationType.OrderCreated;

        asString.ShouldBe("OrderCreated");
    }
}

