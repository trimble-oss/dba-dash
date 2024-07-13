using System;
using System.Data;
using System.Xml.Serialization;
using Microsoft.Data.SqlClient;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Parameter associated with a stored procedure for a user report
    /// </summary>
    public class Param
    {
        [XmlAttribute(AttributeName = "ParamName")]
        public string ParamName { get; set; }

        [XmlAttribute(AttributeName = "ParamType")]
        public string ParamType { get; set; }

        /// <summary>
        /// Create a CustomSqlParameter object with the correct data type and a value based on the type to use as a starting point for user customization.
        /// </summary>
        /// <returns></returns>
        public CustomSqlParameter CreateParameter()
        {
            var type = ParamType switch
            {
                "DATETIME" => SqlDbType.DateTime,
                "INT" => SqlDbType.Int,
                "BIGINT" => SqlDbType.BigInt,
                "SMALLINT" => SqlDbType.SmallInt,
                "TINYINT" => SqlDbType.TinyInt,
                "DECIMAL" => SqlDbType.Decimal,
                "FLOAT" => SqlDbType.Float,
                "MONEY" => SqlDbType.Money,
                "BIT" => SqlDbType.Bit,
                "DATETIME2" => SqlDbType.DateTime2,
                "DATETIMEOFFSET" => SqlDbType.DateTimeOffset,
                "IDS" => SqlDbType.Structured,
                _ => SqlDbType.NVarChar
            };
            object value = ParamType switch
            {
                "DATETIME" => DateTime.Now.Date,
                "INT" => 0,
                "BIGINT" => 0,
                "SMALLINT" => 0,
                "TINYINT" => 0,
                "DECIMAL" => 0.0,
                "FLOAT" => 0.0,
                "MONEY" => 0.0,
                "BIT" => true,
                "DATETIME2" => DateTime.Now.Date,
                "DATETIMEOFFSET" => DateTime.Now.Date,
                _ => ""
            };
            return new CustomSqlParameter()
            {
                Param = new SqlParameter() { ParameterName = ParamName, SqlDbType = type, Value = value},
                UseDefaultValue = true
            };
        }
    }
}