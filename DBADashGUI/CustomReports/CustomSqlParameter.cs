using Microsoft.Data.SqlClient;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Provides SqlParameter with a boolean to indicate if the default value should be used.  The parameter is omitted in this case to allow the default value to be used.
    /// </summary>
    public class CustomSqlParameter
    {
        public SqlParameter Param { get; set; }
        public bool UseDefaultValue { get; set; }
    }
}