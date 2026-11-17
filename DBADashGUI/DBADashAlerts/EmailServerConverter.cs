using DBADash.Alert;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace DBADashGUI.DBADashAlerts
{
    /// <summary>
    /// TypeConverter that shows an Email Server property by name in the PropertyGrid.  (None) is stored as null.
    /// The list to choose from comes from EmailServerEditor, which adds "New server...".  No standard values here: the grid shows
    /// a single button, and a standard values list would replace the editor's dropdown.
    /// </summary>
    public class EmailServerConverter : TypeConverter
    {
        private const string NoneText = "(None)";
        private static List<EmailServer> _servers;
        private static string _serversConnectionString;

        /// <summary>Called after email servers change to force a fresh load next time.</summary>
        public static void Invalidate() => _servers = null;

        /// <summary>The servers in the repository the GUI is connected to - loaded again after switching repositories, as IDs repeat across them</summary>
        private static List<EmailServer> Servers
        {
            get
            {
                var connectionString = Common.ConnectionString;
                if (_servers == null || _serversConnectionString != connectionString)
                {
                    _servers = EmailServer.GetServerList(connectionString);
                    _serversConnectionString = connectionString;
                }
                return _servers;
            }
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => false;

        /// <summary>Keeps the value's text read-only - servers are chosen from the editor's dropdown, not typed</summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value == null) return NoneText;
                if (value is int id) return NameOf(id) ?? $"(Unknown server {id})";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>The server's name.  One not in the list was created since it was loaded (e.g. by another user), so the list is loaded again</summary>
        private static string NameOf(int id)
        {
            var name = Servers.FirstOrDefault(s => s.EmailServerID == id)?.EmailServerName;
            if (name != null) return name;
            Invalidate();
            return Servers.FirstOrDefault(s => s.EmailServerID == id)?.EmailServerName;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string name)
            {
                if (name == NoneText) return null;
                return Servers.FirstOrDefault(s => s.EmailServerName == name)?.EmailServerID;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }
}
