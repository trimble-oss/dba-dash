using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using DBADash;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.Raw;
using static Microsoft.SqlServer.TransactSql.ScriptDom.SensitivityClassification;

namespace DBADashServiceConfig
{
    public partial class ManageCustomCollections : Form
    {
        private Dictionary<string, CustomCollection> _customCollections;

        public Dictionary<string, CustomCollection> CustomCollections
        {
            get => _customCollections;
            set => _customCollections = value.DeepCopy();
        }

        public string ConnectionString { get; set; }
        private bool IsScheduleValid = true;

        private readonly List<DataGridViewColumn> CustomCols = new()
        {
            new DataGridViewTextBoxColumn()
            {
                Name = "Name", HeaderText = "Name", DataPropertyName = "Name",
                SortMode = DataGridViewColumnSortMode.Automatic, ReadOnly = true, Width = 150
            },
            new DataGridViewTextBoxColumn()
            {
                Name = "ProcedureName", HeaderText = "Procedure Name", DataPropertyName = "ProcedureName",
                SortMode = DataGridViewColumnSortMode.Automatic, Width = 150
            },
            new DataGridViewTextBoxColumn()
            {
                Name = "Schedule", HeaderText = "Schedule", DataPropertyName = "Schedule",
                SortMode = DataGridViewColumnSortMode.Automatic, Width = 60
            },
            new DataGridViewTextBoxColumn()
            {
                Name = "CommandTimeout", HeaderText = "Command Timeout", DataPropertyName = "CommandTimeout",
                SortMode = DataGridViewColumnSortMode.Automatic, Width = 70
            },
            new DataGridViewCheckBoxColumn()
            {
                Name = "RunOnServiceStart", HeaderText = "Run On Service Start", DataPropertyName = "RunOnServiceStart",
                SortMode = DataGridViewColumnSortMode.Automatic, Width = 70
            },
            new DataGridViewLinkColumn()
            {
                Name = "Copy", HeaderText = "Copy", Text = "Copy", UseColumnTextForLinkValue = true,
                SortMode = DataGridViewColumnSortMode.Automatic, Width = 50
            },
            new DataGridViewLinkColumn()
            {
                Name = "Delete", HeaderText = "Delete", Text = "Delete", UseColumnTextForLinkValue = true,
                SortMode = DataGridViewColumnSortMode.Automatic, Width = 50
            },
            new DataGridViewLinkColumn()
            {
                Name = "GetScript", HeaderText = "Get Script", Text = "Get Script", UseColumnTextForLinkValue = true,
                SortMode = DataGridViewColumnSortMode.Automatic, Width = 80
            }
        };

        public ManageCustomCollections()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private void CustomCollections_Load(object sender, EventArgs e)
        {
            LoadData();
            Task.Run(LoadProcs);
        }

