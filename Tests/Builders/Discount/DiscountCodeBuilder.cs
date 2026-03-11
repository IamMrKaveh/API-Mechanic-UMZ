using Domain.Discount;

namespace Tests.Builders.Discount;

public class DiscountCodeBuilder
{
    private string _code = "TESTCODE10";
    private decimal _percentage = 10m;
    private decimal? _maxDiscountAmount = null;
    private decimal? _minOrderAmount = null;
    private int? _usageLimit = null;
    private int? _maxUsagePerUser = null;
    private DateTime? _expiresAt = null;
    private DateTime? _startsAt = null;

    public DiscountCodeBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public DiscountCodeBuilder WithPercentage(decimal percentage)
    {
        _percentage = percentage;
        return this;
    }

    public DiscountCodeBuilder WithMaxDiscountAmount(decimal? maxDiscountAmount)
    {
        _maxDiscountAmount = maxDiscountAmount;
        return this;
    }

    public DiscountCodeBuilder WithMinOrderAmount(decimal? minOrderAmount)
    {
        _minOrderAmount = minOrderAmount;
        return this;
    }

    public DiscountCodeBuilder WithUsageLimit(int? usageLimit)
    {
        _usageLimit = usageLimit;
        return this;
    }

    public DiscountCodeBuilder WithMaxUsagePerUser(int? maxUsagePerUser)
    {
        _maxUsagePerUser = maxUsagePerUser;
        return this;
    }

    public DiscountCodeBuilder WithExpiresAt(DateTime? expiresAt)
    {
        _expiresAt = expiresAt;
        return this;
    }

    public DiscountCodeBuilder WithStartsAt(DateTime? startsAt)
    {
        _startsAt = startsAt;
        return this;
    }

    public DiscountCodeBuilder AlreadyExpired()
    {
        _expiresAt = DateTime.UtcNow.AddDays(-1);
        return this;
    }

    public DiscountCodeBuilder NotYetStarted()
    {
        _startsAt = DateTime.UtcNow.AddDays(1);
        return this;
    }

    public DiscountCodeBuilder WithFullUsage(int limit = 5)
    {
        _usageLimit = limit;
        return this;
    }

    public DiscountCode Build()
    {
        return DiscountCode.Create(
            _code,
            _percentage,
            _maxDiscountAmount,
            _minOrderAmount,
            _usageLimit,
            _expiresAt,
            _startsAt,
            _maxUsagePerUser
        );
    }
}