using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Serilog;

namespace DBADash
{
    public class BuildReference
    {
        public static async Task Update(string connectionString)
        {
            var jsonBuildRef = await GetBuildReferenceJson();
            await UpdateBuildReference(connectionString, jsonBuildRef);
        }

        public static async Task UpdateBuildReference(string connectionString, string jsonBuildRef)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("dbo.BuildReference_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("BuildReferenceJson", jsonBuildRef);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<string> GetBuildReferenceJson(string url = "https://dataplat.github.io/assets/dbatools-buildref-index.json")
        {
            using HttpClient client = new();
            return await client.GetStringAsync(url);
        }
    }
}