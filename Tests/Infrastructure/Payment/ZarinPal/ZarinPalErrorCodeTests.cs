using System.Reflection;

namespace Tests.Infrastructure.Payment.ZarinPal;

public class ZarinPalErrorCodeTests
{
    private static Type ErrorCodeType()
    {
        var type = typeof(DBContext).Assembly.GetType(
            "Infrastructure.Payment.ZarinPal.ZarinPalErrorCode");
        type.ShouldNotBeNull();
        type!.IsEnum.ShouldBeTrue();
        return type;
    }

    [Theory]
    [InlineData("Success", 100)]
    [InlineData("AlreadyVerified", 101)]
    [InlineData("IncompleteInformation", -9)]
    [InlineData("IpMismatch", -10)]
    [InlineData("MerchantNotFound", -11)]
    [InlineData("ShaparakThrottling", -12)]
    [InlineData("AuthorityExpired", -22)]
    [InlineData("InvalidAmount", -50)]
    [InlineData("RequestNotFound", -51)]
    [InlineData("TransactionError", -52)]
    [InlineData("AuthorityMismatched", -53)]
    [InlineData("ArchivedRequest", -54)]
    [InlineData("OperationFailed", -1)]
    public void ErrorCode_HasExpectedNumericValue(string name, int expectedValue)
    {
        var type = ErrorCodeType();

        Enum.IsDefined(type, name).ShouldBeTrue();
        Convert.ToInt32(Enum.Parse(type, name)).ShouldBe(expectedValue);
    }

    [Fact]
    public void ErrorCode_DefinesAllExpectedMembers()
    {
        var names = Enum.GetNames(ErrorCodeType());

        names.Length.ShouldBe(13);
    }

    [Fact]
    public void ErrorCode_SuccessAndAlreadyVerified_AreOnlyNonNegativeCodes()
    {
        var type = ErrorCodeType();

        foreach (var value in Enum.GetValues(type).Cast<object>().Select(Convert.ToInt32))
        {
            if (value is 100 or 101)
                continue;
            value.ShouldBeLessThan(0);
        }
    }
}
