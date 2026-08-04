namespace Application.Common.Interfaces;

public interface IHasUniqueConstraintMapping
{
    string? MapUniqueConstraintViolation(string? constraintName);
}
