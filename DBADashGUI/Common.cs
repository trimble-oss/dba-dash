using Microsoft.Data.SqlClient;
using SpreadsheetLight;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using DBADashGUI.SchemaCompare;
using System.Xml.Linq;
using System.Xml;

namespace DBADashGUI
{
    public class Common : CommonShared
    {
        public static Guid ConnectionGUID => RepositoryDBConnection.ConnectionID;

        public static string ConnectionString => RepositoryDBConnection?.ConnectionString;
        public static readonly string JsonConfigPath = System.IO.Path.Combine(Application.StartupPath, "ServiceConfig.json");
        public static bool FreezeKeyColumn { get; set; }

        public static bool IsApplicationRunning { get; set; } = false; /* Set to true if App is running - used to detect design time mode */
        private static CodeViewer FrmCodeViewer;
        public static RepositoryConnection RepositoryDBConnection { get; set; }

        public static void SetConnectionString(RepositoryConnection connection)
        {
            var builder = new SqlConnectionStringBuilder(connection.ConnectionString)
            {
                ApplicationName = "DBADashGUI"
            };
            connection.ConnectionString = builder.ToString();
            RepositoryDBConnection = connection;
            CommonData.ClearCache();
        }

        public static Guid HighPerformancePowerPlanGUID => Guid.Parse("8C5E7FDA-E8BF-4A96-9A85-A6E23A8C635C");

        public static bool ShowHidden { get; set; }

        public static string DDL(long DDLID)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DDL_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("DDLID", DDLID);
            var bDDL = (byte[])cmd.ExecuteScalar();
            return DBADash.SMOBaseClass.Unzip(bDDL);
        }

        public static void CopyHtmlToClipBoard(string html)
        {
            var enc = Encoding.UTF8;

            var begin = "Version:0.9\r\nStartHTML:{0:000000}\r\nEndHTML:{1:000000}"
                        + "\r\nStartFragment:{2:000000}\r\nEndFragment:{3:000000}\r\n";

            var html_begin = "<html>\r\n<head>\r\n"
                             + "<meta http-equiv=\"Content-Type\""
                             + " content=\"text/html; charset=" + enc.WebName + "\">\r\n"
                             + "<title>HTML clipboard</title>\r\n</head>\r\n<body>\r\n"
                             + "<!--StartFragment-->";

            var html_end = "<!--EndFragment-->\r\n</body>\r\n</html>\r\n";

            var begin_sample = string.Format(begin, 0, 0, 0, 0);

            var count_begin = enc.GetByteCount(begin_sample);
            var count_html_begin = enc.GetByteCount(html_begin);
            var count_html = enc.GetByteCount(html);
            var count_html_end = enc.GetByteCount(html_end);

            var html_total = string.Format(
              begin
              , count_begin
              , count_begin + count_html_begin + count_html + count_html_end
              , count_begin + count_html_begin
              , count_begin + count_html_begin + count_html
              ) + html_begin + html + html_end;

            DataObject obj = new();
            obj.SetData(DataFormats.Html, new MemoryStream(
              enc.GetBytes(html_total)));

            obj.SetData(DataFormats.Text, html);

            Clipboard.SetDataObject(obj, true);
        }

        public static void CopyDataGridViewToClipboard(DataGridView dgv)
        {
            CopyDataGridViewToClipboard(dgv, DashColors.TrimbleBlue, Color.White);
        }

