using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;

namespace DBADash
{
    public class BuildReference
    {
        private const int MAX_URL_REDIRECT = 10;

        public static async Task UpdateAsync(string connectionString)
        {
            var jsonBuildRef = await GetBuildReferenceJsonAsync();
            await UpdateBuildReferenceAsync(connectionString, jsonBuildRef);
        }

        public static async Task UpdateBuildReferenceAsync(string connectionString, string jsonBuildRef)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("dbo.BuildReference_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("BuildReferenceJson", jsonBuildRef);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<string> GetBuildReferenceJsonAsync(string url = "https://dataplat.dbatools.io/assets/dbatools-buildref-index.json")
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false // Handle manually
            };

            using var client = new HttpClient(handler, disposeHandler: true);
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            client.DefaultRequestHeaders.Add("User-Agent", $"DBADash/{version.Major}.{version.Minor}");

            var response = await client.GetAsync(url);

            // Handle redirects manually. Automatic redirect didn't work
            int redirectCount = 0;
            while ((response.StatusCode == System.Net.HttpStatusCode.Moved ||
                    response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                    response.StatusCode == System.Net.HttpStatusCode.Found ||
                    response.StatusCode == System.Net.HttpStatusCode.SeeOther ||
                    response.StatusCode == System.Net.HttpStatusCode.TemporaryRedirect ||
                    response.StatusCode == System.Net.HttpStatusCode.PermanentRedirect)
                   && redirectCount < MAX_URL_REDIRECT)
            {
                var location = response.Headers.Location;
                if (location == null)
                    break;

                url = location.IsAbsoluteUri ? location.AbsoluteUri : new Uri(new Uri(url), location).AbsoluteUri;
                response.Dispose();
                response = await client.GetAsync(url);
                redirectCount++;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}