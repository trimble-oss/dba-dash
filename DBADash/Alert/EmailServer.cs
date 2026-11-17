using DBADash;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Data.SqlClient;
using MimeKit;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DBADash.Alert
{
    /// <summary>
    /// SMTP settings shared by Email notification channels and daily reports, which send through them and only hold their own recipients.
    /// Stored encrypted in Alert.EmailServer.
    /// </summary>
    public class EmailServer : IValidatableObject
    {
        /// <summary>
        /// Same key as NotificationChannelBase, so a channel's saved details decrypt as a server's details.
        /// The one-time migration in Script.PostDeployment1.sql copies channel details into servers as they are.
        /// </summary>
        private static readonly Guid ClassGuid = new("B5AA3F37-6A53-4852-9364-DF117CC0579A");
        private const string DataMaskedString = "xxxx";

        /// <summary>Alert.EmailServer.EmailServerName.  A longer name would be truncated when saved, and the settings are encrypted with the full name</summary>
        public const int MaxNameLength = 100;

        [JsonIgnore]
        [Browsable(false)]
        public int EmailServerID { get; set; }

        [JsonIgnore]
        [Category("DBA Dash Email Server")]
        [DisplayName("Server Name (Required)")]
        [Description("Name to identify this email server in DBA Dash.  Email channels and daily reports select it by name.")]
        public string EmailServerName { get; set; }

        /// <summary>Number of channels that use this server</summary>
        [JsonIgnore]
        [Browsable(false)]
        public int ChannelCount { get; set; }

        /// <summary>Number of daily reports that use this server</summary>
        [JsonIgnore]
        [Browsable(false)]
        public int ReportCount { get; set; }

        [Category("Email Config")]
        [Description("Email host. e.g. smtp.gmail.com")]
        public string Host { get; set; }

        [Category("Email Config")]
        [Description("Port")]
        public int Port { get; set; } = 587;

        [Category("Email Config")]
        public string UserName { get; set; }

        [PasswordPropertyText(true)]
        [Category("Email Config")]
        public string Password { get; set; }

        [Category("Email Config")]
        public SecureSocketOptions SecureSocketOption { get; set; } = SecureSocketOptions.Auto;

        [Category("Email Config")]
        [DisplayName("From Email")]
        [Description("Email address alerts and daily reports are sent from")]
        public string FromEmail { get; set; }

        [Category("Email Config")]
        [Description("Name associated with from email address")]
        public string From { get; set; } = "DBADash Alerts";

        public override string ToString() => EmailServerName;

        /// <summary>The servers, by name only.  Settings are not decrypted, so this works for users who can't see them</summary>
        public static List<EmailServer> GetServerList(string connectionString)
        {
            var servers = new List<EmailServer>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.EmailServer_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                servers.Add(new EmailServer
                {
                    EmailServerID = rdr.GetInt32(rdr.GetOrdinal("EmailServerID")),
                    EmailServerName = rdr.GetString(rdr.GetOrdinal("EmailServerName")),
                    ChannelCount = rdr.GetInt32(rdr.GetOrdinal("ChannelCount")),
                    ReportCount = rdr.GetInt32(rdr.GetOrdinal("ReportCount"))
                });
            }
            return servers;
        }

        /// <summary>The server with its SMTP settings decrypted</summary>
        public static EmailServer GetServer(int emailServerId, string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.EmailServer_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@EmailServerID", emailServerId);
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read())
            {
                throw new ArgumentException($"Email server {emailServerId} not found");
            }
            var name = rdr.GetString(rdr.GetOrdinal("EmailServerName"));
            var details = rdr.GetString(rdr.GetOrdinal("ServerDetails"));
            if (details == DataMaskedString)
            {
                throw new Exception("Insufficient permission to access email server details");
            }
            try
            {
                details = details.DecryptString(ClassGuid + name);
            }
            catch
            {
                //Ignore. Might be plain text
            }
            var server = JsonConvert.DeserializeObject<EmailServer>(details);
            server.EmailServerID = emailServerId;
            server.EmailServerName = name;
            server.ChannelCount = rdr.GetInt32(rdr.GetOrdinal("ChannelCount"));
            server.ReportCount = rdr.GetInt32(rdr.GetOrdinal("ReportCount"));
            return server;
        }

        /// <summary>Sends a message in its own session.  To send several, use <see cref="ConnectAsync"/> and send them in one session</summary>
        /// <returns>The server's reply to the message, e.g. "2.0.0 OK queued as 1A2B3C" - accepted by the server, which isn't the same as delivered</returns>
        public async Task<string> SendAsync(MimeMessage message)
        {
            await using var connection = await ConnectAsync();
            return await connection.SendAsync(message);
        }

        /// <summary>Connects and logs in, ready to send messages.  Errors say what to check (host, port, security option, credentials)</summary>
        public async Task<Connection> ConnectAsync()
        {
            var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(Host, Port, SecureSocketOption);
                if (!string.IsNullOrEmpty(UserName) || !string.IsNullOrEmpty(Password)) // Authenticate if username or password is supplied
                {
                    await client.AuthenticateAsync(UserName, Password);
                }
                return new Connection(this, client);
            }
            catch (Exception ex)
            {
                client.Dispose();
                var explained = Explain(ex);
                if (explained == null) throw;
                throw explained;
            }
        }

        /// <summary>The SMTP error with what to check, or null to rethrow it as it is</summary>
        private Exception Explain(Exception ex) => ex switch
        {
            AuthenticationException => new Exception($"SMTP authentication failed on {Host}:{Port} (security: {SecureSocketOption}). Check the username and password.", ex),
            SmtpCommandException command => new Exception($"The mail server at {Host}:{Port} (security: {SecureSocketOption}) rejected the request with status {(int)command.StatusCode} ({command.StatusCode}, {command.ErrorCode}). Verify the port, security option and that this host is permitted to relay. {command.Message}", ex),
            SmtpProtocolException => new Exception($"SMTP protocol error connecting to {Host}:{Port} (security: {SecureSocketOption}). The server may not support the selected security option on this port - try a different port/security option combination. {ex.Message}", ex),
            SslHandshakeException => new Exception($"TLS handshake failed connecting to {Host}:{Port} (security: {SecureSocketOption}). Check the security option matches the port (e.g. SslOnConnect for 465, StartTls for 587) and that the server certificate is trusted. {ex.Message}", ex),
            System.Net.Sockets.SocketException => new Exception($"Could not connect to {Host}:{Port} (security: {SecureSocketOption}). Check the host name, port and that a firewall/network is not blocking the connection. {ex.Message}", ex),
            _ => null
        };

        /// <summary>An SMTP session: several messages over one connection and login</summary>
        public sealed class Connection : IAsyncDisposable
        {
            private readonly EmailServer _server;
            private readonly SmtpClient _client;

            internal Connection(EmailServer server, SmtpClient client)
            {
                _server = server;
                _client = client;
            }

            /// <returns>The server's reply to the message</returns>
            public async Task<string> SendAsync(MimeMessage message)
            {
                try
                {
                    return await _client.SendAsync(message);
                }
                catch (Exception ex)
                {
                    var explained = _server.Explain(ex);
                    if (explained == null) throw;
                    throw explained;
                }
            }

            /// <summary>Says goodbye.  What the server accepted stays sent, so a failure here is only logged - reporting it would send it again</summary>
            public async ValueTask DisposeAsync()
            {
                try
                {
                    if (_client.IsConnected)
                    {
                        await _client.DisconnectAsync(true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Email accepted by {Host}:{Port} but the disconnect failed", _server.Host, _server.Port);
                }
                finally
                {
                    _client.Dispose();
                }
            }
        }

        public void Save(string connectionString)
        {
            var errors = Validate(new ValidationContext(this)).ToList();
            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join(Environment.NewLine, errors.Select(e => e.ErrorMessage)));
            }

            var details = JsonConvert.SerializeObject(this).EncryptString(ClassGuid + EmailServerName);
            try
            {
                SaveDetails(connectionString, details);
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601) // Unique key: the name is taken
            {
                throw new ArgumentException($"An email server named '{EmailServerName}' already exists.", ex);
            }
        }

        private void SaveDetails(string connectionString, string details)
        {
            using var cn = new SqlConnection(connectionString);
            if (EmailServerID == 0)
            {
                using var cmd = new SqlCommand("Alert.EmailServer_Add", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@EmailServerName", EmailServerName);
                cmd.Parameters.AddWithValue("@ServerDetails", details);
                var pID = new SqlParameter("@EmailServerID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pID);
                cn.Open();
                cmd.ExecuteNonQuery();
                EmailServerID = (int)pID.Value;
            }
            else
            {
                using var cmd = new SqlCommand("Alert.EmailServer_Upd", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@EmailServerID", EmailServerID);
                cmd.Parameters.AddWithValue("@EmailServerName", EmailServerName);
                cmd.Parameters.AddWithValue("@ServerDetails", details);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Fails with a readable message when channels or daily reports still use the server</summary>
        public static void Delete(string connectionString, int emailServerId)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.EmailServer_Del", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@EmailServerID", emailServerId);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(EmailServerName))
            {
                yield return new ValidationResult("Server name is required");
            }
            else if (EmailServerName.Length > MaxNameLength)
            {
                yield return new ValidationResult($"Server name can't be longer than {MaxNameLength} characters");
            }
            if (string.IsNullOrWhiteSpace(Host))
            {
                yield return new ValidationResult("Host is required");
            }
            if (string.IsNullOrWhiteSpace(FromEmail))
            {
                yield return new ValidationResult("From Email is required");
            }
            else if (!EmailRecipients.IsBareAddress(FromEmail))
            {
                yield return new ValidationResult("From Email must be just the email address, e.g. alerts@example.com.  The name goes in From.");
            }
        }
    }
}
