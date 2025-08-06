using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DBADash
{
    internal class Utility
    {
        public static string GetResourceString(string resourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(resourcePath) ?? throw new Exception($"Resource {resourcePath} not found.");
            using StreamReader reader = new(stream);

            return reader.ReadToEnd();
        }

        private static readonly FrozenSet<int> ExcludedErrorCodes =
        [
        -2, // retryPolicy excludes query timeout #581
        218, // Could not find the type '%.*ls'. Either it does not exist or you do not have the necessary permission.
        219, // The type '%.*ls' already exists, or you do not have permission to create it.
        229, // The %ls permission was denied on the object '%.*ls', database '%.*ls', schema '%.*ls'.
        230, //	The %ls permission was denied on the column '%.*ls' of the object '%.*ls', database '%.*ls', schema '%.*ls'.
        245,   // Conversion failed when converting the %ls value '%.*ls' to data type %ls.
        262, // %ls permission denied in database '%.*ls'.
        297, //The user does not have permission to perform this action.
        300, // %ls permission was denied on object '%.*ls', database '%.*ls'.
        349,  // The procedure "%.*ls" has no parameter named "%.*ls".
        500,  // Trying to pass a table-valued parameter with %d column(s) where the corresponding user-defined table type requires %d column(s).
        2812, // Could not find stored procedure '%.*ls'.
        6335, // XML data type instance has too many levels of nested nodes. Maximum allowed depth is %d levels.
        8134, // Divide by zero error encountered.
        2627 // Violation of PRIMARY KEY constraint '%.*ls'. Cannot insert duplicate key in object '%.*ls'.
        ];

        public static bool ShouldRetry(Exception ex)
        {
            if (ex is SqlException sqlEx)
            {
                return !ExcludedErrorCodes.Contains(sqlEx.Number) && sqlEx.Message != "Max databases exceeded for Table Size collection" && !sqlEx.Message.Contains("denied", StringComparison.OrdinalIgnoreCase);
            }
            return true;
        }
    }
}