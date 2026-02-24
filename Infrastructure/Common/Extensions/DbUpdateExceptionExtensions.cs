namespace Infrastructure.Common.Extensions;

public static class DbUpdateExceptionExtensions
{
    /// <summary>
    /// Returns true when the <see cref="DbUpdateException"/> was caused by a unique
    /// constraint violation, regardless of the underlying database provider.
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
    {
        if (ex.InnerException is Npgsql.PostgresException pgEx)
            return pgEx.SqlState == "23505";

        if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
            return sqlEx.Number == 2627 || sqlEx.Number == 2601;

        return ex.InnerException?.Message.Contains("UNIQUE constraint failed",
            StringComparison.OrdinalIgnoreCase) == true;
    }
}