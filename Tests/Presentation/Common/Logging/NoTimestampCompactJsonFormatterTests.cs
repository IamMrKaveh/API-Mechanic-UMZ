using Presentation.Common.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace Tests.Presentation.Common.Logging;

public class NoTimestampCompactJsonFormatterTests
{
    private readonly NoTimestampCompactJsonFormatter _sut = new();

    private static string Format(LogEvent evt)
    {
        using var writer = new StringWriter();
        new NoTimestampCompactJsonFormatter().Format(evt, writer);
        return writer.ToString().Trim();
    }

    private static LogEvent BuildEvent(
        LogEventLevel level = LogEventLevel.Information,
        string template = "Hello {Name}",
        Exception? exception = null,
        params LogEventProperty[] properties) =>
        new(
            DateTimeOffset.UtcNow,
            level,
            exception,
            new MessageTemplateParser().Parse(template),
            properties);

    [Theory]
    [InlineData(LogEventLevel.Verbose, "VRB")]
    [InlineData(LogEventLevel.Debug, "DBG")]
    [InlineData(LogEventLevel.Information, "INF")]
    [InlineData(LogEventLevel.Warning, "WRN")]
    [InlineData(LogEventLevel.Error, "ERR")]
    [InlineData(LogEventLevel.Fatal, "FTL")]
    public void Format_MapsLevelToShortCode(LogEventLevel level, string code)
    {
        var json = Format(BuildEvent(level, "plain message"));

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("level").GetString().ShouldBe(code);
        doc.RootElement.GetProperty("message").GetString().ShouldBe("plain message");
    }

    [Fact]
    public void Format_RendersMessageTemplateWithProperties()
    {
        var evt = BuildEvent(LogEventLevel.Information, "Hello {Name}", null,
            new LogEventProperty("Name", new ScalarValue("Ali")));

        using var doc = JsonDocument.Parse(Format(evt));
        doc.RootElement.GetProperty("message").GetString().ShouldBe("Hello \"Ali\"");
        doc.RootElement.GetProperty("name").GetString().ShouldBe("Ali");
    }

    [Fact]
    public void Format_ShortensSourceContextToClassName()
    {
        var evt = BuildEvent(properties:
            [new LogEventProperty("SourceContext", new ScalarValue("A.B.MyService"))]);

        using var doc = JsonDocument.Parse(Format(evt));
        doc.RootElement.GetProperty("source").GetString().ShouldBe("MyService");
        doc.RootElement.TryGetProperty("SourceContext", out _).ShouldBeFalse();
    }

