using ClosedXML.Excel;
using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.Performance;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using System;
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
            using var cn = new SqlConnection(ConnectionString);
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

        public static string DataTableToHTML(DataTable dt, Color? headerBGcolor = null, Color? headerColor = null)
        {
            headerBGcolor ??= DashColors.TrimbleBlue;
            headerColor ??= DashColors.White;
            var DataGridView1Counts = dt.Rows.Count;

            StringBuilder html = new();
            html.Append("<table>");

            if (DataGridView1Counts > 0)
            {
                html.Append("<tr>");
                foreach (DataColumn col in dt.Columns)
                {
                    html.Append(string.Format("<th style=\"background-color:{1};color:{2};\">{0}</th>", col.Caption, headerBGcolor.Value.ToHexString(), headerColor.Value.ToHexString()));
                }
                html.Append("</tr>");

                foreach (DataRow row in dt.Rows)
                {
                    html.Append("<tr>");
                    foreach (DataColumn col in dt.Columns)
                    {
                        html.AppendFormat("<td>{0}</td>", row[col]);
                    }
                    html.Append("</tr>");
                }
            }
            html.Append("</table>");
            return html.ToString();
        }

        public static void CopyDataTableToClipboard(DataTable dt, Color? headerBGcolor = null, Color? headerColor = null)
        {
            var html = DataTableToHTML(dt, headerBGcolor, headerColor);
            CopyHtmlToClipBoard(html);
        }

        public static void PromptSaveDataGridView(ref DataGridView dgv)
        {
            PromptSaveDataGridView(new DataGridView[] { dgv });
        }

        public static void PromptSaveDataGridView(DataGridView dgv)
        {
            PromptSaveDataGridView(new DataGridView[] { dgv });
        }

        public static void PromptSaveDataGridView(DataGridView[] dgv)
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

            try
            {
                SaveDataGridViewsToXLSX(dgv, ofd.FileName); // Assume there will be no invalid XML characters
            }
            catch (XmlException ex)
            {
                Debug.WriteLine("XmlException exporting to Excel, retrying with invalid XML characters removed", ex.ToString());
                SaveDataGridViewsToXLSX(dgv, ofd.FileName, true); // Try again with invalid XML characters removed
                Debug.WriteLine("Invalid XML characters removed");
            }

            var psi = new ProcessStartInfo(ofd.FileName) { UseShellExecute = true };
            Process.Start(psi);
        }

        public static void PromptSaveDataTableToXLSX(DataTable dt)
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

            try
            {
                SaveDataTableToXLSX(dt, ofd.FileName); // Assume there will be no invalid XML characters
            }
            catch (XmlException ex)
            {
                Debug.WriteLine("XmlException exporting to Excel, retrying with invalid XML characters removed", ex.ToString());
                SaveDataTableToXLSX(dt, ofd.FileName, true); // Try again with invalid XML characters removed
                Debug.WriteLine("Invalid XML characters removed");
            }

            var psi = new ProcessStartInfo(ofd.FileName) { UseShellExecute = true };
            Process.Start(psi);
        }

        public static void SaveDataGridViewsToXLSX(DataGridView[] Grids, string path, bool replaceInvalidChars = false)
        {
            using var workbook = new XLWorkbook();
            var sheetIndex = 1;
            foreach (var dgv in Grids)
            {
                var sheetName = $"Sheet{sheetIndex}";
                if (dgv is DBADashDataGridView dbadashDgv)
                {
                    sheetName = string.IsNullOrEmpty(dbadashDgv.ResultSetName) ? sheetName : dbadashDgv.ResultSetName;
                }

                var sheet = workbook.Worksheets.Add(sheetName);

                // Respect display order and visibility
                var orderedVisibleColumns = dgv.Columns
                    .Cast<DataGridViewColumn>()
                    .Where(c => c.Visible)
                    .OrderBy(c => c.DisplayIndex)
                    .ToList();

                // Headers
                var headerColIndex = 1;
                foreach (var col in orderedVisibleColumns)
                {
                    sheet.Cell(1, headerColIndex).SetValue(col.HeaderText.Replace("\n", " "));
                    headerColIndex++;
                }

                // Rows
                var rowIndex = 1;
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    rowIndex += 1;
                    var colIndex = 1;

                    foreach (var col in orderedVisibleColumns)
                    {
                        var cell = row.Cells[col.Index];
                        var cellType = cell.ValueType;

                        var format = string.IsNullOrEmpty(cell.Style.Format)
                            ? cell.InheritedStyle.Format
                            : cell.Style.Format;
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

                        if (!cell.Style.ForeColor.IsEmpty || !cell.Style.BackColor.IsEmpty ||
                            !string.IsNullOrEmpty(format))
                        {
                            var xlCell = sheet.Cell(rowIndex, colIndex);
                            var backColor = cell.Style.BackColor.IsEmpty ? Color.Transparent : cell.Style.BackColor;
                            xlCell.Style.Fill.SetBackgroundColor(XLColor.FromColor(backColor));
                            xlCell.Style.Font.SetFontColor(XLColor.FromColor(cell.Style.ForeColor));
                            xlCell.Style.NumberFormat.Format = format;
                        }

                        try
                        {
                            if (cell.Value == DBNull.Value)
                            {
                                sheet.Cell(rowIndex, colIndex).SetValue(Convert.ToString(cell.FormattedValue));
                            }
                            else if (cellType == typeof(bool))
                            {
                                sheet.Cell(rowIndex, colIndex).SetValue((bool)cell.Value);
                            }
                            else if (cellType.IsNumericType())
                            {
                                if (!decimal.TryParse(cell.FormattedValue as string, out var decimalValue))
                                {
                                    decimalValue = Convert.ToDecimal(cell.Value);
                                }

                                sheet.Cell(rowIndex, colIndex).SetValue(decimalValue);
                            }
                            else if (cellType == typeof(DateTime))
                            {
                                sheet.Cell(rowIndex, colIndex).SetValue(Convert.ToDateTime(cell.Value));
                            }
                            else if (cellType == typeof(byte[]))
                            {
                                sheet.Cell(rowIndex, colIndex).SetValue(Convert.ToString(cell.Value));
                            }
                            else
                            {
                                sheet.Cell(rowIndex, colIndex).SetValue(replaceInvalidChars
                                    ? Convert.ToString(cell.FormattedValue).StripInvalidXmlChars().Truncate(32767, true)
                                    : Convert.ToString(cell.FormattedValue).Truncate(32767, true));
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }

                        colIndex += 1;
                    }
                }

                var finalColCount = orderedVisibleColumns.Count;
                var table = sheet.Range(sheet.Cell(1, 1).Address, sheet.Cell(rowIndex, finalColCount).Address).CreateTable();
                table.Theme = XLTableTheme.None;
                var header = sheet.Range(1, 1, 1, finalColCount);
                header.Style.Fill.SetBackgroundColor(XLColor.FromColor(DashColors.TrimbleBlue));
                header.Style.Font.SetFontColor(XLColor.White);
                header.Style.Font.SetBold();
                var maxColumnWidth = 150;
                sheet.Columns().AdjustToContents();
                for (var i = 1; i <= finalColCount; i++)
                {
                    sheet.Column(i).Width = Math.Min(sheet.Column(i).Width, maxColumnWidth);
                }
                sheetIndex += 1;
            }
            workbook.SaveAs(path);
        }

        public static void SaveDataTableToXLSX(DataTable dataTable, string excelFilePath, bool replaceInvalidChars = false)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Sheet1");

            // Header
            for (var i = 0; i < dataTable.Columns.Count; i++)
            {
                sheet.Cell(1, i + 1).Value = dataTable.Columns[i].ColumnName;
            }

            // Add the DataTable's data to the Excel file, starting from the second row
            for (var rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
            {
                for (var colIndex = 0; colIndex < dataTable.Columns.Count; colIndex++)
                {
                    var value = dataTable.Rows[rowIndex][colIndex];
                    if (value == DBNull.Value)
                    {
                        continue;
                    }

                    var valueType = value.GetType();
                    if (valueType.IsNumericType())
                    {
                        sheet.Cell(rowIndex + 2, colIndex + 1).SetValue(Convert.ToDecimal(value));
                    }
                    else if (valueType == typeof(DateTime))
                    {
                        sheet.Cell(rowIndex + 2, colIndex + 1).SetValue(Convert.ToDateTime(value));
                    }
                    else if (valueType == typeof(bool))
                    {
                        sheet.Cell(rowIndex + 2, colIndex + 1).SetValue(Convert.ToBoolean(value));
                    }
                    else
                    {
                        sheet.Cell(rowIndex + 2, colIndex + 1).SetValue(value.ToString());
                        sheet.Cell(rowIndex, colIndex).SetValue(replaceInvalidChars
                            ? Convert.ToString(value.ToString()).StripInvalidXmlChars().Truncate(32767, true)
                            : Convert.ToString(value.ToString()).Truncate(32767, true));
                    }
                }
            }

            var table = sheet.Range(sheet.Cell(1, 1).Address, sheet.Cell(dataTable.Rows.Count + 1, dataTable.Columns.Count).Address).CreateTable();
            table.Theme = XLTableTheme.None;
            var header = sheet.Range(1, 1, 1, dataTable.Columns.Count);
            header.Style.Fill.SetBackgroundColor(XLColor.FromColor(DashColors.TrimbleBlue));
            header.Style.Font.SetFontColor(XLColor.White);
            header.Style.Font.SetBold();
            var maxColumnWidth = 150;
            sheet.Columns().AdjustToContents();
            for (var i = 1; i <= dataTable.Columns.Count; i++)
            {
                sheet.Column(i).Width = Math.Min(sheet.Column(i).Width, maxColumnWidth);
            }

            // Save the workbook to the specified file path
            workbook.SaveAs(excelFilePath);
        }

        public static void SaveDataGridViewToXLSX(ref DataGridView dgv, string path, bool replaceInvalidChars = false)
        {
            SaveDataGridViewsToXLSX(new DataGridView[] { dgv }, path, replaceInvalidChars);
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

        public static void ShowQueryPlan(string plan, string fileName = null)
        {
            if (!IsValidExecutionPlan(plan))
            {
                throw new Exception("Invalid execution plan");
            }
            ShowFileContent(plan, fileName, ".sqlplan");
        }

        public static void ShowDeadlockGraph(string dlGraph, string fileName = null)
        {
            if (!IsValidDeadlockGraph(dlGraph))
            {
                throw new Exception("Invalid execution plan");
            }
            ShowFileContent(dlGraph, fileName, ".xdl");
        }

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

        private static string GetFilePath(string fileName, string extension)
        {
            // Ensure the extension is correctly formatted.
            extension = extension.StartsWith(".") ? extension : $".{extension}";

            var tempFileName = string.IsNullOrEmpty(fileName) ? Path.GetTempFileName() : null;

            // Determine the directory based on whether a temp file was needed.
            var directory = Path.GetDirectoryName(tempFileName ?? string.Empty) ?? Path.GetTempPath();

            // If fileName is not provided, use the tempFileName with the correct extension.
            // Otherwise, check if fileName ends with the extension, and append the extension if necessary.
            fileName = string.IsNullOrEmpty(fileName)
                ? $"{Path.GetFileNameWithoutExtension(tempFileName)}{extension}"
                : fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? fileName : $"{fileName}{extension}";

            return Path.Combine(directory, fileName);
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

        public static bool IsValidDeadlockGraph(string xmlString)
        {
            if (string.IsNullOrEmpty(xmlString)) return false;
            try
            {
                var doc = XDocument.Parse(xmlString);

                // Basic validation check :The root node is ShowPlanXML
                return doc.Root is { Name.LocalName: "deadlock" };
            }
            catch (XmlException)
            {
                // The XML is not well-formed
                return false;
            }
        }

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

        public static void AddContextInfoDisplayAsMenu(DBADashDataGridView dgv, string colName)
        {
            const string name = "ContextInfoDisplayAs";
            var mnuDisplayAs = dgv.CellContextMenu.Items.OfType<ToolStripMenuItem>().FirstOrDefault(itm => itm.Name == name);
            if (mnuDisplayAs != null) return;
            {
                mnuDisplayAs = new ToolStripMenuItem("Display As") { Name = name };
                var mnuSep = new ToolStripSeparator();
                foreach (var value in Enum.GetValues<Common.ContextInfoDisplayStyles>())
                {
                    var itm = new ToolStripMenuItem(value.ToString())
                    { Tag = value, Checked = value == Common.ContextInfoDisplayStyle };
                    itm.Click += (_sender, _e) =>
                    {
                        Common.ContextInfoDisplayStyle = (Common.ContextInfoDisplayStyles)(((ToolStripMenuItem)_sender)!).Tag!;
                        foreach (var itm in mnuDisplayAs.DropDownItems.OfType<ToolStripMenuItem>())
                        {
                            itm.Checked = (Common.ContextInfoDisplayStyles)itm.Tag! == Common.ContextInfoDisplayStyle;
                        }

                        var dt = ((DataView)dgv.DataSource).Table;

                        Common.ReplaceBinaryContextInfoColumn(ref dt, true);
                    };
                    mnuDisplayAs.DropDownItems.Add(itm);
                }
                dgv.CellContextMenu.Items.Insert(0, mnuDisplayAs);
                dgv.CellContextMenu.Items.Insert(1, mnuSep);
                dgv.CellContextMenuOpening += (sender, e) =>
                {
                    mnuDisplayAs.Visible = dgv.Columns[e.ColumnIndex].Name == colName;
                    mnuSep.Visible = dgv.Columns[e.ColumnIndex].Name == colName;
                };
            }
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
    }
}