using DBADashGUI.DBADashAlerts;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
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

        [Description($"Optional.  Default: {DefaultEmailSubjectTemplate}.\nPlaceholders: {{Emoji}}, {{AlertKey}}, {{Action}}, {{Instance}}, {{Priority}}, {{Title}}.")]
        [Category("Email Message"), DisplayName("Email Subject Template")]
        public string EmailSubjectTemplate { get; set; }

        [Description($"Optional.  Default: {DefaultEmailMessageTemplate}\nPlaceholders: {{Emoji}}, {{AlertKey}}, {{Action}}, {{Instance}}, {{Priority}}, {{Title}}, {{Text}}.\nHTML Default:{DefaultHTMLMessageTemplate}")]
        [Category("Email Message"), DisplayName("Email Message Template")]
        public string EmailMessageTemplate { get; set; }

        [Category("Email Message"), DisplayName("Is HTML?")]
        public bool IsHTML { get; set; }

        private const string DefaultEmailSubjectTemplate = "{Emoji} {AlertKey} {Action} on {Instance}";
        private const string DefaultEmailMessageTemplate = "{Text}";

        private const string DefaultHTMLMessageTemplate =
            "<h1><img src=\"{iconurl}\" alt=\"{Priority}\" width=\"30\" height=\"30\"/> {title}<h1/>\r\n<h2>{instance}</h2>\r\n<h3>Priority: {Priority}</h3>\r\n<p>\r\n{text}\r\n</p>\r\n<hr>\r\n<i>Alert generated from <a href=\"https://dbadash.com\">DBA Dash</a></i>\r\n</body>\r\n</html>\r\n";

        private string GetEmailSubjectTemplate() => string.IsNullOrEmpty(EmailSubjectTemplate)
            ? DefaultEmailSubjectTemplate
            : EmailSubjectTemplate;

        private string GetEmailMessageTemplate() => string.IsNullOrEmpty(EmailMessageTemplate)
            ? IsHTML ? DefaultHTMLMessageTemplate : DefaultEmailMessageTemplate
            : EmailMessageTemplate;

        private static string ReplacePlaceholders(Alert alert, string template) => template.Replace("{Emoji}", alert.GetEmoji(), StringComparison.InvariantCultureIgnoreCase)
            .Replace("{AlertKey}", alert.AlertName, StringComparison.InvariantCultureIgnoreCase)
            .Replace("{Action}", alert.Action, StringComparison.InvariantCultureIgnoreCase)
            .Replace("{Title}", $"{alert.AlertName} [{alert.Status}]", StringComparison.InvariantCultureIgnoreCase)
            .Replace("{instance}", alert.ConnectionID, StringComparison.InvariantCultureIgnoreCase)
            .Replace("{Priority}", alert.Priority.ToString(), StringComparison.InvariantCultureIgnoreCase)
            .Replace("{Text}", alert.Message, StringComparison.InvariantCultureIgnoreCase)
            .Replace("{Icon}", alert.GetIcon(), StringComparison.InvariantCultureIgnoreCase)
            .Replace("{IconUrl}", alert.GetIconUrl(), StringComparison.InvariantCultureIgnoreCase);

        protected override async Task InternalSendNotificationAsync(Alert alert, string connectionString)
        {
            using var message = new MimeMessage();
            message.From.Add(new MailboxAddress(From, FromEmail));
            message.To.Add(new MailboxAddress(string.IsNullOrEmpty(To) ? ChannelName : To, ToEmail));
            message.Subject = ReplacePlaceholders(alert, GetEmailSubjectTemplate());

            message.Body = new TextPart(IsHTML ? "html" : "plain")
            {
                Text = ReplacePlaceholders(alert, GetEmailMessageTemplate())
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