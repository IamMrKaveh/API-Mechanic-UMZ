namespace Infrastructure.Persistence.Extensions;

public static class DbUpdateExceptionExtensions
{
    private const string PostgresUniqueViolationSqlState = "23505";
    private const string PostgresForeignKeyViolationSqlState = "23503";
    private const string PostgresCheckViolationSqlState = "23514";
    private const string PostgresNotNullViolationSqlState = "23502";

    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == PostgresUniqueViolationSqlState;

    public static bool IsForeignKeyViolation(this DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == PostgresForeignKeyViolationSqlState;

    public static bool IsCheckConstraintViolation(this DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == PostgresCheckViolationSqlState;

    public static bool IsNotNullViolation(this DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == PostgresNotNullViolationSqlState;

    public static string? GetConstraintName(this DbUpdateException ex)
        => (ex.InnerException as PostgresException)?.ConstraintName;

    public static string? GetTableName(this DbUpdateException ex)
        => (ex.InnerException as PostgresException)?.TableName;
}
