using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api.Data;

public static class DatabaseErrors
{
    public static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
}
