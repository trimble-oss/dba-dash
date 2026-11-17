using DBADash.Alert;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DBADashGUI.DBADashAlerts
{
    /// <summary>
    /// Dropdown for an Email Server property in the PropertyGrid: pick a server from the list, or "New server..." to create one.
    /// EmailServerConverter must not supply standard values - the grid shows only one button, and a standard values list would replace this dropdown.
    /// </summary>
    public class EmailServerEditor : UITypeEditor
    {
        private const string NewServerText = "New server...";

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.DropDown;

        public override bool IsDropDownResizable => true;

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider?.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService editorService) return value;

            var current = value as int?;
            using var list = new ListBox { BorderStyle = BorderStyle.None, IntegralHeight = true };
            try
            {
                foreach (var server in EmailServer.GetServerList(Common.ConnectionString))
                {
                    list.Items.Add(new Choice(server.EmailServerID, server.EmailServerName));
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
                return value;
            }
            list.Items.Add(new Choice(null, NewServerText));
            list.SelectedIndex = list.Items.Cast<Choice>().ToList().FindIndex(c => c.ID != null && c.ID == current);
            list.Height = list.ItemHeight * Math.Min(list.Items.Count, 12) + 4;

            Choice chosen = null;
            void Commit()
            {
                chosen = list.SelectedItem as Choice;
                editorService.CloseDropDown();
            }
            list.Click += (_, _) => Commit();
            list.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter) Commit();
            };

            editorService.DropDownControl(list);

            if (chosen == null) return value; // Closed without choosing (Escape or clicked away)
            if (chosen.ID != null) return chosen.ID;

            // New server... - the dropdown has closed, so the server editor opens as a normal dialog
            var newServer = new EmailServer();
            return EmailServersDialog.EditServer(null, newServer) ? newServer.EmailServerID : value;
        }

        private sealed class Choice
        {
            public Choice(int? id, string text)
            {
                ID = id;
                Text = text;
            }

            public int? ID { get; }
            public string Text { get; }
            public override string ToString() => Text;
        }
    }
}