        public static void CopyDataGridViewToClipboard(DataGridView dgv, Color headerBGcolor, Color headerColor)
        {
            var DataGridView1Counts = dgv.Rows.Count;

            StringBuilder html = new();
            html.Append("<table>");

            if (DataGridView1Counts > 0)
            {
                html.Append("<tr>");
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Visible)
                    {
                        html.Append(string.Format("<th style=\"background-color:{1};color:{2};\">{0}</th>", col.HeaderText, headerBGcolor.ToHexString(), headerColor.ToHexString()));
                    }
                }
                html.Append("</tr>");

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    html.Append("<tr>");
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.Visible)
                        {
                            html.AppendFormat("<td style=\"background-color:{1}; color:{2};\">{0}</td>", cell.FormattedValue, ColorTranslator.ToHtml(cell.Style.BackColor), ColorTranslator.ToHtml(cell.Style.ForeColor));
                        }
                    }
                    html.Append("</tr>");
                }
            }
            html.Append("</table>");
            CopyHtmlToClipBoard(html.ToString());
        }

        public static void PromptSaveDataGridView(ref DataGridView dgv)
        {
            PromptSaveDataGridView(dgv);
        }

        public static void PromptSaveDataGridView(DataGridView dgv)
        {
            var defaultFileName = "DBADash_" + DateHelper.AppNow.ToString("yyyyMMdd_HHmmss") + ".xlsx";
            using var ofd = new SaveFileDialog() { FileName = defaultFileName, AddExtension = true, DefaultExt = ".xlsx" };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            if (File.Exists(ofd.FileName))
            {
                if (MessageBox.Show($"Are you sure you want to replace the existing file: {ofd.FileName}", "Confirm Replace", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    File.Delete(ofd.FileName);
                }
                else
                {
                    return;
                }
            }
            Common.SaveDataGridViewToXLSX(ref dgv, ofd.FileName);
            var psi = new ProcessStartInfo(ofd.FileName) { UseShellExecute = true };
            Process.Start(psi);
        }

        public static void SaveDataGridViewToXLSX(ref DataGridView dgv, string path)
        {
            SLDocument sl = new();
            var colIndex = 1;
            var rowIndex = 1;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.Visible)
                {
                    sl.SetCellValue(1, colIndex, col.HeaderText);
                    colIndex++;
                }
            }

            foreach (DataGridViewRow row in dgv.Rows)
            {
                colIndex = 0;
                rowIndex += 1;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (!cell.Visible) continue;
                    colIndex += 1;

                    var cellType = cell.ValueType;
                    var style = sl.CreateStyle();
                    var format = string.IsNullOrEmpty(cell.Style.Format) ? cell.InheritedStyle.Format : cell.Style.Format;
                    format = format switch
                    {
                        "P1" => "0.0%",
                        "P:" or "P2" => "0.00%",
                        _ => "",
                    };
                    if (cellType == typeof(DateTime))
                    {
                        format = "yyyy-MM-dd HH:mm";
                    }
                    if (!cell.Style.ForeColor.IsEmpty || !cell.Style.BackColor.IsEmpty || !string.IsNullOrEmpty(format))
                    {
                        var backColor = cell.Style.BackColor.IsEmpty ? Color.Transparent : cell.Style.BackColor;
                        style.Fill.SetPattern(DocumentFormat.OpenXml.Spreadsheet.PatternValues.Solid, backColor, backColor);
                        style.SetFontColor(cell.Style.ForeColor);
                        style.FormatCode = format;
                        sl.SetCellStyle(rowIndex, colIndex, style);
                    }

                    try
                    {
                        if (cellType == typeof(bool))
                        {
                            sl.SetCellValue(rowIndex, colIndex, (bool)cell.Value);
                        }
                        else if (cellType.IsNumericType())
                        {
                            if (!decimal.TryParse(cell.FormattedValue as string, out var decimalValue))
                            {
                                decimalValue = Convert.ToDecimal(cell.Value);
                            }

                            sl.SetCellValue(rowIndex, colIndex, Convert.ToDecimal(decimalValue));
                        }
                        else if (cellType == typeof(DateTime))
                        {
                            sl.SetCellValue(rowIndex, colIndex, Convert.ToDateTime(cell.Value));
                        }
                        else if (cellType == typeof(byte[]))
                        {
                            sl.SetCellValue(rowIndex, colIndex, Convert.ToString(cell.Value));
                        }
                        else
                        {
                            sl.SetCellValue(rowIndex, colIndex, Convert.ToString(cell.FormattedValue));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
            }
            if (rowIndex > 1)
            {
                var tbl = sl.CreateTable(1, 1, rowIndex, colIndex);
                sl.InsertTable(tbl);
                var headerStyle = sl.CreateStyle();
                headerStyle.Fill.SetPattern(DocumentFormat.OpenXml.Spreadsheet.PatternValues.Solid, DashColors.TrimbleBlue, DashColors.TrimbleBlue);
                headerStyle.SetFontColor(Color.White);
                headerStyle.SetFontBold(true);
                sl.SetCellStyle(1, 1, 1, colIndex, headerStyle);
            }
            sl.AutoFitColumn(1, colIndex, 300);
            sl.SaveAs(path);
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
                dgv.DataSource = dtPivot;
            }
            else
            {
                throw new Exception("Expected 1 row for pivot operation");
            }
        }

        public static readonly DataGridViewCellStyle DataGridViewNumericCellStyle = DataGridViewCellStyle("#,##0.###");
        public static readonly DataGridViewCellStyle DataGridViewNumericCellStyleNoDigits = DataGridViewCellStyle("#,##0");
        public static readonly DataGridViewCellStyle DataGridViewPercentCellStyle = DataGridViewCellStyle("P1");

        public static DataGridViewCellStyle DataGridViewCellStyle(string format)
        {
            return new DataGridViewCellStyle() { Format = format };
        }

        public static void ShowCodeViewer(string sql, string title = "", CodeEditor.CodeEditorModes Language = CodeEditor.CodeEditorModes.SQL)
        {
            FrmCodeViewer?.Close();
            FrmCodeViewer = new CodeViewer
            {
                Language = Language,
                Code = sql,
                Text = "Code Viewer" + (string.IsNullOrEmpty(title) ? "" : " - " + title)
            };
            if (FrmCodeViewer.WindowState == FormWindowState.Minimized)
            {
                FrmCodeViewer.WindowState = FormWindowState.Normal;
            }
            FrmCodeViewer.FormClosed += (s, e) => FrmCodeViewer = null;
            FrmCodeViewer.Show();
        }

        public static Image Base64StringAsImage(string base64String)
        {
            var bytes = Convert.FromBase64String(base64String);

            using MemoryStream ms = new(bytes);
            return Image.FromStream(ms);
        }

        internal static void DownloadFile(string localPath, string url)
        {
            using var client = new HttpClient();
            using var s = client.GetStreamAsync(url);
            using var fs = new FileStream(localPath, FileMode.OpenOrCreate);
            s.Result.CopyTo(fs);
        }

        internal static readonly string TempFilePrefix = "DBADashGUITemp_";

        internal static string GetTempFilePath(string extension)
            => Path.Combine(Path.GetTempPath(), TempFilePrefix + Guid.NewGuid().ToString() + (extension.StartsWith(".") ? extension : "." + extension));

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
                Console.WriteLine("Error deleting temp file:" + ex.ToString());
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

        public static void ShowQueryPlan(string plan)
        {
            if (!IsValidExecutionPlan(plan))
            {
                throw new Exception("Invalid execution plan");
            }
            var path = System.IO.Path.GetTempFileName() + ".sqlplan";
            System.IO.File.WriteAllText(path, plan);
            var psi = new ProcessStartInfo(path) { UseShellExecute = true };
            Process.Start(psi);
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
    }
}