    [Fact]
    public void Format_ExcludesNoisyProperties()
    {
        var evt = BuildEvent(properties:
            [new LogEventProperty("ActionId", new ScalarValue("1")),
            new LogEventProperty("RequestId", new ScalarValue("2")),
            new LogEventProperty("KeepMe", new ScalarValue("yes"))]);

        using var doc = JsonDocument.Parse(Format(evt));
        doc.RootElement.TryGetProperty("actionId", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("requestId", out _).ShouldBeFalse();
        doc.RootElement.GetProperty("keepMe").GetString().ShouldBe("yes");
    }

    [Fact]
    public void Format_SkipsNullAndEmptyProperties()
    {
        var evt = BuildEvent(properties:
            [new LogEventProperty("NullProp", new ScalarValue(null)),
            new LogEventProperty("EmptyProp", new ScalarValue(string.Empty))]);

        using var doc = JsonDocument.Parse(Format(evt));
        doc.RootElement.TryGetProperty("nullProp", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("emptyProp", out _).ShouldBeFalse();
    }

    [Fact]
    public void Format_ConvertsScalarTypes()
    {
        var id = Guid.NewGuid();
        var evt = BuildEvent(properties:
            [new LogEventProperty("Count", new ScalarValue(42)),
            new LogEventProperty("Enabled", new ScalarValue(true)),
            new LogEventProperty("Id", new ScalarValue(id)),
            new LogEventProperty("When", new ScalarValue(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc))),
            new LogEventProperty("State", new ScalarValue(DayOfWeek.Monday))]);

        using var doc = JsonDocument.Parse(Format(evt));
        doc.RootElement.GetProperty("count").GetInt32().ShouldBe(42);
        doc.RootElement.GetProperty("enabled").GetBoolean().ShouldBeTrue();
        doc.RootElement.GetProperty("id").GetString().ShouldBe(id.ToString());
        doc.RootElement.GetProperty("when").GetString().ShouldStartWith("2026-01-02");
        doc.RootElement.GetProperty("state").GetString().ShouldBe("Monday");
    }

    [Fact]
    public void Format_ConvertsSequenceDictionaryAndStructureValues()
    {
        var evt = BuildEvent(properties:
            [new LogEventProperty("Tags", new SequenceValue(new LogEventPropertyValue[]
                { new ScalarValue("a"), new ScalarValue("b") })),
            new LogEventProperty("Meta", new DictionaryValue(new Dictionary<ScalarValue, LogEventPropertyValue>
                { [new ScalarValue("k")] = new ScalarValue("v") })),
            new LogEventProperty("User", new StructureValue(
                new[] { new LogEventProperty("FirstName", new ScalarValue("Sara")) }, "User"))]);

        using var doc = JsonDocument.Parse(Format(evt));
        doc.RootElement.GetProperty("tags").GetArrayLength().ShouldBe(2);
        doc.RootElement.GetProperty("meta").GetProperty("k").GetString().ShouldBe("v");
        doc.RootElement.GetProperty("user").GetProperty("firstName").GetString().ShouldBe("Sara");
    }

    [Fact]
    public void Format_TruncatesLongStrings()
    {
        var evt = BuildEvent(properties:
            [new LogEventProperty("Big", new ScalarValue(new string('x', 5000)))]);

        using var doc = JsonDocument.Parse(Format(evt));
        var value = doc.RootElement.GetProperty("big").GetString()!;
        value.Length.ShouldBe(4000 + "…[truncated]".Length);
        value.ShouldEndWith("…[truncated]");
    }

    [Fact]
    public void Format_StopsAtMaxDepth()
    {
        LogEventPropertyValue nested = new ScalarValue("leaf");
        for (var i = 0; i < 8; i++)
            nested = new StructureValue(
                new[] { new LogEventProperty("Child", nested) }, "Node");
        var evt = BuildEvent(properties: [new LogEventProperty("Root", nested)]);

        Format(evt).ShouldContain("[MaxDepthExceeded]");
    }

    [Fact]
    public void Format_IncludesExceptionDetails()
    {
        var inner = new InvalidOperationException("inner boom");
        var ex = new ArgumentException("outer boom", inner);
        ex.Data["Key1"] = "Value1";
        var evt = BuildEvent(LogEventLevel.Error, "failed", ex);

        using var doc = JsonDocument.Parse(Format(evt));
        doc.RootElement.GetProperty("exceptionType").GetString().ShouldContain(nameof(ArgumentException));
        doc.RootElement.GetProperty("exceptionMessage").GetString().ShouldBe("outer boom");
        doc.RootElement.GetProperty("innerExceptionType").GetString().ShouldContain(nameof(InvalidOperationException));
        doc.RootElement.GetProperty("innerExceptionMessage").GetString().ShouldBe("inner boom");
        doc.RootElement.GetProperty("exceptionData").GetProperty("Key1").GetString().ShouldBe("Value1");
        doc.RootElement.GetProperty("exception").GetString().ShouldContain("outer boom");
    }

    [Fact]
    public void Format_WithoutException_HasNoExceptionFields()
    {
        using var doc = JsonDocument.Parse(Format(BuildEvent()));

        doc.RootElement.TryGetProperty("exception", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("exceptionType", out _).ShouldBeFalse();
    }

    [Fact]
    public void Format_DoesNotEmitTimestampField()
    {
        using var doc = JsonDocument.Parse(Format(BuildEvent()));

        doc.RootElement.TryGetProperty("timestamp", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("@t", out _).ShouldBeFalse();
    }

    [Fact]
    public void Format_NullArguments_Throw()
    {
        using var writer = new StringWriter();
        var evt = BuildEvent();

        Should.Throw<ArgumentNullException>(() => _sut.Format(null!, writer));
        Should.Throw<ArgumentNullException>(() => _sut.Format(evt, null!));
    }

    [Fact]
    public void Format_Output_IsSingleJsonLine()
    {
        var output = Format(BuildEvent());

        output.ShouldNotContain("\n");
        Should.NotThrow(() => JsonDocument.Parse(output));
    }
}
