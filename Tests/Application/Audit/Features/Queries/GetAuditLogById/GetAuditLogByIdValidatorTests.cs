using Application.Audit.Features.Queries.GetAuditLogById;

namespace Tests.Application.Audit.Features.Queries.GetAuditLogById;

public class GetAuditLogByIdValidatorTests
{
    private readonly GetAuditLogByIdValidator _sut = new();

    [Fact]
    public void Validate_WithNonEmptyId_IsValid()
    {
        var query = new GetAuditLogByIdQuery(Guid.NewGuid());

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyId_FailsOnId()
    {
        var query = new GetAuditLogByIdQuery(Guid.Empty);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditLogByIdQuery.Id));
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsExactlyOneError()
    {
        var query = new GetAuditLogByIdQuery(Guid.Empty);

        var result = _sut.Validate(query);

        result.Errors.Count.ShouldBe(1);
    }
}
