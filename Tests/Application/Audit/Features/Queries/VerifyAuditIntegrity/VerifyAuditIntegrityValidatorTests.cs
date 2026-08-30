using Application.Audit.Features.Queries.VerifyAuditIntegrity;

namespace Tests.Application.Audit.Features.Queries.VerifyAuditIntegrity;

public class VerifyAuditIntegrityValidatorTests
{
    private readonly VerifyAuditIntegrityValidator _sut = new();

    [Fact]
    public void Validate_WithNonEmptyId_IsValid()
    {
        var query = new VerifyAuditIntegrityQuery(Guid.NewGuid());

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyId_FailsOnId()
    {
        var query = new VerifyAuditIntegrityQuery(Guid.Empty);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(VerifyAuditIntegrityQuery.Id));
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsExactlyOneError()
    {
        var query = new VerifyAuditIntegrityQuery(Guid.Empty);

        var result = _sut.Validate(query);

        result.Errors.Count.ShouldBe(1);
    }

    [Fact]
    public void Validate_WithMultipleDifferentValidIds_IsAlwaysValid()
    {
        for (var i = 0; i < 5; i++)
        {
            var query = new VerifyAuditIntegrityQuery(Guid.NewGuid());

            var result = _sut.Validate(query);

            result.IsValid.ShouldBeTrue();
        }
    }
}
