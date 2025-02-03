using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.Data.SqlClient;

namespace DBADashGUI.DBADashAlerts
{
    internal class EditNotesLinkColumnInfo:LinkColumnInfo
    {
        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex)
        {
            var alertID = (long)row.Cells["AlertID"].Value;
            var notes = (string)row.Cells["Notes"].Value.DBNullToNull();
            using var frm = new CodeEditorForm();
            frm.Code = notes;
            frm.Syntax = CodeEditor.CodeEditorModes.Markdown;
            if (frm.ShowDialog() != DialogResult.OK) return;
            notes = frm.Code;
            UpdateNotes(alertID, notes);
            row.Cells["Notes"].Value = string.IsNullOrEmpty(notes) ? null : notes;
        }

        public static void UpdateNotes(long alertID, string notes)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            cn.Open();
            using var cmd = new SqlCommand("Alert.Alerts_Notes_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@AlertID", alertID);
            cmd.Parameters.AddWithValue("@Notes",string.IsNullOrEmpty(notes) ? DBNull.Value : notes);
            cmd.ExecuteNonQuery();
        }
    }
}
