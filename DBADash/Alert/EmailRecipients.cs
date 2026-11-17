using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBADash.Alert
{
    /// <summary>A list of email addresses as people type it: separated by ; , or new lines</summary>
    public static class EmailRecipients
    {
        /// <summary>The addresses, trimmed, without blanks or duplicates (ignoring case)</summary>
        public static List<string> Parse(string text) => string.IsNullOrWhiteSpace(text)
            ? new List<string>()
            : Split(text)
                .Select(a => a.Trim())
                .Where(a => a.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>
        /// Splits at new lines, and at ; and , outside a quoted name or an &lt;address&gt; - so a mailbox copied from a mail client, such as
        /// "Doe, Jane" &lt;jane@example.com&gt;, stays one address
        /// </summary>
        private static IEnumerable<string> Split(string text)
        {
            var token = new StringBuilder();
            var inQuotes = false;
            var inAngle = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c is '\r' or '\n')
                {
                    // A new line always ends an address, so an unclosed quote can't swallow the rest of the list
                    yield return token.ToString();
                    token.Clear();
                    inQuotes = inAngle = false;
                    continue;
                }
                if (inQuotes && c == '\\' && i + 1 < text.Length)
                {
                    token.Append(c).Append(text[++i]); // Escaped character in a quoted name
                    continue;
                }
                if (c == '"' && !inAngle) inQuotes = !inQuotes;
                else if (c == '<' && !inQuotes) inAngle = true;
                else if (c == '>' && !inQuotes) inAngle = false;
                else if (c is ';' or ',' or '\t' && !inQuotes && !inAngle)
                {
                    yield return token.ToString();
                    token.Clear();
                    continue;
                }
                token.Append(c);
            }
            yield return token.ToString();
        }

        /// <summary>The stored form: addresses separated by "; ".  Null when there are none</summary>
        public static string Normalize(string text)
        {
            var addresses = Parse(text);
            return addresses.Count == 0 ? null : string.Join("; ", addresses);
        }

        /// <summary>A single address, optionally with a name: "ops@example.com" or "Ops &lt;ops@example.com&gt;"</summary>
        public static bool IsValid(string address) =>
            MailboxAddress.TryParse(address, out var mailbox)
            && mailbox.Address.IndexOf('@') is > 0 and var at
            && at < mailbox.Address.Length - 1;

        /// <summary>Just an address, with no name or angle brackets - "ops@example.com" - for where the name is set separately</summary>
        public static bool IsBareAddress(string address) =>
            IsValid(address)
            && MailboxAddress.TryParse(address, out var mailbox)
            && string.IsNullOrEmpty(mailbox.Name)
            && string.Equals(mailbox.Address, address.Trim(), StringComparison.OrdinalIgnoreCase);

        /// <summary>The addresses that aren't valid, to show the user</summary>
        public static List<string> Invalid(IEnumerable<string> addresses) => addresses.Where(a => !IsValid(a)).ToList();
    }
}
