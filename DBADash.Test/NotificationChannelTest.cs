using DBADash.Alert;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DBADashConfig.Test
{
    [TestClass]
    public class NotificationChannelTest
    {
        private const string dateformat = "yyyy-MM-dd'T'HH:mm:ssXXX";
        private const string utcDateformat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

        [TestMethod]
        public void TestTriggerDatePlaceholder()
        {
            // Arrange
            var testAlert = new Alert
            {
                AlertID = 1,
                AlertName = "TEST_ALERT",
                Priority = Alert.Priorities.High1,
                TriggerDate = new DateTime(2026, 2, 9, 14, 30, 45),
                Message = "Test alert message",
                ConnectionID = "TestServer",
                InstanceDisplayName = "TestServer",
                AlertType = "Test"
            };

            var emailChannel = new EmailNotificationChannel
            {
                ChannelName = "TestChannel"
            };

            var template = "Alert triggered at {TriggerDate} on {Instance}";

            // Act
            var result = emailChannel.ReplacePlaceholders(testAlert, template);

            // Assert
            Assert.AreEqual($"Alert triggered at {testAlert.TriggerDate.ToString(utcDateformat)} on TestServer", result);
        }

        [TestMethod]
        public void TestTriggerDatePlaceholderWithTimeZone()
        {
            const int newYorkOffsetHours = -5; // New York is UTC-5 during standard time
            // Arrange
            var testAlert = new Alert
            {
                AlertID = 1,
                AlertName = "TEST_ALERT",
                Priority = Alert.Priorities.High1,
                TriggerDate = new DateTime(2026, 2, 9, 14, 30, 45),
                Message = "Test alert message",
                ConnectionID = "TestServer",
                InstanceDisplayName = "TestServer",
                AlertType = "Test"
            };

            var emailChannel = new EmailNotificationChannel
            {
                ChannelName = "TestChannel"
            };

            var template = "Alert triggered at {TriggerDate:America/New_York} on {Instance}";

            // Act
            var result = emailChannel.ReplacePlaceholders(testAlert, template);

            // Assert
            Assert.AreEqual($"Alert triggered at {testAlert.TriggerDate.AddHours(newYorkOffsetHours).ToString(dateformat)} on TestServer", result);
        }

        [TestMethod]
        public void TestTriggerDatePlaceholderCaseInsensitive()
        {
            // Arrange
            var testAlert = new Alert
            {
                AlertID = 1,
                AlertName = "TEST_ALERT",
                Priority = Alert.Priorities.Medium1,
                TriggerDate = new DateTime(2026, 2, 9, 10, 15, 30),
                Message = "Test message",
                ConnectionID = "Server1",
                InstanceDisplayName = "Server1",
                AlertType = "Test"
            };

            var emailChannel = new EmailNotificationChannel
            {
                ChannelName = "TestChannel"
            };

            // Test various case combinations
            var templates = new[]
            {
                "{triggerdate}",
                "{TRIGGERDATE}",
                "{TriggerDate}",
                "{TrIgGeRdAtE}"
            };
            var formattedDate = testAlert.TriggerDate.ToString(utcDateformat);
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
            var testAlert = new Alert
            {
                AlertID = 1,
                AlertName = "CPU_HIGH",
                Priority = Alert.Priorities.Critical,
                TriggerDate = new DateTime(2026, 2, 9, 16, 45, 0),
                Message = "CPU usage is 95%",
                ConnectionID = "PROD-SQL-01",
                InstanceDisplayName = "Production SQL Server",
                AlertType = "Performance"
            };

            var emailChannel = new EmailNotificationChannel
            {
                ChannelName = "ProdAlerts"
            };

            var template = "{Emoji} {AlertKey} {Action} on {Instance} at {TriggerDate}\n{Text}";

            // Act
            var result = emailChannel.ReplacePlaceholders(testAlert, template);
            var formattedDate = testAlert.TriggerDate.ToString(utcDateformat);

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
    }
}