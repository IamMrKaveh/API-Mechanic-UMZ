using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common.Extensions;

public static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("23505") == true;
    }

    public static bool IsForeignKeyViolation(this DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("foreign key", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("23503") == true;
    }
}