        private void LoadProcs()
        {
            try
            {
                using var cn = new SqlConnection(ConnectionString);
                using var cmd = new SqlCommand(@"SELECT QUOTENAME(s.name) + '.' + QUOTENAME(p.name) AS ProcName
FROM sys.procedures p
JOIN sys.schemas s ON s.schema_id = p.schema_id
ORDER BY ProcName", cn);
                cn.Open();
                using var rdr = cmd.ExecuteReader();

                var items = new List<object>();
                while (rdr.Read())
                {
                    items.Add(rdr.GetString(0));
                }

                cboProcedureName.Invoke(new Action(() =>
                {
                    cboProcedureName.Items.Clear();
                    cboProcedureName.Items.AddRange(items.ToArray());
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading stored procedures:" + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadData()
        {
            dgvCustom.Columns.Clear();
            dgvCustom.AutoGenerateColumns = false;
            dgvCustom.Columns.AddRange(CustomCols.ToArray());
            dgvCustom.Rows.Clear();
            foreach (var kvp in CustomCollections)
            {
                dgvCustom.Rows.Add(kvp.Key, kvp.Value.ProcedureName, kvp.Value.Schedule, kvp.Value.CommandTimeout,
                    kvp.Value.RunOnServiceStart);
            }
            dgvCustom.ApplyTheme();
        }

        private void DgvCustom_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var name = dgvCustom.Rows[e.RowIndex].Cells["Name"].Value.ToString();
            if (e.ColumnIndex == dgvCustom.Columns["Copy"]!.Index)
            {
                // Handle Edit operation
                CopyRecord(name);
            }
            else if (e.ColumnIndex == dgvCustom.Columns["Delete"]!.Index)
            {
                // Handle Delete operation
                DeleteRecord(name);
            }
            else if (e.ColumnIndex == dgvCustom.Columns["GetScript"]!.Index)
            {
                if (timer1.Enabled)
                {
                    MessageBox.Show("A previous Get Script request is currently running. Please wait until the current operation completes.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var strRetention = "14";
                if (CommonShared.ShowInputDialog(ref strRetention,
                        "How many days retention should be used?", default, "0  = The previous snapshot collected is removed.  The table is not partitioned.\n>0  = Table is partitioned and DBA Dash will automatically handle data retention.\n\nDaily partitions are used if retention is 1 year or less, otherwise monthly partitions are created.  Retention can be adjusted later in the GUI.") !=
                    DialogResult.OK)
                {
                    return;
                }
                if (!int.TryParse(strRetention, out var retention) || retention < 0)
                {
                    MessageBox.Show("Invalid retention specified", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Task.Run(() => TryGetScript(name, retention));
            }
        }

        private void TryGetScript(string name, int retentionDays)
        {
            try
            {
                StartStopGetScript(true);
                GetScript(name, retentionDays);
            }
            catch (Exception ex) when (ex.Message ==
                                       "Invoke or BeginInvoke cannot be called on a control until the window handle has been created.")
            {
                // user has closed the window, ignore
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error getting script:" + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                StartStopGetScript(false);
            }
        }

        private void StartStopGetScript(bool start)
        {
            lblGetScript.Invoke(() =>
            {
                lblGetScript.Text = baseText ?? lblGetScript.Text;
                lblGetScript.Visible = start;
                timer1.Enabled = start;
            });
        }

        private void GetScript(string name, int retentionDays)
        {
            if (CustomCollection.IsValidName(name) == false) throw new Exception("Invalid name");
            var usePartitions = retentionDays > 0;
            if (!_customCollections.ContainsKey(name)) return;
            var collection = _customCollections[name];
            using var cn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(collection.ProcedureName, cn)
            { CommandType = CommandType.StoredProcedure, CommandTimeout = collection.CommandTimeout ?? 60 };
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            var schema = rdr.GetColumnSchema();

            var colsSchema = string.Join("," + Environment.NewLine,
                schema.Select(s =>
                    $"\t[{s.ColumnName.Replace("]", "]]")}] {s.GetDataTypeString()}"));

            var colsSelect = ColsSelect(schema, "");
            var colsSelectWithUDPrefix = ColsSelect(schema, "\tUD.");
            var colsSelectWithTPrefix = ColsSelect(schema, "T.");

            var sb = new StringBuilder();
            /* Boilerplate */
            sb.AppendLine("/*");
            sb.AppendLine($"\tDBA Dash script to setup custom collection for {name} custom collection");
            sb.AppendLine();
            sb.AppendLine($"\tGenerated: {DateTime.Now}");
            sb.AppendLine($"\tVersion {Upgrade.CurrentVersion().ToString(3)}");
            sb.AppendLine();
            sb.AppendLine(
                "\tThis script is designed to be used as a template to allow fast setup of custom data collections.");
            sb.AppendLine(
                "\tIt is recommended that you review the script and make any changes required for your environment.");
            sb.AppendLine("\tRun this script in your DBA Dash repository database");
            sb.AppendLine(
                $"\tThe collected data will be inserted into a table called UserData.{name} with additional columns for InstanceID and SnapshotDate");
            sb.AppendLine("\tThe InstanceID column can be joined to dbo.Instances");
            sb.AppendLine(
                $"\tThe data collected in previous snapshots is replaced.  Remove the DELETE statement in the UserData.{name}_Upd stored procedure to keep old snapshots.");
            sb.AppendLine("\tIf you remove the DELETE statement, consider data retention for the collection");
            sb.AppendLine();
            sb.AppendLine("\t** WARNING: Consider the cost of running your custom data collection **");
            sb.AppendLine("*/");
            sb.AppendLine();

            /* Data retention & partitioning function/scheme creation */
            if (usePartitions)
            {
                sb.AppendLine("/*** SET RETENTION ***");
                sb.AppendLine("How long should we keep the collected data?");
                sb.AppendLine(
                    "Daily partitions will be created to make it efficient to clear out old data.  If retention is over 365 days, monthly partitions will be used instead");
                sb.AppendLine("*/");
                sb.AppendLine($"DECLARE @RetentionDays INT = {retentionDays}");
                sb.AppendLine("/* Create partition function and scheme to make it efficient to clear out old data */");
                sb.AppendLine($"CREATE PARTITION FUNCTION [PF_UserData_{name}](DATETIME2) AS RANGE RIGHT FOR VALUES()");
                sb.AppendLine(
                    $"CREATE PARTITION SCHEME [PS_UserData_{name}] AS PARTITION [PF_UserData_{name}] ALL TO([PRIMARY])");
                sb.AppendLine(
                    $"EXEC dbo.DataRetention_Upd @SchemaName ='UserData', @TableName = '{name}',@RetentionDays=@RetentionDays, @Validate=0");
            }

            /* Create table type used to import data */
            sb.AppendLine("/* Create user defined type so we can pass the collected data to our stored procedure */");
            sb.AppendLine($"CREATE TYPE UserData.[{name}] AS TABLE (");
            sb.AppendLine(colsSchema);
            sb.AppendLine(");");
            sb.AppendLine("GO");

            sb.AppendLine("/* Create table to store the collected data */");
            sb.AppendLine("GO");

            /* Create table to store the collected data */
            sb.AppendLine($"CREATE TABLE UserData.[{name}] (");
            sb.AppendLine("\t[InstanceID] INT NOT NULL,");
            sb.AppendLine("\t[SnapshotDate] DATETIME2 NOT NULL,");
            sb.AppendLine(colsSchema);
            sb.AppendLine(
                "\t/* Warning: Script just creates a clustered index on InstanceID and SnapshotDate. Consider replacing this, adding a primary key and other indexes if required */");
            sb.AppendLine($"\tINDEX IX_UserData_{name} CLUSTERED(InstanceID,SnapshotDate)");
            sb.AppendLine(")" + (usePartitions ? $" ON [PS_UserData_{name}](SnapshotDate);" : ""));
            sb.AppendLine("GO");

            /* Create procedure to handle our data import */
            sb.AppendLine("/* Create procedure to handle our data import */");
            sb.AppendLine("GO");
            sb.AppendLine($"CREATE PROCEDURE UserData.[{name}_Upd]");
            sb.AppendLine("(");
            sb.AppendLine("\t@InstanceID INT,");
            sb.AppendLine("\t@SnapshotDate DATETIME2,");
            sb.AppendLine($"\t@{name} [{name}] READONLY");
            sb.AppendLine(")");
            sb.AppendLine("AS");
            sb.AppendLine("SET XACT_ABORT ON");
            sb.AppendLine("SET NOCOUNT ON");

            /* Remove the previous data if retention is 0 */
            if (!usePartitions)
            {
                sb.AppendLine("BEGIN TRAN");
                sb.AppendLine(
                    "/* !! Remove the data associated with the previous collection.  Remove to keep old snapshots.  If removed, consider retention. !!*/");
                sb.AppendLine($"DELETE UserData.[{name}]");
                sb.AppendLine("WHERE InstanceID = @InstanceID");
                sb.AppendLine();
            }
            sb.AppendLine($"INSERT INTO UserData.[{name}]");
            sb.AppendLine("(");
            sb.AppendLine("\t[InstanceID],");
            sb.AppendLine("\t[SnapshotDate],");
            sb.AppendLine(colsSelect);
            sb.AppendLine(")");
            sb.AppendLine("SELECT");
            sb.AppendLine("\t@InstanceID AS InstanceID,");
            sb.AppendLine("\t@SnapshotDate AS SnapshotDate,");
            sb.AppendLine(colsSelect);
            sb.AppendLine($"FROM @{name}");
            sb.AppendLine();
            if (!usePartitions)
            {
                sb.AppendLine("COMMIT TRAN");
            }
            sb.AppendLine();
            sb.AppendLine(
                $"EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,\n\t\t@Reference = 'UserData.{name}',\n\t\t@SnapshotDate = @SnapshotDate");
            sb.AppendLine("GO");
            sb.AppendLine();

            /* Create example report */
            sb.AppendLine("/*");
            sb.AppendLine("\tCustom report example. Returns data collected associated with the last collection");
            sb.AppendLine("*/");
            sb.AppendLine("GO");
            sb.AppendLine($"CREATE PROC [UserReport].[{name} Example]");
            sb.AppendLine("(");
            sb.AppendLine("\t@InstanceIDs IDs READONLY,");
            sb.Append("\t@SnapshotDate DATETIME2 = NULL");
            sb.AppendLine(")");
            sb.AppendLine("AS");
            sb.AppendLine("WITH T AS (");
            sb.AppendLine("\tSELECT\tI.InstanceDisplayName AS Instance,");
            sb.AppendLine("\t\t\tUD.SnapshotDate,");
            sb.AppendLine(colsSelectWithUDPrefix + ",");
            sb.AppendLine("\t\t\t/* Get the last snapshot for each instance (RNK=1) */");
            sb.AppendLine("\t\t\tRANK() OVER(PARTITION BY UD.InstanceID ORDER BY UD.SnapshotDate DESC) RNK");
            sb.AppendLine($"\tFROM UserData.{name} UD");
            sb.AppendLine("\tJOIN dbo.Instances I ON I.InstanceID = UD.InstanceID");
            sb.AppendLine("\tWHERE EXISTS(SELECT 1");
            sb.AppendLine("\t\t\t\tFROM @InstanceIDs T");
            sb.AppendLine("\t\t\t\tWHERE T.ID = UD.InstanceID");
            sb.AppendLine("\t\t\t\t)");
            sb.AppendLine("\tAND (@SnapshotDate IS NULL OR UD.SnapshotDate = @SnapshotDate)");
            sb.AppendLine(")");
            sb.AppendLine("SELECT T.Instance,");
            sb.AppendLine("\tT.SnapshotDate,");
            sb.AppendLine(colsSelectWithTPrefix);
            sb.AppendLine("FROM T");
            sb.AppendLine("WHERE T.RNK = 1");
            sb.AppendLine("OPTION(RECOMPILE)");
            sb.AppendLine("GO");

            /* Create snapshot report with drill down */
            sb.AppendLine($"CREATE PROC [UserReport].[{name} Snapshots]");
            sb.AppendLine("(");
            sb.AppendLine("\t@InstanceID INT,");
            sb.AppendLine("\t@FromDate DATETIME2,");
            sb.AppendLine("\t@ToDate DATETIME2");
            sb.AppendLine(")");
            sb.AppendLine("AS");
            sb.AppendLine("SELECT\tUD.SnapshotDate,");
            sb.AppendLine("\t\t\tCOUNT(*) AS [Row Count]");
            sb.AppendLine($"FROM UserData.[{name}] UD");
            sb.AppendLine("WHERE UD.InstanceID = @InstanceID");
            sb.AppendLine("\tAND UD.SnapshotDate >= @FromDate");
            sb.AppendLine("\tAND UD.SnapshotDate < @ToDate");
            sb.AppendLine("GROUP BY UD.SnapshotDate");
            sb.AppendLine("ORDER BY UD.SnapshotDate");
            sb.AppendLine("GO");

            sb.AppendLine("INSERT INTO dbo.CustomReport(SchemaName,ProcedureName,MetaData)");
            sb.AppendLine(
                $"VALUES('UserReport','{name} Snapshots','{{ \"CustomReportResults\": {{  \"0\": {{   \"LinkColumns\": {{ \"SnapshotDate\": {{ \"$type\": \"DrillDownLinkColumnInfo\", \"ReportProcedureName\": \"{name} Example\",  \"ColumnToParameterMap\": {{ \"@SnapshotDate\": \"SnapshotDate\" }} }} }} }} }} }}')");

            /* Create initial partitions */
            if (usePartitions)
            {
                sb.AppendLine("/* Create initial partitions */");
                sb.AppendLine("EXEC dbo.Partitions_Add");
            }

            /* Create script to remove objects created */
            sb.AppendLine("/*");
            sb.AppendLine("--Script to remove objects that were created");
            sb.AppendLine($"DROP PROC UserData.[{name}_Upd]");
            sb.AppendLine("GO");
            sb.AppendLine($"DROP TYPE UserData.[{name}]");
            sb.AppendLine("GO");
            sb.AppendLine($"DROP TABLE UserData.[{name}]");
            sb.AppendLine("GO");
            sb.AppendLine($"DROP PROC [UserReport].[{name} Example]");
            sb.AppendLine("GO");
            sb.AppendLine($"DROP PROC [UserReport].[{name} Snapshots]");
            sb.AppendLine("GO");
            sb.AppendLine(
                $"DELETE dbo.CustomReport WHERE SchemaName = 'UserReport' AND ProcedureName IN('{name} Snapshots','{name} Example')");
            sb.AppendLine("GO");
            if (usePartitions)
            {
                sb.AppendLine($"DROP PARTITION SCHEME [PS_UserData_{name}]");
                sb.AppendLine("GO");
                sb.AppendLine($"DROP PARTITION FUNCTION [PF_UserData_{name}]");
                sb.AppendLine("GO");
                sb.AppendLine($"DELETE dbo.DataRetention WHERE SchemaName = 'UserData' AND TableName = '{name}'");
            }
            sb.AppendLine("*/");

            StartStopGetScript(false);
            this.Invoke(() =>
            {
                Clipboard.SetText(sb.ToString());
                MessageBox.Show("Script copied to clipboard", "Success", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            });
        }

        private static string ColsSelect(IEnumerable<DbColumn> schema, string prefix = "")
        {
            return string.Join("," + Environment.NewLine,
                schema.Select(s => $"\t{prefix}[{s.ColumnName.Replace("]", "]]")}]"));
        }

        private void CopyRecord(string name)
        {
            cboProcedureName.Text = CustomCollections[name].ProcedureName;
            txtCron.Text = CustomCollections[name].Schedule;
            chkDefaultTimeout.Checked = CustomCollections[name].CommandTimeout == null;
            numTimeout.Value = CustomCollections[name].CommandTimeout == null ? numTimeout.Value : Convert.ToDecimal(CustomCollections[name].CommandTimeout);
            chkRunOnStart.Checked = CustomCollections[name].RunOnServiceStart;
            txtName.Text = name;
        }

        private void DeleteRecord(string name)
        {
            if (!CustomCollections.ContainsKey(name) || MessageBox.Show($"Delete {name} collection?", "Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            CustomCollections.Remove(name);
            LoadData();
        }

        private bool ValidateInput()
        {
            var validated = true;
            if (string.IsNullOrEmpty(txtName.Text))
            {
                errorProvider1.SetError(txtName, "Name is required");
                validated = false;
            }
            else if (CustomCollection.IsValidName(txtName.Text) == false)
            {
                errorProvider1.SetError(txtName, "Name is invalid");
                validated = false;
            }
            if (string.IsNullOrEmpty(cboProcedureName.Text))
            {
                errorProvider1.SetError(cboProcedureName, "Procedure Name is required");
                validated = false;
            }

            if (IsScheduleValid == false)
            {
                errorProvider1.SetError(txtCron, "Schedule is invalid");
                validated = false;
            }
            if (validated && string.IsNullOrEmpty(txtCron.Text))
            {
                if (MessageBox.Show("No schedule has been specified.  Would you like to add without a schedule?",
                        "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    validated = false;
                }
            }

            return validated;
        }

        private void BttnAdd_Click(object sender, EventArgs e)
        {
            if (ValidateInput() == false) return;
            if (CustomCollections.ContainsKey(txtName.Text))
            {
                if (MessageBox.Show("Update existing collection?", "Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                    DialogResult.Yes) return;
                CustomCollections.Remove(txtName.Text);
            }

            var collection = new CustomCollection()
            {
                CommandTimeout = chkDefaultTimeout.Checked ? null : Convert.ToInt32(numTimeout.Value),
                ProcedureName = cboProcedureName.Text,
                RunOnServiceStart = chkRunOnStart.Checked,
                Schedule = txtCron.Text
            };
            CustomCollections.Add(txtName.Text, collection);
            LoadData();
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void ChkDefaultTimeout_CheckedChanged(object sender, EventArgs e)
        {
            numTimeout.Enabled = !chkDefaultTimeout.Checked;
        }

        private void DgvCustom_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var row = dgvCustom.Rows[e.RowIndex];
            var name = row.Cells["Name"].Value.ToString();
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!CustomCollections.ContainsKey(name)) return;
            CustomCollections[name].ProcedureName = row.Cells["ProcedureName"].Value.ToString();
            CustomCollections[name].Schedule = row.Cells["Schedule"].Value.ToString();
            CustomCollections[name].CommandTimeout = Convert.ToInt32(row.Cells["CommandTimeout"].Value);
            CustomCollections[name].RunOnServiceStart = Convert.ToBoolean(row.Cells["RunOnServiceStart"].Value);
        }

        private void CboProcedureName_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtName.Text = CleanName(cboProcedureName.Text);
        }

        private static string CleanName(string input)
        {
            // Remove everything up to and including the first period
            var afterPeriod = Regex.Replace(input, @"^.*?\.", "");

            // Replace all whitespace characters with underscores
            var withUnderscores = Regex.Replace(afterPeriod, @"\s", "_");

            // Remove all characters that are not A-Z, a-z, 0-9, or underscore
            var cleaned = Regex.Replace(withUnderscores, @"[^A-Za-z0-9_]", "");

            return cleaned;
        }

        private bool IsPreviewRunning;
        private Form PreviewForm;

        private void LnkPreview_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cboProcedureName.Text)) return;
            var procedureName = cboProcedureName.Text;
            var timeOut = chkDefaultTimeout.Checked ? 60 : Convert.ToInt32(numTimeout.Value);
            if (IsPreviewRunning)
            {
                MessageBox.Show("Preview is already running.  Please wait.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Task.Run(() =>
            {
                try
                {
                    IsPreviewRunning = true;
                    var dt = ExecuteProcedure(procedureName, timeOut);
                    Invoke(() => ShowPreview(dt));
                }
                catch (Exception ex)
                {
                    Invoke(() => MessageBox.Show("Error executing procedure:" + ex.Message, "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error));
                }
                finally
                {
                    IsPreviewRunning = false;
                }
            });
        }

        private void ShowPreview(DataTable dt)
        {
            if (PreviewForm != null)
            {
                PreviewForm.Close();
                PreviewForm = null;
            }
            PreviewForm = new Form
            {
                Text = "Preview",
                Width = 600,
                Height = 600
            };

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = dt,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
            };
            PreviewForm.Controls.Add(dgv);
            dgv.AutoResizeColumns();
            PreviewForm.ApplyTheme();
            PreviewForm.Show();
            PreviewForm.Closed += (sender, args) => PreviewForm = null;
        }

        private DataTable ExecuteProcedure(string procedureName, int timeOut)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                return null;
            var dt = new DataTable();
            using var cn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(procedureName, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = timeOut
            };
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        private void CboProcedureName_Validating(object sender, CancelEventArgs e)
        {
            errorProvider1.SetError(cboProcedureName, string.Empty);
        }

        private void TxtName_Validating(object sender, CancelEventArgs e)
        {
            errorProvider1.SetError(cboProcedureName, string.Empty);
            if (string.IsNullOrEmpty(txtName.Text)) return;
            if (CustomCollection.IsValidName(txtName.Text) == false)
                txtName.Text = CleanName(txtName.Text);
        }

        private string baseText = null;

        private void Timer1_Tick(object sender, EventArgs e)
        {
            baseText ??= lblGetScript.Text;
            try
            {
                lblGetScript.Invoke(() =>
                {
                    lblGetScript.Text = lblGetScript.Text.Length - baseText.Length > 10
                        ? baseText
                        : lblGetScript.Text += ".";
                });
            }
            catch
            {
                timer1.Enabled = false;
            }
        }

        private void SetCron(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var link = (LinkLabel)sender;
            txtCron.Text = link.Tag.ToString();
        }

        private void TxtCron_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtCron.Text))
                {
                    lblCron.Text = "No Schedule";
                }
                else if (int.TryParse(txtCron.Text, out var intCron))
                {
                    lblCron.Text = $"Every {intCron} seconds";
                }
                else
                {
                    lblCron.Text = CronExpressionDescriptor.ExpressionDescriptor.GetDescription(txtCron.Text);
                }
                IsScheduleValid = true;
            }
            catch
            {
                lblCron.Text = "Invalid Schedule";
                IsScheduleValid = true;
            }
        }
    }
}