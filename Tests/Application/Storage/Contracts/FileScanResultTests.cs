using Application.Storage.Contracts;

namespace Tests.Application.Storage.Contracts;

public class FileScanResultTests
{
    [Fact]
    public void Clean_WhenInvoked_ReturnsResultWithIsCleanTrueAndNullThreatAndNullEngineMessage()
    {
        var sut = FileScanResult.Clean();

        sut.IsClean.ShouldBeTrue();
        sut.ThreatName.ShouldBeNull();
        sut.EngineMessage.ShouldBeNull();
    }

    [Fact]
    public void Infected_WithThreatNameOnly_ReturnsResultWithIsCleanFalseAndProvidedThreatAndNullEngineMessage()
    {
        var sut = FileScanResult.Infected("Eicar-Test-Signature");

        sut.IsClean.ShouldBeFalse();
        sut.ThreatName.ShouldBe("Eicar-Test-Signature");
        sut.EngineMessage.ShouldBeNull();
    }

    [Fact]
    public void Infected_WithThreatNameAndEngineMessage_ReturnsResultWithIsCleanFalseAndProvidedValues()
    {
        var sut = FileScanResult.Infected("Win.Trojan.Test", "stream: Win.Trojan.Test FOUND");

        sut.IsClean.ShouldBeFalse();
        sut.ThreatName.ShouldBe("Win.Trojan.Test");
        sut.EngineMessage.ShouldBe("stream: Win.Trojan.Test FOUND");
    }

    [Theory]
    [InlineData("Eicar-Test-Signature", null)]
    [InlineData("ClamAV.EngineError", "engine failure")]
    [InlineData("ClamAV.Timeout", null)]
    [InlineData("ClamAV.Unavailable", "connection refused")]
    public void Infected_WithVariousArguments_PreservesProvidedValues(string threatName, string? engineMessage)
    {
        var sut = FileScanResult.Infected(threatName, engineMessage);

        sut.IsClean.ShouldBeFalse();
        sut.ThreatName.ShouldBe(threatName);
        sut.EngineMessage.ShouldBe(engineMessage);
    }

    [Fact]
    public void Clean_WhenCalledTwice_ReturnsEqualInstances()
    {
        var first = FileScanResult.Clean();
        var second = FileScanResult.Clean();

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Infected_WithSameArguments_ReturnsEqualInstances()
    {
        var first = FileScanResult.Infected("Win.Trojan.Test", "stream: Win.Trojan.Test FOUND");
        var second = FileScanResult.Infected("Win.Trojan.Test", "stream: Win.Trojan.Test FOUND");

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Infected_WithDifferentThreatNames_AreNotEqual()
    {
        var first = FileScanResult.Infected("Win.Trojan.A");
        var second = FileScanResult.Infected("Win.Trojan.B");

        first.ShouldNotBe(second);
    }

    [Fact]
    public void Infected_WithDifferentEngineMessages_AreNotEqual()
    {
        var first = FileScanResult.Infected("Win.Trojan.Test", "message one");
        var second = FileScanResult.Infected("Win.Trojan.Test", "message two");

        first.ShouldNotBe(second);
    }

    [Fact]
    public void Clean_AndInfected_AreNotEqual()
    {
        var clean = FileScanResult.Clean();
        var infected = FileScanResult.Infected("Win.Trojan.Test");

        clean.ShouldNotBe(infected);
    }

    [Fact]
    public void PrimaryConstructor_WithPositionalArguments_SetsAllProperties()
    {
        var sut = new FileScanResult(false, "Custom.Threat", "custom engine message");

        sut.IsClean.ShouldBeFalse();
        sut.ThreatName.ShouldBe("Custom.Threat");
        sut.EngineMessage.ShouldBe("custom engine message");
    }
}
