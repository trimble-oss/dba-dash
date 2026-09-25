using DBADashGUI.Viewers;
using ClosedXML.Excel;
using DBADash;
using DBADash.Deadlock;
using DBADash.QueryPlan;
using DBADash.QueryPlan.Model;
using DBADash.Deadlock.Model;
using Humanizer;
using DBADashGUI.CustomReports;
using DBADashGUI.Performance;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace DBADashGUI
{
    public class Common : CommonShared
    {
        public static Guid ConnectionGUID => RepositoryDBConnection.ConnectionID;

        public static string ConnectionString => RepositoryDBConnection?.ConnectionString;
        public static readonly string JsonConfigPath = Path.Combine(Application.StartupPath, "ServiceConfig.json");
        public static bool FreezeKeyColumn { get; set; }

        public static bool IsApplicationRunning { get; set; } = false; /* Set to true if App is running - used to detect design time mode */

        public static RepositoryConnection RepositoryDBConnection { get; set; }

        public static void SetConnectionString(RepositoryConnection connection)
        {
            if (connection != null)
            {
                var builder = new SqlConnectionStringBuilder(connection.ConnectionString)
                {
                    ApplicationName = "DBADashGUI"
                };
                connection.ConnectionString = builder.ToString();
            }
            RepositoryDBConnection = connection;
            CommonData.ClearCache();
        }

        public static Guid HighPerformancePowerPlanGUID => Guid.Parse("8C5E7FDA-E8BF-4A96-9A85-A6E23A8C635C");

        public static bool ShowHidden { get; set; }

        public static string DDL(long DDLID)
        {
            using var cn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand("dbo.DDL_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("DDLID", DDLID);
            var bDDL = (byte[])cmd.ExecuteScalar();
            return DBADash.SMOBaseClass.Unzip(bDDL);
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new(ba.Length * 2);
            hex.Append("0x");
            foreach (var b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static string StripInvalidFileNameChars(string path)
        {
            return string.Join("_", path.Split(Path.GetInvalidFileNameChars()));
        }

        public static void PivotDGV(ref DataGridView dgv)
        {
            var dtPivot = new DataTable();
            dtPivot.Columns.Add("Attribute");
            dtPivot.Columns.Add("Value");
            if (dgv.Rows.Count == 1)
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    var row = dtPivot.NewRow();
                    row["Attribute"] = col.HeaderText;
                    row["Value"] = dgv.Rows[0].Cells[col.Index].Value;
                    dtPivot.Rows.Add(row);
                }
                dgv.Columns.Clear();
                dgv.AutoGenerateColumns = true;
                dgv.DataSource = new DataView(dtPivot);
            }
            else
            {
                throw new Exception("Expected 1 row for pivot operation");
            }
        }

        public static void PivotDGV(ref DBADashDataGridView dgv)
        {
            var dgv1 = dgv as DataGridView;
            PivotDGV(ref dgv1);
        }

        public static readonly DataGridViewCellStyle DataGridViewNumericCellStyle = DataGridViewCellStyle("#,##0.###");
        public static readonly DataGridViewCellStyle DataGridViewNumericCellStyleNoDigits = DataGridViewCellStyle("#,##0");
        public static readonly DataGridViewCellStyle DataGridViewPercentCellStyle = DataGridViewCellStyle("P1");
        public static readonly DataGridViewCellStyle DataGridViewDateCellStyle = DataGridViewCellStyle("g");

        /// <summary>
        /// 25 = 25.0% instead of 0.25 = 25.0% - used for cases where percentage is stored as whole number rather than fraction
        /// </summary>
        public static readonly DataGridViewCellStyle DataGridViewWholeNumberPercentCellStyle = DataGridViewCellStyle("#,##0.0'%'");

        public static DataGridViewCellStyle DataGridViewCellStyle(string format)
        {
            return new DataGridViewCellStyle() { Format = format };
        }

        /// <summary>
        /// Delete temp files generated
        /// </summary>
        internal static void TryDeleteTempFiles()
        {
            try
            {
                var pattern = TempFilePrefix + "*";
                foreach (var f in Directory.EnumerateFiles(Path.GetTempPath(), pattern))
                {
                    File.Delete(f);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting temp file:" + ex);
            }
        }

        internal static void ConfigureService()
        {
            var psi = new ProcessStartInfo(Properties.Resources.ServiceConfigToolName) { UseShellExecute = true };
            Process.Start(psi);
            Application.Exit();
        }

        public static string AsciiProgressBar(double progress, int totalWidth = 10)
        {
            var totalWidthDouble = totalWidth * 2;  // Double width for half block resolution
            var filledWidth = (int)Math.Round(totalWidthDouble * progress);

            var fullBlocks = filledWidth / 2;
            var fullBlockPart = new string('█', fullBlocks);

            // Half blocks
            var halfBlocks = filledWidth % 2;
            var halfBlockPart = halfBlocks > 0 ? "▓" : "";

            // Empty blocks
            var emptyBlocks = totalWidthDouble / 2 - fullBlocks - halfBlocks;
            var emptyBlockPart = new string('░', emptyBlocks);

            return "[" + fullBlockPart + halfBlockPart + emptyBlockPart + "]";
        }

        public static void SearchGoogle(string searchQuery)
        {
            var url = $"https://www.google.com/search?q={Uri.EscapeDataString(searchQuery)}";

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Opens a plan in the viewer.  <paramref name="context"/> says where it came from, where the
        /// caller knows - it names the instance on an AI analysis and records one against it.  Optional
        /// because a plan can arrive from a file or from a grid with no instance behind it.
        /// </summary>
        public static void ShowQueryPlan(string plan, string fileName = null, DBADashContext context = null) =>
            ViewerLauncher.ShowQueryPlan(plan, fileName, DBADashViewerHost.Create(context));

        /// <summary>
        /// Open a deadlock graph in the built-in viewer.
        /// </summary>
        /// <param name="context">
        /// The instance the graph came from, where the caller knows it.  Supplying it lights up the actions
        /// that need to go back to the source instance - collecting the current plan for a statement, and the
        /// Query Store lookup.  Without it the viewer still shows everything the graph itself carries.
        /// </param>
        public static void ShowDeadlockGraph(string dlGraph, string fileName = null, DBADashContext context = null) =>
            ViewerLauncher.ShowDeadlockGraph(dlGraph, fileName, DBADashViewerHost.Create(context));


        private static void ShowFileContent(string content, string fileName, string extension)
        {
            try
            {
                var path = GetFilePath(fileName, extension);
                File.WriteAllText(path, content);
                var psi = new ProcessStartInfo(path) { UseShellExecute = true };
                using var process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                // Log or handle exceptions as needed.
                throw new InvalidOperationException($"Failed to show content: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Validate that a string is a valid SQL Server XML execution plan.  Basic validation to check that XML is well-formed and has a root node of ShowPlanXML
        /// </summary>
        /// <param name="xmlString">String to validate</param>
        /// <returns></returns>
        public static bool IsValidExecutionPlan(string xmlString)
        {
            if (string.IsNullOrEmpty(xmlString)) return false;
            try
            {
                var doc = XDocument.Parse(xmlString);

                // Basic validation check :The root node is ShowPlanXML
                return doc.Root is { Name.LocalName: "ShowPlanXML" };
            }
            catch (XmlException)
            {
                // The XML is not well-formed
                return false;
            }
        }

        /// <summary>
        /// Validate that a string is a SQL Server deadlock graph.
        ///
        /// This previously required a root element of "deadlock", which rejected the deadlock-list
        /// form SSMS writes when saving a .xdl, and the extended events event envelope.  The parser
        /// accepts a deadlock element wherever it appears, so all three now work.
        /// </summary>
        public static bool IsValidDeadlockGraph(string xmlString) => DeadlockParser.IsDeadlockXml(xmlString);

        public static int[] GetCustomColors()
        {
            return new[]
            {
                DashColors.Warning.ToWin32(),
                DashColors.Yellow.ToWin32(),
                DashColors.YellowLight.ToWin32(),
                DashColors.Fail.ToWin32(),
                DashColors.Red.ToWin32(),
                DashColors.RedLight.ToWin32(),
                DashColors.RedPale.ToWin32(),
                DashColors.Success.ToWin32(),
                DashColors.Green.ToWin32(),
                DashColors.GreenLight.ToWin32(),
                DashColors.Information.ToWin32(),
                DashColors.TrimbleBlue.ToWin32(),
                DashColors.TrimbleBlueDark.ToWin32(),
                DashColors.TrimbleGray.ToWin32(),
                DashColors.GrayLight.ToWin32(),
                DashColors.AvoidanceZone.ToWin32(),
                DashColors.BluePale.ToWin32(),
            };
        }

        public static Color? ShowColorDialog(Color currentColor)
        {
            using var colorDialog = new ColorDialog
            {
                Color = currentColor,
                CustomColors = GetCustomColors()
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                return colorDialog.Color;
            }

            return null;
        }

        public static void ShowColorDialog(Control panel, Control textBox)
        {
            var selectedColor = ShowColorDialog(panel.BackColor);
            if (!selectedColor.HasValue) return;
            panel.BackColor = selectedColor.Value;
            textBox.Text = selectedColor.Value.ToHexString();
        }

        public static void AdjustColorBrightness(Control panel, Control textBox, float correctionFactor)
        {
            var newColor = panel.BackColor.ChangeColorBrightness(correctionFactor);
            panel.BackColor = newColor;
            textBox.Text = newColor.ToHexString();
        }

        public const float ColorBrightnessIncrement = 0.05f;

        public enum ContextInfoDisplayStyles
        {
            Hex,
            UTF8String,
            UnicodeString,
            ASCIIString,
            Guid,
            Int
        }

        public static Common.ContextInfoDisplayStyles ContextInfoDisplayStyle { get; set; } = Common.ContextInfoDisplayStyles.Hex;

        public static void ReplaceBinaryContextInfoColumn(ref DataTable dt, bool redo = false)
        {
            if (redo && dt.Columns.Contains("context_info_bin") && dt.Columns.Contains("context_info"))
            {
                dt.Columns.Remove("context_info");
                dt.Columns["context_info_bin"]!.ColumnName = "context_info";
            }
            if (!dt.Columns.Contains("context_info")) return;
            if (dt.Columns.Contains("context_info_bin")) return;
            dt.Columns["context_info"]!.ColumnName = "context_info_bin";
            dt.Columns.Add("context_info", typeof(string));
            try
            {
                foreach (DataRow row in dt.Rows)
                {
                    string contextInfo;
                    if (row["context_info_bin"] == DBNull.Value)
                    {
                        contextInfo = string.Empty;
                    }
                    else
                    {
                        try
                        {
                            var contextInfoBin = (byte[])row["context_info_bin"];
                            contextInfo = ContextInfoDisplayStyle switch
                            {
                                ContextInfoDisplayStyles.Hex => contextInfoBin.ToHexString(true),
                                ContextInfoDisplayStyles.UTF8String => Encoding.UTF8.GetString(contextInfoBin),
                                ContextInfoDisplayStyles.UnicodeString => Encoding.Unicode.GetString(contextInfoBin),
                                ContextInfoDisplayStyles.ASCIIString => Encoding.ASCII.GetString(contextInfoBin),
                                ContextInfoDisplayStyles.Guid => contextInfoBin.Length == 16
                                    ? new Guid(contextInfoBin).ToString()
                                    : "Error converting value: Invalid Length for GUID",
                                ContextInfoDisplayStyles.Int => contextInfoBin.Length switch
                                {
                                    4 => contextInfoBin.ByteArrayToIntBigEndian().ToString(),
                                    8 => contextInfoBin.ByteArrayToLongBigEndian().ToString(),
                                    _ => "Error converting value: Invalid Length for INT/BIGINT"
                                },
                                _ => ((byte[])row["context_info_bin"]).ToHexString(true)
                            };
                        }
                        catch (Exception ex)
                        {
                            contextInfo = "Error converting value: " + ex.Message;
                        }
                    }

                    row["context_info"] = contextInfo;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private const string ContextInfoDisplayAsMenuName = "ContextInfoDisplayAs";

        /// <summary>
        /// Build the shared "Display As" context menu (plus its separator) for a context_info column and insert it at the top of the grid's
        /// cell context menu. <paramref name="onStyleChanged"/> is invoked after the selected display style changes so the caller can re-render.
        /// </summary>
        private static ToolStripMenuItem BuildContextInfoDisplayAsMenu(DBADashDataGridView dgv, Action onStyleChanged, out ToolStripSeparator separator)
        {
            var mnuDisplayAs = new ToolStripMenuItem("Display As") { Name = ContextInfoDisplayAsMenuName };
            separator = new ToolStripSeparator();
            foreach (var value in Enum.GetValues<Common.ContextInfoDisplayStyles>())
            {
                var itm = new ToolStripMenuItem(value.ToString())
                { Tag = value, Checked = value == Common.ContextInfoDisplayStyle };
                itm.Click += (_sender, _e) =>
                {
                    Common.ContextInfoDisplayStyle = (Common.ContextInfoDisplayStyles)(((ToolStripMenuItem)_sender)!).Tag!;
                    foreach (var child in mnuDisplayAs.DropDownItems.OfType<ToolStripMenuItem>())
                    {
                        child.Checked = (Common.ContextInfoDisplayStyles)child.Tag! == Common.ContextInfoDisplayStyle;
                    }

                    onStyleChanged();
                };
                mnuDisplayAs.DropDownItems.Add(itm);
            }
            dgv.CellContextMenu.Items.Insert(0, mnuDisplayAs);
            dgv.CellContextMenu.Items.Insert(1, separator);
            return mnuDisplayAs;
        }

        public static void AddContextInfoDisplayAsMenu(DBADashDataGridView dgv, string colName)
        {
            if (dgv.CellContextMenu.Items.OfType<ToolStripMenuItem>().Any(itm => itm.Name == ContextInfoDisplayAsMenuName)) return;
            var mnuDisplayAs = BuildContextInfoDisplayAsMenu(dgv, () =>
            {
                var dt = ((DataView)dgv.DataSource).Table;
                Common.ReplaceBinaryContextInfoColumn(ref dt, true);
            }, out var mnuSep);
            dgv.CellContextMenuOpening += (sender, e) =>
            {
                mnuDisplayAs.Visible = dgv.Columns[e.ColumnIndex].Name == colName;
                mnuSep.Visible = dgv.Columns[e.ColumnIndex].Name == colName;
            };
        }

        /// <summary>
        /// Adds the context_info "Display As" menu to a pivoted (Attribute/Value) grid, where context_info appears as a row rather than a column.
        /// <paramref name="sourceWithBin"/> must still contain the context_info_bin column so the value can be re-converted; <paramref name="refresh"/>
        /// is called to re-render the grid after the display style changes.
        /// </summary>
        public static void AddContextInfoDisplayAsMenuForPivot(DBADashDataGridView dgv, DataTable sourceWithBin, Action refresh)
        {
            if (dgv.CellContextMenu.Items.OfType<ToolStripMenuItem>().Any(itm => itm.Name == ContextInfoDisplayAsMenuName)) return;
            if (!sourceWithBin.Columns.Contains("context_info_bin")) return; // No binary context_info present - nothing to re-convert
            var contextInfoAttribute = "context_info".Titleize();
            var mnuDisplayAs = BuildContextInfoDisplayAsMenu(dgv, () =>
            {
                Common.ReplaceBinaryContextInfoColumn(ref sourceWithBin, true);
                refresh();
            }, out var mnuSep);
            dgv.CellContextMenuOpening += (sender, e) =>
            {
                var isContextInfoRow = e.RowIndex >= 0 && e.RowIndex < dgv.Rows.Count
                    && dgv.Rows[e.RowIndex].DataBoundItem is DataRowView drv
                    && drv.Row.Table.Columns.Contains("Attribute")
                    && string.Equals(Convert.ToString(drv["Attribute"]), contextInfoAttribute, StringComparison.Ordinal);
                mnuDisplayAs.Visible = isContextInfoRow;
                mnuSep.Visible = isContextInfoRow;
            };
        }

        // Very simple grid diff tool.  Relies on rows being in order.
        public static void HighlightGridDifferences(DataGridView grid1, DataGridView grid2)
        {
            var rowCount1 = grid1.RowCount;
            var rowCount2 = grid2.RowCount;
            var minRowCount = Math.Min(rowCount1, rowCount2);
            var highlightBackColor = DashColors.RedPale;
            var highlightForeColor = DashColors.TrimbleGray;

            // Compare cells in the common rows
            for (var row = 0; row < minRowCount; row++)
            {
                if (row >= grid1.RowCount || row >= grid2.RowCount) continue; // Ensure index is within bounds
                for (var col = 0; col < grid1.ColumnCount && col < grid2.ColumnCount; col++)
                {
                    var value1 = grid1.Rows[row].Cells[col].Value;
                    var value2 = grid2.Rows[row].Cells[col].Value;

                    if ((value1 == null && value2 != null) || (value1 != null && !value1.Equals(value2)))
                    {
                        grid1.Rows[row].Cells[col].SetColor(highlightBackColor, highlightForeColor);
                        grid2.Rows[row].Cells[col].SetColor(highlightBackColor, highlightForeColor);
                    }
                }
                // Handle potential column count differences within the common rows
                for (var col = grid1.ColumnCount; col < grid2.ColumnCount && row < grid2.RowCount; col++)
                {
                    // Highlight extra columns in grid2
                    grid2.Rows[row].Cells[col].SetColor(highlightBackColor, highlightForeColor);
                }
                for (var col = grid2.ColumnCount; col < grid1.ColumnCount && row < grid1.RowCount; col++)
                {
                    // Highlight extra columns in grid1
                    grid1.Rows[row].Cells[col].SetColor(highlightBackColor, highlightForeColor);
                }
            }

            // Highlight extra rows in grid1
            for (var row = minRowCount; row < rowCount1; row++)
            {
                for (var col = 0; col < grid1.ColumnCount; col++)
                {
                    // Indicate extra row in grid1
                    grid1.Rows[row].Cells[col].SetColor(highlightBackColor, highlightForeColor);
                }
            }

            // Highlight extra rows in grid2
            for (var row = minRowCount; row < rowCount2; row++)
            {
                for (var col = 0; col < grid2.ColumnCount; col++)
                {
                    // Indicate extra row in grid2
                    grid2.Rows[row].Cells[col].SetColor(highlightBackColor, highlightForeColor);
                }
            }
        }

        public static void ShowObjectExecutionSummary(DBADashContext context, Form parent)
        {
            Form objectExecutionForm;
            objectExecutionForm = new Form()
            {
                Text = context.ObjectName,
                Width = parent.Width / 2,
                Height = parent.Height / 2
            };
            var oes = new ObjectExecutionSummary() { Dock = DockStyle.Fill, UseGlobalTime = false };
            oes.SetContext(context);
            objectExecutionForm.Controls.Add(oes);

            objectExecutionForm.ShowSingleInstance();
        }

        public static void KeyPressAllowNumericOnly(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != '-')
                e.Handled = true;

            switch (e.KeyChar)
            {
                // only allow one decimal point
                case '.' when ((TextBox)sender).Text.IndexOf('.') > -1:
                // only allow one minus sign
                case '-' when ((TextBox)sender).Text.IndexOf('-') > -1:
                    e.Handled = true;
                    break;
            }
        }
    }
}