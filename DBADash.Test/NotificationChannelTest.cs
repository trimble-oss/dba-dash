using DBADash.Alert;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DBADashConfig.Test
{
    [TestClass]
    public class NotificationChannelTest
    {
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
            Assert.AreEqual("Alert triggered at 2026-02-09T14:30:45Z on TestServer", result);
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

            // Act & Assert
            foreach (var template in templates)
            {
                var result = emailChannel.ReplacePlaceholders(testAlert, template);
                Assert.AreEqual("2026-02-09T10:15:30Z", result, $"Failed for template: {template}");
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

            // Assert
            Assert.IsTrue(result.Contains("2026-02-09T16:45:00Z"), "Result should contain formatted trigger date");
            Assert.IsTrue(result.Contains("CPU_HIGH"), "Result should contain alert key");
            Assert.IsTrue(result.Contains("Production SQL Server"), "Result should contain instance name");
            Assert.IsTrue(result.Contains("CPU usage is 95%"), "Result should contain message text");
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
            Assert.IsTrue(placeholders.Contains("{TriggerDate}"), "Placeholders list should contain {TriggerDate}");
        }
    }
}
