#nullable enable
using Microsoft.Data.SqlClient;
using System;
using System.Security.Cryptography;
using System.Text;

namespace DBADash
{
    /// <summary>
    /// Produces a stable fingerprint identifying the repository database a component is
    /// connected to. Used by the AI service and GUI to verify they are pointed at the
    /// same repository.
    ///
    /// The identity is obtained by asking SQL Server for its own canonical server and
    /// database name (SERVERPROPERTY('ServerName') and DB_NAME()) rather than parsing the
    /// connection string, so that server-name aliases (localhost vs machine name, FQDN,
    /// IP, alias, or named port) don't produce false mismatches.
    /// </summary>
    public static class RepositoryFingerprint
    {
        /// <summary>
        /// Queries SQL Server for its canonical identity ("server|database", lower-cased)
        /// using a short timeout. Throws if the repository can't be reached - callers rely
        /// on the repository being available and should surface the failure rather than
        /// guessing an identity from the connection string.
        /// </summary>
        public static string GetRepositoryIdentity(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is required.", nameof(connectionString));

            var builder = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 5 };
            using var conn = new SqlConnection(builder.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                "SELECT CONVERT(nvarchar(256), SERVERPROPERTY('ServerName')) + N'|' + DB_NAME()", conn)
            { CommandTimeout = 5 };
            var result = cmd.ExecuteScalar() as string;
            if (string.IsNullOrWhiteSpace(result))
                throw new InvalidOperationException("Could not determine repository identity from SQL Server.");
            return result.ToLowerInvariant();
        }

        /// <summary>
        /// Computes a SHA-256 fingerprint of the repository identity. Returns null if the
        /// connection string is blank or the repository can't be queried.
        /// </summary>
        public static string? GetFingerprint(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;
            try
            {
                var identity = GetRepositoryIdentity(connectionString);
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
                return Convert.ToHexString(hash);
            }
            catch
            {
                return null;
            }
        }
    }
}
