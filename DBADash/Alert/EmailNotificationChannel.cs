using DBADashGUI.DBADashAlerts;
using DBADash;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
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

        [Description($"Optional.  Default: {DefaultEmailSubjectTemplate}.\nPlaceholders: {{Emoji}}, {{AlertKey}}, {{Action}}, {{Instance}}, {{ConnectionID}}, {{InstanceAndConnectionID}}, {{Priority}}, {{Title}}, {{TriggerDate}}.")]
        [Category("Email Message"), DisplayName("Email Subject Template")]
        public string EmailSubjectTemplate { get; set; }

        [Description("Optional.  Default: {Text}\nHTML Default: https://github.com/trimble-oss/dba-dash/blob/main/DBADash/Alert/HTMLEmailAlertTemplate.html\nPlaceholders: {Emoji}, {AlertKey}, {Action}, {Instance}, {ConnectionID}, {InstanceAndConnectionID}, {Priority}, {Title}, {Text}, {TriggerDate}.")]
        [Category("Email Message"), DisplayName("Email Message Template")]
        public string EmailMessageTemplate { get; set; }

        [Category("Email Message"), DisplayName("Is HTML?")]
        public bool IsHTML { get; set; }

        [Category("Email Config")]
        [DisplayName("Resolution To Email")]
        [Description("Optional. Email address to send resolved alert notifications to. If blank, uses To Email.")]
        public string ResolutionToEmail { get; set; }

        [Description("Optional. Name associated with resolution email address. Defaults to ResolutionToEmail if specified, otherwise defaults to To (which defaults to channel name).")]
        [Category("Email Config")]
        [DisplayName("Resolution To")]
        public string ResolutionTo { get; set; }

        private const string DefaultEmailSubjectTemplate = "{Emoji} {AlertKey} {Action} on {Instance}";
        private const string DefaultEmailMessageTemplate = "{Text}";

        private static readonly Lazy<string> DefaultHTMLMessageTemplateLazy = new(() =>
            Utility.GetResourceString("DBADash.Alert.HTMLEmailAlertTemplate.html")
        );

        private string DefaultHTMLMessageTemplate => DefaultHTMLMessageTemplateLazy.Value;

        private string GetEmailSubjectTemplate() => string.IsNullOrEmpty(EmailSubjectTemplate)
            ? DefaultEmailSubjectTemplate
            : EmailSubjectTemplate;

        internal string GetEmailMessageTemplate() => string.IsNullOrEmpty(EmailMessageTemplate)
            ? IsHTML ? DefaultHTMLMessageTemplate : DefaultEmailMessageTemplate
            : EmailMessageTemplate;

        /// <summary>
        /// Gets the recipient email address based on alert resolution status and configuration.
        /// </summary>
        /// <param name="alert">The alert to determine the recipient for</param>
        /// <returns>The recipient email address</returns>
        public string GetRecipientEmail(Alert alert)
        {
            var sendToResolutionRecipient = alert.IsResolved && !string.IsNullOrWhiteSpace(ResolutionToEmail);
            return sendToResolutionRecipient ? ResolutionToEmail : ToEmail;
        }

        protected override async Task InternalSendNotificationAsync(Alert alert, string connectionString)
        {
            var recipientEmail = GetRecipientEmail(alert);
            var sendToResolutionRecipient = alert.IsResolved && !string.IsNullOrWhiteSpace(ResolutionToEmail);
            var recipientName = sendToResolutionRecipient
                ? (string.IsNullOrWhiteSpace(ResolutionTo) ? ResolutionToEmail : ResolutionTo)
                : (string.IsNullOrWhiteSpace(To) ? ChannelName : To);

            using var message = new MimeMessage();
            message.From.Add(new MailboxAddress(From, FromEmail));
            message.To.Add(new MailboxAddress(recipientName, recipientEmail));
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
            if (!string.IsNullOrEmpty(EmailMessageTemplate) && !string.IsNullOrEmpty(EmailSubjectTemplate))
            {
                if (!Placeholders.Any(p => EmailMessageTemplate.Contains(p, StringComparison.InvariantCultureIgnoreCase) || EmailSubjectTemplate.Contains(p, StringComparison.InvariantCultureIgnoreCase)))
                {
                    yield return new ValidationResult($"Email templates must contain at least one of the following placeholders: {string.Join(", ", Placeholders)}.  Or leave blank to use the default template.");
                }
            }

            if (!string.IsNullOrWhiteSpace(ResolutionTo) && string.IsNullOrWhiteSpace(ResolutionToEmail))
            {
                yield return new ValidationResult("ResolutionToEmail is required when ResolutionTo is specified. ResolutionTo will only be used when sending to resolved alert recipients.");
            }

            foreach (var validationResult in ValidateBase(validationContext)) yield return validationResult;
        }
    }
}