using DBADash;
using DBADash.Alert;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DBADashConfig.Test
{
    [TestClass]
    public class NotificationChannelTest
    {
        // Test data constants
        private static readonly DateTime DefaultTriggerDate = new DateTime(2026, 2, 9, 14, 30, 45);
        private const string DefaultAlertName = "TEST_ALERT";
        private const string DefaultMessage = "Test alert message";
        private const string DefaultConnectionId = "TestServer";
        private const string DefaultInstanceDisplayName = "TestServer";
        private const string DefaultAlertType = "Test";

        private const string DefaultChannelName = "TestChannel";
        private const string DefaultSmtpHost = "smtp.example.com";
        private const int DefaultSmtpPort = 587;
        private const string DefaultFromEmail = "alerts@example.com";
        private const string DefaultFromName = "DBADash";
        private const string DefaultToEmail = "to@example.com";
        private const string DefaultResolutionToEmail = "resolved@example.com";

        [TestMethod]
        public void TestTriggerDatePlaceholder()
        {
            // Arrange
            var testAlert = CreateTestAlert();
            var emailChannel = new EmailNotificationChannel
            {
                ChannelName = DefaultChannelName
            };

            var template = "Alert triggered at {TriggerDate} on {Instance}";

            // Act
            var result = emailChannel.ReplacePlaceholders(testAlert, template);

            // Assert
            Assert.AreEqual($"Alert triggered at {testAlert.TriggerDate.ToUtcDateTimeOffset().ToStandardString()} on {DefaultInstanceDisplayName}", result);
        }

        [TestMethod]
        public void TestTriggerDatePlaceholderWithTimeZone()
        {
            // Arrange
            var testAlert = CreateTestAlert();
            var emailChannel = new EmailNotificationChannel
            {
                ChannelName = DefaultChannelName
            };

            var template = "Alert triggered at {TriggerDate:America/New_York} on {Instance}";

            // Act
            var result = emailChannel.ReplacePlaceholders(testAlert, template);

            var convertedDate = TimeZoneInfo.ConvertTime(testAlert.TriggerDate.ToUtcDateTimeOffset(), TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));
            // Assert
            Assert.AreEqual($"Alert triggered at {convertedDate.ToStandardString()} on {DefaultInstanceDisplayName}", result);
        }

        [TestMethod]
        public void TestTriggerDatePlaceholderCaseInsensitive()
        {
            // Arrange
            var testAlert = CreateTestAlert(priority: Alert.Priorities.Medium1, triggerDate: new DateTime(2026, 2, 9, 10, 15, 30), message: "Test message", connectionId: "Server1", instanceDisplayName: "Server1");
            var emailChannel = new EmailNotificationChannel
            {
                ChannelName = DefaultChannelName
            };

            // Test various case combinations
            var templates = new[]
            {
                "{triggerdate}",
                "{TRIGGERDATE}",
                "{TriggerDate}",
                "{TrIgGeRdAtE}"
            };
            var formattedDate = testAlert.TriggerDate.ToUtcDateTimeOffset().ToStandardString();
            // Act & Assert
            foreach (var template in templates)
            {
                var result = emailChannel.ReplacePlaceholders(testAlert, template);
                Assert.AreEqual(formattedDate, result, $"Failed for template: {template}");
            }
        }

        [TestMethod]
        public void TestAllPlaceholdersIncludingTriggerDate()
        {
            // Arrange
            var testAlert = CreateTestAlert(
                alertName: "CPU_HIGH",
                priority: Alert.Priorities.Critical,
                triggerDate: new DateTime(2026, 2, 9, 16, 45, 0),
                message: "CPU usage is 95%",
                connectionId: "PROD-SQL-01",
                instanceDisplayName: "Production SQL Server",
                alertType: "Performance");

            var emailChannel = new EmailNotificationChannel
            {
                ChannelName = "ProdAlerts"
            };

            var template = "{Emoji} {AlertKey} {Action} on {Instance} at {TriggerDate}\n{Text}";

            // Act
            var result = emailChannel.ReplacePlaceholders(testAlert, template);
            var formattedDate = testAlert.TriggerDate.ToUtcDateTimeOffset().ToStandardString();

            // Assert
            Assert.Contains(formattedDate, result, "Result should contain formatted trigger date");
            Assert.Contains("CPU_HIGH", result, "Result should contain alert key");
            Assert.Contains("Production SQL Server", result, "Result should contain instance name");
            Assert.Contains("CPU usage is 95%", result, "Result should contain message text");
        }

        [TestMethod]
        public void TestTriggerDateInPlaceholdersList()
        {
            // Arrange
            var emailChannel = new EmailNotificationChannel
            {
                ChannelName = "TestChannel"
            };

            // Act
            var placeholders = emailChannel.Placeholders;

            // Assert
            Assert.Contains("{TriggerDate}", placeholders, "Placeholders list should contain {TriggerDate}");
        }

        [TestMethod]
        public void GetEmailMessageTemplate_ReturnsHtmlTemplate()
        {
            // Ensure the HTML email template is accessible via the EmailNotificationChannel's default HTML property
            var emailChannel = new EmailNotificationChannel() { IsHTML = true };

            var content = emailChannel.GetEmailMessageTemplate();

            Assert.IsFalse(string.IsNullOrWhiteSpace(content), "Embedded HTML template should not be empty");
            StringAssert.Contains(content.ToLowerInvariant(), "<html", "Embedded resource should contain HTML content");
        }

        [TestMethod]
        public void GetEmailMessageTemplate_ReturnsPlainText()
        {
            // Ensure the plain-text email template is returned when IsHTML is false
            var emailChannel = new EmailNotificationChannel() { IsHTML = false };

            var content = emailChannel.GetEmailMessageTemplate();

            // When IsHTML is false ensure the returned template is not HTML
            Assert.IsFalse(string.IsNullOrWhiteSpace(content), "Template should not be empty");
            Assert.IsFalse(content.ToLowerInvariant().Contains("<html"), "Expected plain-text template, not HTML");
        }

        private static Alert CreateTestAlert(
            bool isResolved = false,
            string alertName = DefaultAlertName,
            Alert.Priorities priority = Alert.Priorities.High1,
            DateTime? triggerDate = null,
            string message = DefaultMessage,
            string connectionId = DefaultConnectionId,
            string instanceDisplayName = DefaultInstanceDisplayName,
            string alertType = DefaultAlertType)
        {
            return new Alert
            {
                AlertID = 1,
                AlertName = alertName,
                Priority = priority,
                TriggerDate = triggerDate ?? DefaultTriggerDate,
                Message = message,
                ConnectionID = connectionId,
                InstanceDisplayName = instanceDisplayName,
                AlertType = alertType,
                IsResolved = isResolved
            };
        }

        private static EmailNotificationChannel CreateEmailChannel(
            string toEmail = DefaultToEmail,
            string resolutionToEmail = DefaultResolutionToEmail,
            string channelName = DefaultChannelName,
            string host = DefaultSmtpHost,
            int port = DefaultSmtpPort,
            string fromEmail = DefaultFromEmail,
            string from = DefaultFromName)
        {
            return new EmailNotificationChannel
            {
                ChannelName = channelName,
                Host = host,
                Port = port,
                FromEmail = fromEmail,
                From = from,
                ToEmail = toEmail,
                ResolutionToEmail = resolutionToEmail
            };
        }

        [TestMethod]
        public void GetRecipientEmail_UnresolvedAlert_UsesToEmail()
        {
            // Arrange
            var testAlert = CreateTestAlert(isResolved: false);
            var emailChannel = CreateEmailChannel();

            // Act
            var result = emailChannel.GetRecipientEmail(testAlert);

            // Assert
            Assert.AreEqual("to@example.com", result, "Unresolved alerts should use ToEmail");
        }

        [TestMethod]
        public void GetRecipientEmail_ResolvedAlert_UsesResolutionToEmail()
        {
            // Arrange
            var testAlert = CreateTestAlert(isResolved: true);
            var emailChannel = CreateEmailChannel();

            // Act
            var result = emailChannel.GetRecipientEmail(testAlert);

            // Assert
            Assert.AreEqual("resolved@example.com", result, "Resolved alerts should use ResolutionToEmail when configured");
        }

        [TestMethod]
        public void GetRecipientEmail_ResolvedAlert_FallsBackToToEmail_WhenResolutionToEmailNotConfigured()
        {
            // Arrange
            var testAlert = CreateTestAlert(isResolved: true);
            var emailChannel = CreateEmailChannel(resolutionToEmail: null!);

            // Act
            var result = emailChannel.GetRecipientEmail(testAlert);

            // Assert
            Assert.AreEqual("to@example.com", result, "Resolved alerts should fall back to ToEmail when ResolutionToEmail is not configured");
        }

        [TestMethod]
        public void GetRecipientEmail_ResolvedAlert_FallsBackToToEmail_WhenResolutionToEmailIsEmpty()
        {
            // Arrange
            var testAlert = CreateTestAlert(isResolved: true);
            var emailChannel = CreateEmailChannel(resolutionToEmail: "   ");

            // Act
            var result = emailChannel.GetRecipientEmail(testAlert);

            // Assert
            Assert.AreEqual("to@example.com", result, "Resolved alerts should fall back to ToEmail when ResolutionToEmail is whitespace");
        }
    }
}