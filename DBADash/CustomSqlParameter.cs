using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Data;
using System;
using Newtonsoft.Json;

namespace DBADash
{
    /// <summary>
    /// Provides SqlParameter with a boolean to indicate if the default value should be used.  The parameter is omitted in this case to allow the default value to be used.
    /// </summary>
    public class CustomSqlParameter
    {
        [JsonIgnore]
        public SqlParameter Param { get; set; }

        public bool UseDefaultValue { get; set; }

        public string SerializedParam
        {
            get
            {
                var jObject = new JObject
                {
                    { "ParameterName", Param.ParameterName },
                    { "DbType", Param.DbType.ToString() },
                    { "Value", JToken.FromObject(Param.Value) },
                    { "Direction", Param.Direction.ToString() },
                };
                return jObject.ToString();
            }
            set
            {
                var jObject = JObject.Parse(value);
                Param = new SqlParameter
                {
                    ParameterName = jObject["ParameterName"]?.ToString(),
                    DbType = (DbType)Enum.Parse(typeof(DbType),
                        jObject["DbType"]?.ToString() ?? string.Empty),
                    Value = jObject["Value"]?.ToObject<object>(),
                    Direction = (ParameterDirection)Enum.Parse(typeof(ParameterDirection), jObject["Direction"]?.ToString() ?? ParameterDirection.Input.ToString()),
                };
            }
        }
    }
}