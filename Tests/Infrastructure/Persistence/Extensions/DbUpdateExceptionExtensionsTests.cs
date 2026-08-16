using Infrastructure.Persistence.Extensions;

namespace Tests.Infrastructure.Persistence.Extensions;

public class DbUpdateExceptionExtensionsTests
{
    [Fact]
    public void IsUniqueConstraintViolation_WithoutPostgresInnerException_ReturnsFalse()
    {
        var ex = new DbUpdateException("update failed");

        ex.IsUniqueConstraintViolation().ShouldBeFalse();
    }

    [Fact]
    public void IsUniqueConstraintViolation_WithNonPostgresInnerException_ReturnsFalse()
    {
        var ex = new DbUpdateException("update failed", new InvalidOperationException("unrelated"));

        ex.IsUniqueConstraintViolation().ShouldBeFalse();
    }

    [Fact]
    public void IsForeignKeyViolation_WithoutPostgresInnerException_ReturnsFalse()
    {
        var ex = new DbUpdateException("update failed");

        ex.IsForeignKeyViolation().ShouldBeFalse();
    }

    [Fact]
    public void IsForeignKeyViolation_WithNonPostgresInnerException_ReturnsFalse()
    {
        var ex = new DbUpdateException("update failed", new InvalidOperationException("unrelated"));

        ex.IsForeignKeyViolation().ShouldBeFalse();
    }

    [Fact]
    public void IsCheckConstraintViolation_WithoutPostgresInnerException_ReturnsFalse()
    {
        var ex = new DbUpdateException("update failed");

        ex.IsCheckConstraintViolation().ShouldBeFalse();
    }

    [Fact]
    public void IsCheckConstraintViolation_WithNonPostgresInnerException_ReturnsFalse()
    {
        var ex = new DbUpdateException("update failed", new InvalidOperationException("unrelated"));

        ex.IsCheckConstraintViolation().ShouldBeFalse();
    }

    [Fact]
    public void IsNotNullViolation_WithoutPostgresInnerException_ReturnsFalse()
    {
        var ex = new DbUpdateException("update failed");

        ex.IsNotNullViolation().ShouldBeFalse();
    }

    [Fact]
    public void IsNotNullViolation_WithNonPostgresInnerException_ReturnsFalse()
    {
        var ex = new DbUpdateException("update failed", new InvalidOperationException("unrelated"));

        ex.IsNotNullViolation().ShouldBeFalse();
    }

    [Fact]
    public void GetConstraintName_WithoutPostgresInnerException_ReturnsNull()
    {
        var ex = new DbUpdateException("update failed");

        ex.GetConstraintName().ShouldBeNull();
    }

    [Fact]
    public void GetConstraintName_WithNonPostgresInnerException_ReturnsNull()
    {
        var ex = new DbUpdateException("update failed", new InvalidOperationException("unrelated"));

        ex.GetConstraintName().ShouldBeNull();
    }

    [Fact]
    public void GetTableName_WithoutPostgresInnerException_ReturnsNull()
    {
        var ex = new DbUpdateException("update failed");

        ex.GetTableName().ShouldBeNull();
    }

    [Fact]
    public void GetTableName_WithNonPostgresInnerException_ReturnsNull()
    {
        var ex = new DbUpdateException("update failed", new InvalidOperationException("unrelated"));

        ex.GetTableName().ShouldBeNull();
    }
}
