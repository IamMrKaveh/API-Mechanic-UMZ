using Presentation.Auth.Requests;

namespace Tests.Presentation.Auth.Requests;

public class AuthRequestsTests
{
    [Fact]
    public void SendOtpRequest_WithPhoneNumber_SetsCorrectly()
    {
        var request = new SendOtpRequest("09123456789");

        request.PhoneNumber.ShouldBe("09123456789");
    }

    [Fact]
    public void SendOtpRequest_IsRecord_EqualityWorks()
    {
        var request1 = new SendOtpRequest("09123456789");
        var request2 = new SendOtpRequest("09123456789");
        var request3 = new SendOtpRequest("09987654321");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void VerifyOtpRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new VerifyOtpRequest("09123456789", "123456", "device-1");

        request.PhoneNumber.ShouldBe("09123456789");
        request.Code.ShouldBe("123456");
        request.DeviceInfo.ShouldBe("device-1");
    }

    [Fact]
    public void VerifyOtpRequest_WithDefaultDeviceInfo_SetsToNull()
    {
        var request = new VerifyOtpRequest("09123456789", "123456");

        request.DeviceInfo.ShouldBeNull();
    }

    [Fact]
    public void VerifyOtpRequest_IsRecord_EqualityWorks()
    {
        var request1 = new VerifyOtpRequest("09123456789", "123456", "device-1");
        var request2 = new VerifyOtpRequest("09123456789", "123456", "device-1");
        var request3 = new VerifyOtpRequest("09123456789", "654321", "device-1");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
