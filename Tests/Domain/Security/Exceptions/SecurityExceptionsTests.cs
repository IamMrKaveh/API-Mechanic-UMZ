using Domain.Security.Exceptions;
using Domain.Security.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Security.Exceptions;

public class SecurityExceptionsTests
{
    [Fact]
    public void InvalidOtpCodeException_ExposesOtpIdAndErrorCode()
    {
        var id = OtpId.NewId();

        var sut = new InvalidOtpCodeException(id);

        sut.OtpId.ShouldBe(id);
        sut.ErrorCode.ShouldBe("INVALID_OTP_CODE");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void OtpAlreadyVerifiedException_ExposesOtpIdAndErrorCode()
    {
        var id = OtpId.NewId();

        var sut = new OtpAlreadyVerifiedException(id);

        sut.OtpId.ShouldBe(id);
        sut.ErrorCode.ShouldBe("OTP_ALREADY_VERIFIED");
    }

    [Fact]
    public void OtpExpiredException_ExposesOtpIdAndErrorCode()
    {
        var id = OtpId.NewId();

        var sut = new OtpExpiredException(id);

        sut.OtpId.ShouldBe(id);
        sut.ErrorCode.ShouldBe("OTP_EXPIRED");
    }

    [Fact]
    public void OtpMaxAttemptsExceededException_ExposesOtpIdMaxAttemptsAndErrorCode()
    {
        var id = OtpId.NewId();

        var sut = new OtpMaxAttemptsExceededException(id, 5);

        sut.OtpId.ShouldBe(id);
        sut.MaxAttempts.ShouldBe(5);
        sut.ErrorCode.ShouldBe("OTP_MAX_ATTEMPTS_EXCEEDED");
        sut.Message.ShouldContain("5");
    }

    [Fact]
    public void SessionExpiredException_ExposesSessionIdAndErrorCode()
    {
        var id = SessionId.NewId();

        var sut = new SessionExpiredException(id);

        sut.SessionId.ShouldBe(id);
        sut.ErrorCode.ShouldBe("SESSION_EXPIRED");
    }

    [Fact]
    public void SecurityExceptions_HaveDistinctErrorCodes()
    {
        var codes = new[]
        {
            new InvalidOtpCodeException(OtpId.NewId()).ErrorCode,
            new OtpAlreadyVerifiedException(OtpId.NewId()).ErrorCode,
            new OtpExpiredException(OtpId.NewId()).ErrorCode,
            new OtpMaxAttemptsExceededException(OtpId.NewId(), 3).ErrorCode,
            new SessionExpiredException(SessionId.NewId()).ErrorCode
        };

        codes.Distinct().Count().ShouldBe(5);
    }
}
