using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace DBADashGUI
{
    public class Common : CommonShared
    {
        public static string ConnectionString;
        public static readonly string JsonConfigPath = System.IO.Path.Combine(Application.StartupPath, "ServiceConfig.json");

        public static Dictionary<Int32, string> DateGroups = new Dictionary<Int32, string>() {
                {0,"None" },
                { 1, "1min" },
                { 2, "2min" },
                { 5, "5min" },
                { 10, "10min" },
                { 15, "15min" },
                { 30, "30min" },
                { 60, "1hr" },
                { 120, "2hrs" },
                { 240, "4hrs" },
                { 360, "6hrs" },
                { 720, "12hrs" },
                { 1440, "1 Day" },
                { 2880, "2 Days" },
                { 10880, "7 Days" },
                { 20160, "14 Days" }
            };

        public static Guid HighPerformancePowerPlanGUID
        {
            get
            {
                return Guid.Parse("8C5E7FDA-E8BF-4A96-9A85-A6E23A8C635C");
            }
        }

        public static string DateGroupString(Int32 mins)
        {
            return (DateGroups.Where(k => k.Key == mins).First()).Value;
        }

        public static void AddDateGroups(ToolStripDropDownButton tsRoot, EventHandler click)
        {
            foreach (var dg in Common.DateGroups)
            {
                var ts = new ToolStripMenuItem(dg.Value)
                {
                    Tag = dg.Key
                };
                ts.Click += click;
                tsRoot.DropDownItems.Add(ts);
            }
        }

        public static Int32 DateGrouping(Int32 Mins, Int32 MaxPoints)
        {
            Int32 lastMins = 0;

            foreach (var mins in Common.DateGroups.OrderBy(k => k.Key)
                .Select(k => k.Key)
                .ToList())
            {
                double div = mins == 0 ? 0.2 : mins;
                if (Mins / div < MaxPoints)
                {
                    return mins;
                }
                lastMins = mins;
            }
            return lastMins;
        }

        public static string DDL(Int64 DDLID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (var cmd = new SqlCommand("dbo.DDL_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("DDLID", DDLID);
                    var bDDL = (byte[])cmd.ExecuteScalar();
                    return DBADash.SchemaSnapshotDB.Unzip(bDDL);
                }
            }
        }

        public static Int32 UtcOffset
        {
            get
            {
                return (Int32)DateTime.Now.Subtract(DateTime.UtcNow).TotalMinutes;
            }
        }



        public static DataTable ConvertUTCToLocal(ref DataTable dt, List<string> convertCols = null)
        {
            List<Int32> convertColsIdx = new List<int>();
            if (convertCols == null || convertCols.Count == 0)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.DataType == typeof(DateTime))
                    {
                        convertColsIdx.Add(col.Ordinal);
                    }
                }
            }
            else
            {
                foreach (string col in convertCols)
                {
                    convertColsIdx.Add(dt.Columns[col].Ordinal);
                }
            }
            if (convertColsIdx.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    foreach (var col in convertColsIdx)
                    {
                        if (row[col] != DBNull.Value)
                        {
                            row[col] = ((DateTime)row[col]).ToLocalTime();
                        }
                    }
                }
            }
            return dt;

        }

        public static void CopyHtmlToClipBoard(string html)
        {
            Encoding enc = Encoding.UTF8;

            string begin = "Version:0.9\r\nStartHTML:{0:000000}\r\nEndHTML:{1:000000}"
              + "\r\nStartFragment:{2:000000}\r\nEndFragment:{3:000000}\r\n";

            string html_begin = "<html>\r\n<head>\r\n"
              + "<meta http-equiv=\"Content-Type\""
              + " content=\"text/html; charset=" + enc.WebName + "\">\r\n"
              + "<title>HTML clipboard</title>\r\n</head>\r\n<body>\r\n"
              + "<!--StartFragment-->";

            string html_end = "<!--EndFragment-->\r\n</body>\r\n</html>\r\n";

            string begin_sample = String.Format(begin, 0, 0, 0, 0);

            int count_begin = enc.GetByteCount(begin_sample);
            int count_html_begin = enc.GetByteCount(html_begin);
            int count_html = enc.GetByteCount(html);
            int count_html_end = enc.GetByteCount(html_end);

            string html_total = String.Format(
              begin
              , count_begin
              , count_begin + count_html_begin + count_html + count_html_end
              , count_begin + count_html_begin
              , count_begin + count_html_begin + count_html
              ) + html_begin + html + html_end;

            DataObject obj = new DataObject();
            obj.SetData(DataFormats.Html, new MemoryStream(
              enc.GetBytes(html_total)));

            Clipboard.SetDataObject(obj, true);
        }


        public static void CopyDataGridViewToClipboard(DataGridView dgv, string headerBGcolor = "#696969", string headerColor = "#FFFFFF")
        {

            var DataGridView1Counts = dgv.Rows.Count;


            StringBuilder html = new StringBuilder();
            html.Append("<table>");

            if (DataGridView1Counts > 0)
            {
                html.Append("<tr>");
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Visible)
                    {
                        html.Append(string.Format("<th style=\"background-color:{1};color:{2};\">{0}</th>", col.HeaderText, headerBGcolor, headerColor));
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
            string defaultFileName = "DBADash_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";
            using (var ofd = new SaveFileDialog() { FileName=defaultFileName,  AddExtension = true, DefaultExt = ".xlsx" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
        
                    if (File.Exists(ofd.FileName))
                    {
                        if (MessageBox.Show(string.Format("Are you sure you want to replace the existing file: {0}", ofd.FileName), "Confirm Replace", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
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
            }
        }

        public static void SaveDataGridViewToXLSX(ref DataGridView dgv,string path, SLTableStyleTypeValues tableStyle= SLTableStyleTypeValues.Light8)
        {
            SLDocument sl = new SLDocument();
            Int32 colIndex = 1;
            Int32 rowIndex = 1;
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
                    if (cell.Visible)
                    {
                        colIndex += 1;
                        SLStyle style = sl.CreateStyle();
                        string format = string.IsNullOrEmpty(cell.Style.Format) ? cell.InheritedStyle.Format : cell.Style.Format;
                        format = format switch
                        {
                            "P1" => "0.0%",
                            "P:" or "P2" => "0.00%",
                            _ => "",
                        };
                        if (!cell.Style.ForeColor.IsEmpty || !cell.Style.BackColor.IsEmpty || !string.IsNullOrEmpty(format))
                        {                            
                            style.Fill.SetPattern(DocumentFormat.OpenXml.Spreadsheet.PatternValues.Solid, cell.Style.BackColor.IsEmpty ? Color.Transparent : cell.Style.BackColor, cell.Style.ForeColor);                          
                            style.FormatCode = format;
                            sl.SetCellStyle(rowIndex, colIndex, style);
                        }
                        var cellType = cell.Value==null ? typeof(System.String) : cell.Value.GetType();
                        if (cellType == typeof(decimal) || cellType == typeof(float))
                        {
                            sl.SetCellValue(rowIndex, colIndex, Convert.ToDecimal(cell.Value));
                        }
                        else if(cellType == typeof(int) || cellType== typeof(long) || cellType == typeof(short) || cellType == typeof(uint) || cellType == typeof(ulong) || cellType == typeof(ushort))
                        {
                            sl.SetCellValue(rowIndex, colIndex, Convert.ToInt64(cell.Value));
                        }
                        else
                        {
                            sl.SetCellValue(rowIndex, colIndex, Convert.ToString(cell.Value));
                        }
                       
                    }
                }
            }
            if (rowIndex > 1)
            {
                var tbl = sl.CreateTable(1, 1, rowIndex, colIndex);
                tbl.SetTableStyle(tableStyle);
                sl.InsertTable(tbl);
            }
            sl.AutoFitColumn(1, colIndex, 300);
            sl.SaveAs(path);
        }

        public static DialogResult ShowInputDialog(ref string input, string title)
        {
            System.Drawing.Size size = new System.Drawing.Size(400, 70);
            Form inputBox = new Form
            {
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                ClientSize = size,
                Text = title,
                MaximizeBox = false,
                MinimizeBox = false
            };

            System.Windows.Forms.TextBox textBox = new TextBox
            {
                Size = new System.Drawing.Size(size.Width - 10, 23),
                Location = new System.Drawing.Point(5, 5),
                Text = input
            };
            inputBox.Controls.Add(textBox);

            Button okButton = new Button
            {
                DialogResult = System.Windows.Forms.DialogResult.OK,
                Name = "okButton",
                Size = new System.Drawing.Size(75, 23),
                Text = "&OK",
                Location = new System.Drawing.Point(size.Width - 80 - 80, 39)
            };
            inputBox.Controls.Add(okButton);

            Button cancelButton = new Button
            {
                DialogResult = System.Windows.Forms.DialogResult.Cancel,
                Name = "cancelButton",
                Size = new System.Drawing.Size(75, 23),
                Text = "&Cancel",
                Location = new System.Drawing.Point(size.Width - 80, 39)
            };
            inputBox.Controls.Add(cancelButton);

            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;

            DialogResult result = inputBox.ShowDialog();
            input = textBox.Text;
            return result;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            hex.Append("0x");
            foreach (byte b in ba)
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

        public static DataGridViewCellStyle DataGridViewNumericCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###" };
        public static DataGridViewCellStyle DataGridViewNumericCellStyleNoDigits = new DataGridViewCellStyle() { Format = "#,##0" };       

       
                    
    }
}
