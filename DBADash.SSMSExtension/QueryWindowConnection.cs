using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// The connection an SSMS query window's results came from, for DBA Dash to make its own connection to the same
    /// instance - to follow a live plan (<see cref="LivePlanCommand"/>).
    ///
    /// The query window (ScriptAndResultsEditorControl, SQLEditors.dll) keeps the open connection in a private
    /// m_connection field - a Microsoft.Data.SqlClient SqlConnection in SSMS 21 and 22, reached through IDbConnection so
    /// this project needn't reference SqlClient - and the UIConnectionInfo it was made from in its Connection property.
    /// The connection string has everything but the password (Persist Security Info is off), so for a SQL login that
    /// comes from UIConnectionInfo.Password. AuthenticationType is 0 for Windows and 1 for a SQL login; anything else is
    /// Microsoft Entra, which isn't supported yet - there's no password to pass on, and the token can't be. All
    /// confirmed against the SSMS 22 install; none of it is a supported API, so a future build that changes it gives
    /// null and a message rather than a crash.
    /// </summary>
    internal sealed class QueryWindowConnection
    {
        private const int WindowsAuthentication = 0;
        private const int SqlAuthentication = 1;

        private QueryWindowConnection(string connectionString, string password)
        {
            ConnectionString = connectionString;
            Password = password;
        }

        /// <summary>The query window's connection string, without a password.</summary>
        public string ConnectionString { get; }

        /// <summary>The SQL login's password, or null for Windows authentication.</summary>
        public string Password { get; }

        /// <summary>
        /// The connection of the query window focus is in.  Null, with <paramref name="reason"/> saying why, when there
        /// isn't one or it can't be used.
        /// </summary>
        public static QueryWindowConnection TryGetFocused(out string reason)
        {
            reason = null;
            var window = ResultsGridReader.FocusedQueryWindow();
            if (window == null)
            {
                reason = "Focus isn't in a query window.";
                return null;
            }

            try
            {
                var connection = GetMember(window, "m_connection") as IDbConnection;
                var info = GetMember(window, "Connection");
                if (connection == null || info == null || string.IsNullOrEmpty(connection.ConnectionString))
                {
                    reason = "The query window isn't connected, or this version of SSMS isn't supported.";
                    return null;
                }

                var connectionString = WithoutPassword(connection.ConnectionString);
                switch (GetMember(info, "AuthenticationType") as int?)
                {
                    case WindowsAuthentication:
                        return new QueryWindowConnection(connectionString, null);
                    case SqlAuthentication:
                        var password = GetMember(info, "Password") as string;
                        if (string.IsNullOrEmpty(password)) password = ToPlainText(GetMember(info, "InMemoryPassword") as SecureString);
                        if (string.IsNullOrEmpty(password))
                        {
                            reason = "The query window's SQL login password isn't available.";
                            return null;
                        }
                        return new QueryWindowConnection(connectionString, password);
                    default:
                        reason = "Live plans from SSMS support Windows and SQL Server authentication.  Microsoft Entra authentication isn't supported yet.";
                        return null;
                }
            }
            catch (Exception ex)
            {
                reason = "Could not read the query window's connection: " + ex.Message;
                return null;
            }
        }

        /// <summary>
        /// The connection string with any password taken out - it would only be there with Persist Security Info on - so
        /// the password is only ever passed on protected (<see cref="VisualizerLauncher.OpenLivePlan"/>), never as plain
        /// text in the file.
        /// </summary>
        private static string WithoutPassword(string connectionString)
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            builder.Remove("Password");
            builder.Remove("PWD");
            return builder.ConnectionString;
        }

        private static string ToPlainText(SecureString secure)
        {
            if (secure == null || secure.Length == 0) return null;

            var bstr = Marshal.SecureStringToBSTR(secure);
            try
            {
                return Marshal.PtrToStringBSTR(bstr);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(bstr);
            }
        }

        private static object GetMember(object target, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null) return property.GetValue(target);
                var field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
            }

            return null;
        }
    }
}
