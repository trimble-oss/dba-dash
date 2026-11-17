using DBADashGUI.DBADashAlerts;
using DBADash;
using MimeKit;
using Newtonsoft.Json;
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

        [JsonIgnore]
        [Browsable(true)]
        [Category("Email Config")]
        [DisplayName("Email Server (Required)")]
        [Description("The SMTP settings alerts and daily reports are sent with, from the Email Servers dialog on the Alerts page. Use New server... to create one.")]
        // Named by string: the picker lives in the GUI assembly (DBADash), which this assembly can't reference
        [Editor("DBADashGUI.DBADashAlerts.EmailServerEditor, DBADash", "System.Drawing.Design.UITypeEditor, System.Drawing")]
        public override int? EmailServerID { get; set; }

        [Category("Email Config")]
        [DisplayName("To Email")]
        [Description("Email address to send alert to")]
        public string ToEmail { get; set; }

        [Description("Name associated with to email address.  Default to channel name.")]
        [Category("Email Config")]
        public string To { get; set; }

        [Description($"Optional.  Default: {DefaultEmailSubjectTemplate}.\nPlaceholders: {{Emoji}}, {{AlertKey}}, {{Action}}, {{Instance}}, {{ConnectionID}}, {{InstanceAndConnectionID}}, {{Priority}}, {{Title}}, {{TriggerDate}}.")]
        [Category("Email Message"), DisplayName("Email Subject Template")]
        public string EmailSubjectTemplate { get; set; }

        [Description("Optional.  Default: {Text}\nHTML Default: https://github.com/trimble-oss/dba-dash/blob/main/DBADash/Alert/HTMLEmailAlertTemplate.html\nPlaceholders: {Emoji}, {AlertKey}, {Action}, {Instance}, {ConnectionID}, {InstanceAndConnectionID}, {Priority}, {Title}, {Text}, {TriggerDate}, {CloudProvider}, {CloudResourceID}, {CloudRegion}, {CloudAccountID}.")]
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
        /// Determines whether the resolution recipient should be used for the given alert.
        /// </summary>
        /// <param name="alert">The alert to check</param>
        /// <returns>True if the resolution recipient should be used; otherwise false</returns>
        private bool ShouldUseResolutionRecipient(Alert alert)
        {
            return alert.IsResolved && !string.IsNullOrWhiteSpace(ResolutionToEmail);
        }

        /// <summary>
        /// Gets the recipient email address based on alert resolution status and configuration.
        /// </summary>
        /// <param name="alert">The alert to determine the recipient for</param>
        /// <returns>The recipient email address</returns>
        public string GetRecipientEmail(Alert alert)
        {
            return ShouldUseResolutionRecipient(alert) ? ResolutionToEmail : ToEmail;
        }

        /// <summary>
        /// SMTP settings saved on the channel before email servers existed.  Only for a channel the upgrade couldn't move to a server (the
        /// deployment lacked UNMASK - see Script.PostDeployment1.sql), so it keeps sending until it's edited and given one.  Never saved.
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        internal EmailServer LegacyServer { get; set; }

        /// <summary>The SMTP settings in a channel's details from before email servers, or null when it has none</summary>
        internal static EmailServer LegacySettings(string channelDetailsJson)
        {
            var server = JsonConvert.DeserializeObject<EmailServer>(channelDetailsJson);
            return string.IsNullOrWhiteSpace(server?.Host) ? null : server;
        }

        /// <summary>
        /// The server to send through, loaded for each send so an edit to it applies to the next alert (the channel itself is cached by the
        /// service).  A channel without one uses its pre-email-server settings if it has them.  Otherwise fails with a message the user can act on
        /// </summary>
        private EmailServer LoadServer(string connectionString) => EmailServerID is int id
            ? EmailServer.GetServer(id, connectionString)
            : LegacyServer ?? throw new InvalidOperationException($"Email channel '{ChannelName}' has no Email Server. Edit the channel and select one.");

        protected override async Task InternalSendNotificationAsync(Alert alert, string connectionString)
        {
            var server = LoadServer(connectionString);
            var recipientEmail = GetRecipientEmail(alert);
            var useResolutionRecipient = ShouldUseResolutionRecipient(alert);
            var recipientName = useResolutionRecipient
                ? (string.IsNullOrWhiteSpace(ResolutionTo) ? ResolutionToEmail : ResolutionTo)
                : (string.IsNullOrWhiteSpace(To) ? ChannelName : To);

            using var message = new MimeMessage();
            message.From.Add(new MailboxAddress(server.From, server.FromEmail));
            message.To.Add(new MailboxAddress(recipientName, recipientEmail));
            message.Subject = ReplacePlaceholders(alert, GetEmailSubjectTemplate());

            message.Body = new TextPart(IsHTML ? "html" : "plain")
            {
                Text = ReplacePlaceholders(alert, GetEmailMessageTemplate())
            };

            await server.SendAsync(message);
        }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EmailServerID == null)
            {
                yield return new ValidationResult("Select an Email Server. Use New server... to create one.");
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
