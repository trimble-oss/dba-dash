using Amazon.Runtime.Internal.Transform;
using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace DBADashGUI.Changes
{
    public partial class InstanceMetadata : UserControl, ISetContext, IRefreshData
    {
        private readonly CodeEditor codeEditor1 = new();
        private readonly ElementHost elHost = new() { Dock = DockStyle.Fill };

        public InstanceMetadata()
        {
            InitializeComponent();
            customReportView1.Report = InstanceMetadataReport;
            customReportView1.PostGridRefresh += GridRefresh;
            elHost.Child = codeEditor1;
            codeEditor1.Mode = CodeEditor.CodeEditorModes.Json;
            tabJson.Controls.Add(elHost);
            contextCopy.Click += Copy_Click;
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            if (jsonTreeView.SelectedNode != null)
            {
                try
                {
                    Clipboard.SetText(jsonTreeView.SelectedNode.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error copying text: {ex.Message}", "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Clipboard.SetText(codeEditor1.Text);
            }
        }

        private void GridRefresh(object sender, EventArgs e)
        {
            if (customReportView1.Grids.Count <= 0 || customReportView1.Grids[0].Rows.Count <= 0) return;
            var grid = customReportView1.Grids[0];
            grid.SelectionChanged -= GridSelectionChanged;
            grid.SelectionChanged += GridSelectionChanged;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.Rows[0].Selected = true;
            grid.Select();
            grid.HideEmptyColumns();
            var instanceIdCol = grid.Columns["InstanceID"];
            if (instanceIdCol != null) instanceIdCol.Visible = false;
        }

        private void GridSelectionChanged(object sender, EventArgs e)
        {
            if (splitContainer1.Panel1Collapsed) return; // Don't do anything if the panel is collapsed
            var grid = (DBADashDataGridView)sender;
            if (grid.SelectedRows.Count == 1)
            {
                var row = grid.SelectedRows[0];
                var meta = row.Cells["Metadata"].Value?.ToString() ?? string.Empty;
                codeEditor1.Text = meta;
                diffControl1.NewText = meta;
                if (grid.Rows.Count > row.Index + 1)
                {
                    var compareRow = grid.Rows[row.Index + 1];
                    diffControl1.OldText = compareRow.Cells["Metadata"].Value?.ToString() ?? string.Empty;
                }
                else
                {
                    diffControl1.OldText = string.Empty;
                }

                jsonTreeView.Nodes.Clear(); // Clear any existing nodes

                try
                {
                    var rootToken = JToken.Parse(meta);
                    var rootNode = CreateTreeNode(rootToken, "Instance Metadata");
                    jsonTreeView.Nodes.Add(rootNode);
                    rootNode.ExpandAll();
                    jsonTreeView.SelectedNode = rootNode; // Select the root node
                }
                catch (JsonReaderException ex)
                {
                    CommonShared.ShowExceptionDialog(ex, "Invalid JSON format");
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, "Unexpected error loading TreeView");
                }
            }
            else
            {
                codeEditor1.Text = string.Empty;
                diffControl1.NewText = string.Empty;
                diffControl1.OldText = string.Empty;
            }
        }

        private static TreeNode CreateTreeNode(JToken token, string nodeName)
        {
            var node = new TreeNode(nodeName)
            {
                ForeColor = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark ? DashColors.White : DashColors.TrimbleBlue
            };
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var property in token.Children<JProperty>())
                    {
                        node.Nodes.Add(CreateTreeNode(property.Value, property.Name));
                    }
                    break;

                case JTokenType.Array:
                    // If it's an array, iterate its elements
                    node.Text += $@" [{((JArray)token).Count}]";
                    var index = 0;
                    foreach (var item in token.Children())
                    {
                        node.Nodes.Add(CreateTreeNode(item, $"[{index++}]")); // Use index as name for array elements
                    }
                    break;

                case JTokenType.Property:
                    // This case is handled by `JProperty` in the object iteration,
                    // the `property.Name` becomes the `nodeName` and `property.Value` is passed.
                    // We'll never directly call CreateTreeNode with a JTokenType.Property itself.
                    break;

                case JTokenType.Null:
                case JTokenType.None:
                    node.Text += " (null)";
                    break;

                case JTokenType.String:
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Boolean:
                case JTokenType.Date:
                case JTokenType.Constructor:
                case JTokenType.Comment:
                case JTokenType.Undefined:
                case JTokenType.Raw:
                case JTokenType.Bytes:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                default:
                    node.Nodes.Add(new TreeNode(token.ToString(Formatting.None)) { ForeColor = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark ? DashColors.GreenPale : DashColors.GreenDark });
                    break;
            }
            return node;
        }

        public void SetContext(DBADashContext _context)
        {
            if (_context.Clone() is not DBADashContext context) return;
            context.Report = context.InstanceID > 0 ? InstanceMetadataHistoryReport : InstanceMetadataReport;
            splitContainer1.Panel1Collapsed = context.InstanceID == 0;
            customReportView1.SetContext(context);
        }

        public void RefreshData()
        {
            customReportView1.RefreshData();
        }

        private SystemReport _instanceMetadataReport;

        private SystemReport InstanceMetadataReport
        {
            get
            {
                if (_instanceMetadataReport != null) return _instanceMetadataReport;
                _instanceMetadataReport = InstanceMetadataHistoryReport.DeepCopy() as SystemReport;
                _instanceMetadataReport.ProcedureName = "InstanceMetadata_Get";
                _instanceMetadataReport.QualifiedProcedureName = "dbo.InstanceMetadata_Get";
                _instanceMetadataReport.Params = new Params()
                {
                    ParamList = new List<Param>
                    {
                        new()
                        {
                            ParamName = "@InstanceIDs",
                            ParamType = "IDS"
                        },
                    }
                };
                _instanceMetadataReport.Pickers = new List<Picker>();
                return _instanceMetadataReport;
            }
        }

        private readonly SystemReport InstanceMetadataHistoryReport = new()
        {
            ReportName = "Cloud Provider Metadata",
            SchemaName = "dbo",
            ProcedureName = "InstanceMetadataHistory_Get",
            QualifiedProcedureName = "dbo.InstanceMetadataHistory_Get",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new()
                    {
                        ParamName = "@InstanceID",
                        ParamType = "INT"
                    },
                    new()
                    {
                        ParamName = "@Top",
                        ParamType = "INT"
                    },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@Top",
                    Name = "Top",
                    DefaultValue = 100,
                    PickerItems = new()
                    {
                        { 100, "100" },
                        { 200, "200" },
                        { 500, "500" },
                        { 1000, "1000" },
                        { 5000, "5000" },
                        { 10000, "10000" }
                    }
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "History",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "InstanceID", new ColumnMetadata { Alias = "InstanceID", Visible = false } },
                            { "InstanceDisplayName", new ColumnMetadata { Alias = "Instance", Visible = false } },
                            { "Provider", new ColumnMetadata { Alias = "Provider" } },
                            { "Metadata", new ColumnMetadata {
                                Alias = "Metadata",
                                Link = new TextLinkColumnInfo
                                {
                                    TargetColumn = "Metadata",
                                    TextHandling = CodeEditor.CodeEditorModes.Json
                                }
                            } },
                            { "Name", new ColumnMetadata { Alias = "Name" } },
                            { "VMSize", new ColumnMetadata { Alias = "VM Size" } },
                            { "SKU", new ColumnMetadata { Alias = "SKU" } },
                            { "ResourceGroup", new ColumnMetadata { Alias = "Resource Group" } },
                            { "PrivateIPs", new ColumnMetadata { Alias = "Private IPs" } },
                            { "PublicIPs", new ColumnMetadata { Alias = "Public IPs" } },
                            { "IPAddresses", new ColumnMetadata { Alias = "IPs" } },
                            { "Location", new ColumnMetadata { Alias = "Location" } },
                            { "AvailabilityZone", new ColumnMetadata { Alias = "Availability Zone" } },
                            { "SubscriptionID", new ColumnMetadata { Alias = "Subscription ID" } },
                            { "Tags", new ColumnMetadata { Alias = "Tags" } },
                            { "AccountID", new ColumnMetadata { Alias = "Account Id" } },
                            { "ImageId", new ColumnMetadata { Alias = "Image Id" } },
                            { "InstanceId", new ColumnMetadata { Alias = "InstanceId" } },
                            { "VMHostName", new ColumnMetadata { Alias = "VM Host Name" } },
                            { "IAMRole", new ColumnMetadata { Alias = "IAM Role" } },
                            { "SecurityGroups", new ColumnMetadata { Alias = "Security Groups" } },
                            { "LocalHostname", new ColumnMetadata { Alias = "Local Hostname" } },
                            { "BillingProducts", new ColumnMetadata { Alias = "Billing Products" } },
                            { "ClusterIP", new ColumnMetadata { Alias = "Cluster IP" } },
                            { "ClusterName", new ColumnMetadata { Alias = "Cluster Name" } },
                            { "SnapshotDate", new ColumnMetadata { Alias = "First Snapshot Date" } },
                            { "LastSnapshotDate", new ColumnMetadata { Alias = "Last Snapshot Date" } },
                            { "ValidFrom", new ColumnMetadata { Alias = "Valid From" } },
                            { "ValidTo", new ColumnMetadata { Alias = "Valid To" } }
                        }
                    }
                }
            }
        };

        private void NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                jsonTreeView.SelectedNode = e.Node;
            }
        }
    }
}