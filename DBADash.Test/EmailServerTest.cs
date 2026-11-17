using DBADash.Alert;
using MailKit.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DBADashConfig.Test
{
    [TestClass]
    public class EmailServerTest
    {
        [TestMethod]
        public void ChannelDetails_DoNotContainSmtpSettings()
        {
            // The SMTP settings live on the Email Server, so a channel's saved details only hold its own settings
            var channel = new EmailNotificationChannel
            {
                ChannelName = "Channel",
                ToEmail = "to@example.com",
                EmailServerID = 3
            };

            var json = JObject.Parse(channel.GetChannelDetailsJson());

            Assert.AreEqual("to@example.com", json["ToEmail"]?.ToString());
            Assert.IsNull(json["EmailServerID"], "The server is stored on the channel row, not in its details");
            Assert.IsNull(json["Host"]);
            Assert.IsNull(json["Password"]);
        }

        [TestMethod]
        public void Validate_RequiresEmailServer()
        {
            var channel = new EmailNotificationChannel
            {
                ChannelName = "Channel",
                ToEmail = "to@example.com"
            };

            var errors = channel.Validate(new ValidationContext(channel)).Select(r => r.ErrorMessage).ToList();

            CollectionAssert.Contains(errors, "Select an Email Server. Use New server... to create one.");
        }

        [TestMethod]
        public void Validate_WithEmailServer_Passes()
        {
            var channel = new EmailNotificationChannel
            {
                ChannelName = "Channel",
                ToEmail = "to@example.com",
                EmailServerID = 1
            };

            var errors = channel.Validate(new ValidationContext(channel)).Select(r => r.ErrorMessage).ToList();

            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void Recipients_ParseSplitsTrimsAndRemovesDuplicates()
        {
            var parsed = EmailRecipients.Parse("a@example.com; b@example.com,\r\n  A@EXAMPLE.com ;;\tc@example.com\n");

            CollectionAssert.AreEqual(new[] { "a@example.com", "b@example.com", "c@example.com" }, parsed);
            Assert.AreEqual(0, EmailRecipients.Parse(null).Count);
            Assert.AreEqual(0, EmailRecipients.Parse(" ; , ").Count);
        }

        [TestMethod]
        public void Recipients_SeparatorsInsideANameOrAddressDontSplit()
        {
            var parsed = EmailRecipients.Parse("\"Doe, Jane\" <jane@example.com>; Ops <ops@example.com>, \"A \\\"quoted; name\\\"\" <q@example.com>\r\nb@example.com");

            CollectionAssert.AreEqual(new[] { "\"Doe, Jane\" <jane@example.com>", "Ops <ops@example.com>", "\"A \\\"quoted; name\\\"\" <q@example.com>", "b@example.com" }, parsed);
            Assert.AreEqual(0, EmailRecipients.Invalid(parsed).Count, "Each is a valid mailbox");
            CollectionAssert.AreEqual(parsed, EmailRecipients.Parse(EmailRecipients.Normalize(string.Join(System.Environment.NewLine, parsed))), "The stored form parses back the same");
        }

        [TestMethod]
        public void Recipients_UnclosedQuoteStopsAtTheLineEnd()
        {
            var parsed = EmailRecipients.Parse("\"Doe <jane@example.com>, x@example.com\nb@example.com");

            Assert.AreEqual("b@example.com", parsed.Last(), "The next line is still read");
        }

        [TestMethod]
        public void Recipients_NormalizeIsTheStoredForm()
        {
            Assert.AreEqual("a@example.com; b@example.com", EmailRecipients.Normalize("a@example.com\r\nb@example.com, a@example.com"));
            Assert.IsNull(EmailRecipients.Normalize("  "));
        }

        [TestMethod]
        public void Recipients_InvalidListsWhatCantBeSentTo()
        {
            var invalid = EmailRecipients.Invalid(new[] { "a@example.com", "not an address", "b@", "@example.com", "Ops <ops@example.com>" });

            CollectionAssert.AreEqual(new[] { "not an address", "b@", "@example.com" }, invalid);
        }

        [TestMethod]
        public void EmailServer_Validate_RequiresNameAndHost()
        {
            var server = new EmailServer();

            var errors = server.Validate(new ValidationContext(server)).Select(r => r.ErrorMessage).ToList();

            CollectionAssert.AreEquivalent(new[] { "Server name is required", "Host is required", "From Email is required" }, errors);
        }

        [TestMethod]
        public void EmailServer_Validate_FromEmailMustBeABareAddress()
        {
            const string expected = "From Email must be just the email address, e.g. alerts@example.com.  The name goes in From.";
            foreach (var invalid in new[] { "dbadash", "DBA Dash <dbadash@example.com>", "<dbadash@example.com>" })
            {
                var server = new EmailServer { EmailServerName = "Shared", Host = "smtp.example.com", FromEmail = invalid };
                var errors = server.Validate(new ValidationContext(server)).Select(r => r.ErrorMessage).ToList();
                CollectionAssert.AreEqual(new[] { expected }, errors, invalid);
            }

            var valid = new EmailServer { EmailServerName = "Shared", Host = "smtp.example.com", FromEmail = "dbadash@example.com" };
            Assert.AreEqual(0, valid.Validate(new ValidationContext(valid)).Count());
        }

        [TestMethod]
        public void EmailServer_Validate_NameFitsTheColumn()
        {
            var server = new EmailServer { EmailServerName = new string('x', EmailServer.MaxNameLength), Host = "smtp.example.com", FromEmail = "a@example.com" };
            Assert.AreEqual(0, server.Validate(new ValidationContext(server)).Count());

            server.EmailServerName += "x";
            CollectionAssert.AreEqual(new[] { "Server name can't be longer than 100 characters" },
                server.Validate(new ValidationContext(server)).Select(r => r.ErrorMessage).ToList());
        }

        [TestMethod]
        public void LegacySettings_ReadFromChannelDetailsSavedBeforeEmailServers()
        {
            // A channel's details as the previous build saved them: SMTP settings alongside the recipient
            const string json = "{\"Host\":\"smtp.legacy.example.com\",\"Port\":465,\"UserName\":\"u\",\"Password\":\"p\",\"SecureSocketOption\":2,\"ToEmail\":\"ops@example.com\",\"FromEmail\":\"dbadash@example.com\",\"From\":\"Legacy\"}";

            var legacy = EmailNotificationChannel.LegacySettings(json);

            Assert.IsNotNull(legacy);
            Assert.AreEqual("smtp.legacy.example.com", legacy.Host);
            Assert.AreEqual(465, legacy.Port);
            Assert.AreEqual("p", legacy.Password);
            Assert.AreEqual(SecureSocketOptions.SslOnConnect, legacy.SecureSocketOption);
            Assert.AreEqual("dbadash@example.com", legacy.FromEmail);
            Assert.AreEqual("Legacy", legacy.From);
            Assert.IsNull(EmailNotificationChannel.LegacySettings("{\"ToEmail\":\"ops@example.com\"}"), "A channel saved since has no settings of its own");
        }
    }
}
