using DBADashGUI.DBADashAlerts;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DBADash.Alert
{
    public class EmailNotificationChannel : NotificationChannelBase
    {
        public override NotificationChannelTypes NotificationChannelType => NotificationChannelTypes.Email;

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
        [DisplayName("To Email")]
        [Description("Email address to send alert to")]
        public string ToEmail { get; set; }

        [Category("Email Config")]
        [DisplayName("From Email")]
        [Description("Email address alert is sent from")]
        public string FromEmail { get; set; }

        [Description("Name associated with from email address")]
        [Category("Email Config")]
        public string From { get; set; } = "DBADash Alerts";

        [Description("Name associated with to email address.  Default to channel name.")]
        [Category("Email Config")]
        public string To { get; set; }

        protected override async Task InternalSendNotificationAsync(Alert alert, string connectionString)
        {
            using var message = new MimeMessage();
            message.From.Add(new MailboxAddress(From, FromEmail));
            message.To.Add(new MailboxAddress(string.IsNullOrEmpty(To) ? ChannelName : To, ToEmail));
            message.Subject = alert.AlertName + " triggered on " + alert.ConnectionID;

            message.Body = new TextPart("plain")
            {
                Text = alert.Message
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(Host, Port, SecureSocketOption);
            if (!string.IsNullOrEmpty(UserName) || !string.IsNullOrEmpty(Password)) // Authenticate if username or password is supplied
            {
                await client.AuthenticateAsync(UserName, Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Host))
            {
                yield return new ValidationResult("Host is required");
            }

            if (string.IsNullOrEmpty(ToEmail))
            {
                yield return new ValidationResult("Email to is required");
            }

            foreach (var validationResult in ValidateBase(validationContext)) yield return validationResult;
        }
    }
}