using DBADash;
using DBADashGUI.AgentJobs;
using DBADashGUI.CustomReports;
using DBADashGUI.DBADashAlerts;
using DBADashGUI.Properties;
using Microsoft.SqlServer.Management.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI
{
    public class SQLTreeItem : TreeNode
    {
        private readonly Dictionary<string, object> _attributes;

        public Dictionary<string, object> Attributes => _attributes;

        public enum TreeType
        {
            DummyNode,
            Folder,
            DBADashRoot,
            Instance,
            Database,
            AggregateFunction,
            CLRAssembly,
            CLRProcedure,
            CLRScalarFunction,
            CLRTableFunction,
            DatabaseTrigger,
            InlineFunction,
            Role,
            ScalarFunction,
            Schema,
            SequenceObject,
            ServiceBrokerBinding,
            ServiceBrokerContract,
            ServiceBrokerMessageType,
            ServiceBrokerPriorities,
            ServiceBrokerQueue,
            ServiceBrokerRoute,
            ServiceBrokerService,
            StoredProcedure,
            Synonym,
            Table,
            TableFunction,
            UserDefinedDataType,
            UserDefinedTableType,
            UserDefinedType,
            View,
            XMLSchemaCollection,
            Trigger,
            CLRTrigger,
            Configuration,
            AzureInstance,
            AzureDatabase,
            AgentJobs,
            AgentJob,
            HADR,
            DBAChecks,
            Tags,
            AgentJobStep,
            InstanceFolder,
            DatabasesFolder,
            Storage,
            Drive,
            ReportsFolder,
            CustomReport,
            ElasticPool,
            SystemReport,
            RecycleBin,
            CommunityToolsFolder,
            CommunityTool,
            CustomToolsFolder,
            CustomTool
        }

        private DatabaseEngineEdition _engineEdition = DatabaseEngineEdition.Unknown;

        public DatabaseEngineEdition EngineEdition
        {
            get
            {
                if (_engineEdition == DatabaseEngineEdition.Unknown && Type != TreeType.DBADashRoot && Parent != null)
                {
                    _engineEdition = SQLTreeItemParent.EngineEdition;
                }
                return _engineEdition;
            }
            set
            {
                _engineEdition = value;
                SetIcon();
            }
        }

        public string FriendlyFullPath => Type == TreeType.Drive ? FullPath[..^Text.Length].Replace("\\", " \\ ") + DriveName : FullPath.Replace("\\", " \\ ");

        private DBADashContext InternalContext;
        public SQLTreeItem SQLTreeItemParent => Parent.AsSQLTreeItem();
        private bool IsChildOfInstanceOrAzureDB => (InstanceID > 0 && !IsInstanceOrAzureDB) || Type == TreeType.ElasticPool;
        private bool IsChildOfInstanceOrAzureInstance => InstanceID > 0 && !IsInstanceOrAzureInstance;
        private bool IsInstanceOrAzureDB => Type == TreeType.Instance || Type == TreeType.AzureDatabase;
        private bool IsInstanceOrAzureInstance => Type == TreeType.Instance || Type == TreeType.AzureInstance;

        public string DriveName;

        public string ElasticPoolName { get; set; }

        public CustomReport Report;

        public DBADashContext Context
        {
            get
            {
                if (InternalContext != null) return InternalContext;
                InternalContext = new DBADashContext()
                {
                    ObjectID = ObjectID,
                    ObjectName = ObjectName,
                    SchemaName = SchemaName,
                    InstanceID = InstanceID,
                    DatabaseID = DatabaseID,
                    JobID = JobID,
                    JobStepID = JobStepID,
                    Type = Type,
                    DatabaseName = DatabaseName,
                    ParentType = Parent == null ? TreeType.DBADashRoot : SQLTreeItemParent.Type,
                    DriveName = DriveName,
                    Report = Report,
                    ElasticPoolName = ElasticPoolName,
                    TreeLevel = Level
                };
                switch (Type)
                {
                    case TreeType.AzureInstance:
                        InternalContext.AzureInstanceIDsWithHidden = CommonData.Instances.Rows.Cast<DataRow>().Where(r => (string)r["Instance"] == ObjectName && (int)r["EngineEdition"] == 5)
                         .Select(r => (int)r["InstanceID"]).ToHashSet();

                        break;

                    case TreeType.DBADashRoot or TreeType.InstanceFolder:
                        {
                            foreach (SQLTreeItem itm in Nodes) // Look down the tree for Instance/AzureDB nodes
                            {
                                if (itm.InstanceID > 0) // We have an instance to add
                                {
                                    if (itm.Type == TreeType.AzureDatabase)
                                    {
                                        InternalContext.AzureInstanceIDsWithHidden.Add(itm.instanceID);
                                    }
                                    else
                                    {
                                        InternalContext.RegularInstanceIDsWithHidden.Add(itm.instanceID);
                                    }
                                }
                                else if (itm.Type is TreeType.AzureInstance or TreeType.InstanceFolder)
                                {
                                    // We need to get the InstanceIDs from next level down

                                    InternalContext.AzureInstanceIDsWithHidden.UnionWith(itm.Context.AzureInstanceIDsWithHidden);
                                    InternalContext.RegularInstanceIDsWithHidden.UnionWith(itm.Context.RegularInstanceIDsWithHidden);
                                }
                            }

                            break;
                        }
                    // Return a list with a single ID of the AzureDB
                    case TreeType.AzureDatabase:
                        InternalContext.AzureInstanceIDsWithHidden.Add(instanceID);
                        break;
                    // Return a list with a single ID of the Instance
                    case TreeType.Instance:
                        InternalContext.RegularInstanceIDsWithHidden.Add(instanceID);
                        break;

                    case TreeType.ElasticPool:
                        InternalContext.AzureInstanceIDsWithHidden = Parent?.Nodes.Cast<SQLTreeItem>().Where(n => n.Type == TreeType.AzureDatabase && string.Equals(n.ElasticPoolName, ElasticPoolName, StringComparison.InvariantCultureIgnoreCase)).Select(n => n.InstanceID).ToHashSet();
                        break;

                    default:
                        InternalContext.AzureInstanceIDsWithHidden = Parent.AsSQLTreeItem().AzureInstanceIDsWithHidden;
                        InternalContext.RegularInstanceIDsWithHidden = Parent.AsSQLTreeItem().RegularInstanceIDsWithHidden;
                        break;
                }

                InternalContext.InstanceName = Type switch
                {
                    TreeType.Instance or TreeType.AzureInstance => ObjectName,
                    TreeType.DBADashRoot => string.Empty,
                    _ => Parent?.AsSQLTreeItem().Context.InstanceName ?? string.Empty
                };
                if (Type is TreeType.ElasticPool or TreeType.AzureInstance)
                {
                    InternalContext.MasterInstanceID = CommonData.Instances.Rows.Cast<DataRow>()
                        .Where(r => string.Equals((string)r["Instance"], InstanceName, StringComparison.InvariantCultureIgnoreCase) && string.Equals((string)r["AzureDBName"].DBNullToNull(), "master", StringComparison.InvariantCultureIgnoreCase))
                        .Select(r => (int)r["InstanceID"])
                        .FirstOrDefault(0);
                }
                else
                {
                    InternalContext.MasterInstanceID = 0;
                }
                return InternalContext;
            }
        }

        public Guid JobID => Type switch
        {
            TreeType.AgentJob => (Guid)Tag,
            TreeType.AgentJobStep => (Guid)Parent.Tag,
            _ => Guid.Empty
        };

        public int JobStepID => Type == TreeType.AgentJobStep ? (int)Tag : -1;

        /// <summary>
        /// Return a list of instance IDs associated with the current node in the tree.  Includes AzureDB.
        /// </summary>
        public HashSet<int> InstanceIDs => Context.InstanceIDs;

        /// <summary>
        /// Return a list of instance IDs associated with the current node in the tree.  Includes AzureDB.
        /// </summary>
        public HashSet<int> InstanceIDsWithHidden => Context.AzureInstanceIDsWithHidden;

        /// <summary>
        /// Return a list of instance IDs associated with the current node in the tree.  Excludes AzureDB.
        /// </summary>
        public HashSet<int> RegularInstanceIDs => Context.RegularInstanceIDs;

        public HashSet<int> RegularInstanceIDsWithHidden => Context.RegularInstanceIDsWithHidden;

        /// <summary>
        /// Return a list of AzureDB instance IDs associated with the current node in the tree.
        /// </summary>
        public HashSet<int> AzureInstanceIDs => Context.AzureInstanceIDs;

        public HashSet<int> AzureInstanceIDsWithHidden => Context.AzureInstanceIDsWithHidden;

        public int MasterInstanceID => Context.MasterInstanceID;

        public string FullName() => string.IsNullOrEmpty(_schemaName) ? _objectName : _schemaName + "." + _objectName;

        public SQLTreeItem(string objectName, TreeType type, Dictionary<string, object> attributes = null, string schemaName = null) : base()
        {
            _schemaName = schemaName;
            _objectName = objectName;
            Text = objectName;
            Type = type;
            _attributes = attributes;
            SetIcon();
            switch (type)
            {
                case TreeType.DBADashRoot or TreeType.Instance or TreeType.InstanceFolder or TreeType.AzureInstance:
                    AddFindDatabase();
                    break;

                case TreeType.AgentJob:
                    AddAgentJobContextMenuItems();
                    break;
            }
        }

        public SQLTreeItem(string objectName, string schemaName, string type)
        {
            Type = type switch
            {
                "P" => TreeType.StoredProcedure,
                "V" => TreeType.View,
                "IF" => TreeType.InlineFunction,
                "U" => TreeType.Table,
                "TF" => TreeType.TableFunction,
                "FN" => TreeType.ScalarFunction,
                "AF" => TreeType.AggregateFunction,
                "DTR" => TreeType.DatabaseTrigger,
                "CLR" => TreeType.CLRAssembly,
                "FT" => TreeType.CLRTableFunction,
                "FS" => TreeType.CLRScalarFunction,
                "TYP" => TreeType.UserDefinedDataType,
                "TT" => TreeType.UserDefinedTableType,
                "UTY" => TreeType.UserDefinedType,
                "XSC" => TreeType.XMLSchemaCollection,
                "SO" => TreeType.SequenceObject,
                "PC" => TreeType.CLRProcedure,
                "TR" => TreeType.Trigger,
                "TA" => TreeType.CLRTrigger,
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };
            _objectName = objectName;
            _schemaName = schemaName;
            Text = FullName();
            SetIcon();
        }

        private bool isVisibleInSummary;

        public bool IsVisibleInSummary
        { get => isVisibleInSummary; set { isVisibleInSummary = value; SetIcon(); } }

        private string instanceName;
        private int instanceID;

        public int InstanceID
        {
            get => instanceID > 0 ? instanceID : Parent == null ? 0 : SQLTreeItemParent.InstanceID;
            set => instanceID = value;
        }

        public TreeType Type;

        public string InstanceName => Context.InstanceName;

        public string _objectName;
        public string _schemaName;
        private int _databaseID = -1;
        private string databaseName;

        public string ObjectName
        { get => _objectName; set { _objectName = value; Name = FullName(); } }

        public string SchemaName
        { get => _schemaName; set { _schemaName = value; Name = FullName(); } }

        public long ObjectID { get; set; }

        public int DatabaseID
        {
            get
            {
                if (Type is TreeType.DBADashRoot or TreeType.Instance)
                {
                    return -1;
                }
                else if (_databaseID != -1)
                {
                    return _databaseID;
                }
                else
                {
                    _databaseID = SQLTreeItemParent.DatabaseID;
                    return _databaseID;
                }
            }
            set => _databaseID = value;
        }

        public string DatabaseName
        {
            get
            {
                switch (Type)
                {
                    case TreeType.DBADashRoot or TreeType.Instance or TreeType.AzureInstance:
                        return string.Empty;

                    case TreeType.Database or TreeType.AzureDatabase:
                        return databaseName;

                    default:
                        {
                            if (!string.IsNullOrEmpty(databaseName))
                            {
                                return databaseName;
                            }
                            else
                            {
                                databaseName = SQLTreeItemParent.DatabaseName;
                                return databaseName;
                            }
                        }
                }
            }
            set => databaseName = value;
        }

        private void SetIcon()
        {
            switch (Type)
            {
                case TreeType.DBADashRoot:
                    ImageIndex = 0;
                    break;

                case TreeType.Instance:
                    if (EngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                    {
                        ImageIndex = 18;
                    }
                    else if (IsVisibleInSummary)
                    {
                        ImageIndex = 1;
                    }
                    else
                    {
                        ImageIndex = 19;
                    }
                    break;

                case TreeType.Database:
                    ImageIndex = 2;
                    break;

                case TreeType.Table:
                    ImageIndex = 4;
                    break;

                case TreeType.StoredProcedure:
                case TreeType.InlineFunction:
                case TreeType.ScalarFunction:
                case TreeType.TableFunction:
                case TreeType.AggregateFunction:
                case TreeType.DatabaseTrigger:
                case TreeType.CLRAssembly:
                    ImageIndex = 5;
                    break;

                case TreeType.RecycleBin:
                case TreeType.Folder:
                case TreeType.DatabasesFolder:
                    ImageIndex = 3;
                    break;

                case TreeType.HADR:
                    ImageIndex = 23;
                    break;

                case TreeType.Configuration:
                    ImageIndex = 7;
                    break;

                case TreeType.AzureInstance:
                    ImageIndex = 8;
                    break;

                case TreeType.AzureDatabase:
                    ImageIndex = 9;
                    break;

                case TreeType.AgentJobs:
                    ImageIndex = 22;
                    break;

                case TreeType.DBAChecks:
                    ImageIndex = 10;
                    break;

                case TreeType.Tags:
                    ImageIndex = 11;
                    break;

                case TreeType.AgentJobStep:
                    var subsystem = (string)_attributes["subsystem"];
                    ImageIndex = subsystem switch
                    {
                        "CmdExec" => 12,
                        "PowerShell" => 15,
                        "Ssis" => 14,
                        "AnalysisQuery" => 13,
                        _ => 16
                    };
                    break;

                case TreeType.AgentJob:
                    ImageIndex = Attributes.Count > 0 && (bool)Attributes["enabled"] ? 5 : 17;
                    break;

                case TreeType.InstanceFolder:
                    ImageIndex = 20;
                    break;

                case TreeType.Storage:
                case TreeType.Drive:
                    ImageIndex = 21;
                    break;

                case TreeType.CustomReport:
                    ImageIndex = 24;
                    break;

                case TreeType.SystemReport:
                    ImageIndex = 27;
                    break;

                case TreeType.ReportsFolder:
                    ImageIndex = 25;
                    break;

                case TreeType.ElasticPool:
                    ImageIndex = 26;
                    break;

                case TreeType.CommunityToolsFolder:
                    ImageIndex = 29;
                    break;

                case TreeType.CommunityTool:
                    ImageIndex = 30;
                    break;

                case TreeType.CustomToolsFolder:
                    ImageIndex = 20;
                    break;

                case TreeType.CustomTool:
                    ImageIndex = 30;
                    break;

                default:
                    ImageIndex = 5;
                    break;
            }
            SelectedImageIndex = ImageIndex;
        }

        private static SQLTreeItem NewFolder(string name, string tag, bool addDummyNode)
        {
            var n = new SQLTreeItem(name, TreeType.Folder)
            {
                Tag = tag
            };
            if (addDummyNode)
            {
                n.AddDummyNode();
            }
            return n;
        }

        public void AddDummyNode()
        {
            SQLTreeItem dummyNode = new("", TreeType.DummyNode);
            Nodes.Add(dummyNode);
            AddRefreshContextMenu();
        }

        public static SQLTreeItem GetReportsFolder(IEnumerable<CustomReport> reports)
        {
            var reportsNode = new SQLTreeItem("Reports", TreeType.ReportsFolder);
            foreach (var report in reports)
            {
                if (!report.HasAccess()) continue;
                var reportNode = new SQLTreeItem(report.ReportName, report is SystemReport ? TreeType.SystemReport : TreeType.CustomReport) { Report = report };
                reportsNode.Nodes.Add(reportNode);
            }

            return reportsNode;
        }

        public void AddReportsFolder(IEnumerable<CustomReport> reports)
        {
            var reportsNode = GetReportsFolder(reports);
            if (reportsNode.Nodes.Count > 0)
            {
                Nodes.Add(reportsNode);
            }
        }

        public void AddDatabaseFolders()
        {
            var nTables = NewFolder("Tables", "U", true);
            var nViews = NewFolder("Views", "V", true);
            var nProgrammability = NewFolder("Programmability", null, false);
            var nStoredProcs = NewFolder("Stored Procedures", "P,PC", true);
            var nTableFunctions = NewFolder("Table Functions", "IF,TF,FT", true);
            var nScalarFunctions = NewFolder("Scalar Functions", "FN,FS", true);
            var nAggFunctions = NewFolder("Aggregate Functions", "AF", true);
            var nDBTriggers = NewFolder("Database Triggers", "DTR", true);
            var nAssemblies = NewFolder("Assemblies", "CLR", true);
            var nTypes = NewFolder("Types", "", false);
            var nTableTypes = NewFolder("User-Defined Table Types", "TT", true);
            var nDataTypes = NewFolder("User-Defined Data Types", "TYP", true);
            var nUserDefinedTypes = NewFolder("User-Defined Types", "UTY", true);
            var nXML = NewFolder("XML Schema Collections", "XSC", true);
            var nSeq = NewFolder("Sequences", "SO", true);
            var nTriggers = NewFolder("Triggers", "TA,TR", true);
            AddReportsFolder(CustomReports.CustomReports.GetCustomReports().DatabaseLevelReports);
            AddCommunityTools();
            AddCustomToolsFolder();
            nTypes.Nodes.Add(nTableTypes);
            nTypes.Nodes.Add(nDataTypes);
            nTypes.Nodes.Add(nUserDefinedTypes);
            nTypes.Nodes.Add(nXML);
            nProgrammability.Nodes.Add(nStoredProcs);
            nProgrammability.Nodes.Add(nAggFunctions);
            nProgrammability.Nodes.Add(nTableFunctions);
            nProgrammability.Nodes.Add(nScalarFunctions);
            nProgrammability.Nodes.Add(nDBTriggers);
            nProgrammability.Nodes.Add(nTriggers);
            nProgrammability.Nodes.Add(nAssemblies);
            nStoredProcs.AddFilterContextMenu();
            nAggFunctions.AddFilterContextMenu();
            nTableFunctions.AddFilterContextMenu();
            nScalarFunctions.AddFilterContextMenu();
            nDBTriggers.AddFilterContextMenu();
            nTriggers.AddFilterContextMenu();
            nAssemblies.AddFilterContextMenu();
            nTableTypes.AddFilterContextMenu();
            nDataTypes.AddFilterContextMenu();
            nUserDefinedTypes.AddFilterContextMenu();
            nXML.AddFilterContextMenu();
            nViews.AddFilterContextMenu();
            nTables.AddFilterContextMenu();
            nTypes.AddFilterContextMenu();
            nSeq.AddFilterContextMenu();

            Nodes.Add(nTables);
            Nodes.Add(nViews);
            Nodes.Add(nProgrammability);

            Nodes.Add(nTypes);
            Nodes.Add(nSeq);
        }

        private void AddRefreshContextMenu()
        {
            ContextMenuStrip ??= new ContextMenuStrip();
            if (ContextMenuStrip.Items.OfType<ToolStripMenuItem>().Any(mnu => mnu.Text == @"Refresh")) return;
            var mnuRefresh = new ToolStripMenuItem("Refresh") { Image = Resources._112_RefreshArrow_Green_16x16_72 };
            ContextMenuStrip.Items.Add(mnuRefresh);
            mnuRefresh.Click += MnuRefresh_Click;
        }

        private void AddFindDatabase()
        {
            ContextMenuStrip ??= new ContextMenuStrip();
            if (ContextMenuStrip.Items.Cast<ToolStripMenuItem>().Any(mnu => mnu.Text == @"Find Database")) return;
            var mnuFindDB = new ToolStripMenuItem("Find Database") { Image = Resources.Database_16x };
            ContextMenuStrip.Items.Add(mnuFindDB);
            mnuFindDB.Click += FindDatabase;
        }

        private void AddAgentJobContextMenuItems()
        {
            ContextMenuStrip ??= new ContextMenuStrip();
            if (ContextMenuStrip.Items.Cast<ToolStripMenuItem>().Any(mnu => mnu.Text == @"Start/Stop Job")) return;
            var mnuStartStop = new ToolStripMenuItem("Start/Stop Job") { Image = Resources.ProjectSystemModelRefresh_16x };
            var mnuInfo = new ToolStripMenuItem("Job Info", Resources.Information_blue_6227_16x16);
            var mnuConfigureThresholds =
                new ToolStripMenuItem("Configure Thresholds", Resources.SettingsOutline_16x);
            ContextMenuStrip.Items.AddRange(new ToolStripItem[] { mnuInfo, mnuConfigureThresholds, mnuStartStop, new ToolStripSeparator() });
            mnuStartStop.Click += StartStopJob_Click;
            mnuInfo.Click += JobInfo_Click;
            mnuConfigureThresholds.Click += ConfigureJobThresholds_Click;
        }

        private void ConfigureJobThresholds_Click(object sender, EventArgs e)
        {
            var frm = new AgentJobThresholdsConfig()
            {
                InstanceID = Context.InstanceID,
                JobID = Context.JobID,
                connectionString = Common.ConnectionString
            };
            frm.Show();
        }

        private void JobInfo_Click(object sender, EventArgs e)
        {
            var frm = new JobInfoForm()
            {
                DBADashContext = Context
            };
            frm.Show();
        }

        private void StartStopJob_Click(object sender, EventArgs e)
        {
            var frm = new JobExecutionDialog()
            {
                InstanceId = Context.InstanceID,
                JobId = Context.JobID,
                JobName = Context.ObjectName
            };
            frm.Show();
        }

        private void FindDatabase(object sender, EventArgs e)
        {
            var dbName = string.Empty;
            if (CommonShared.ShowInputDialog(ref dbName, "Find Database", default, "Enter database name") != DialogResult.OK || string.IsNullOrEmpty(dbName)) return;
            ShowFindDatabaseDialog(dbName);
        }

        private static CustomReportViewer databaseFinderDialog;
        private int? databaseFinderWidth;
        private int? databaseFinderHeight;

        private void ShowFindDatabaseDialog(string dbName)
        {
            databaseFinderDialog?.Close();
            var report = DatabaseFinderReport.Instance;
            var tempContext = (DBADashContext)Context.DeepCopy();
            tempContext.Report = report;
            databaseFinderWidth ??= Convert.ToInt32(Math.Max(Main.MainFormInstance.Width * 0.7, 800));
            databaseFinderHeight ??= Convert.ToInt32(Math.Max(Main.MainFormInstance.Height * 0.7, 600));
            var sqlParams = report.GetCustomSqlParameters();
            var pSearchString = sqlParams.First(p => p.Param.ParameterName == "@SearchString");
            pSearchString.UseDefaultValue = false;
            pSearchString.Param.Value = "%" + dbName + "%";
            databaseFinderDialog = new CustomReportViewer
            {
                Context = tempContext,
                Width = databaseFinderWidth.Value,
                Height = databaseFinderHeight.Value,
                CustomParams = sqlParams
            };
            databaseFinderDialog.SizeChanged += (s, e) =>
            {
                if (databaseFinderDialog.WindowState is FormWindowState.Maximized or FormWindowState.Minimized) return;
                databaseFinderWidth = databaseFinderDialog.Width;
                databaseFinderHeight = databaseFinderDialog.Height;
            };
            databaseFinderDialog.FormClosed += (s, e) => databaseFinderDialog = null;
            databaseFinderDialog.Show();
        }

        private void MnuRefresh_Click(object sender, EventArgs e)
        {
            RefreshNode();
        }

        private void RefreshNode()
        {
            var isExpanded = IsExpanded;
            Nodes.Clear();
            AddDummyNode();
            Collapse();
            if (isExpanded)
            {
                Expand();
            }
        }

        private void AddFilterContextMenu()
        {
            ContextMenuStrip ??= new ContextMenuStrip();
            var mnuFilter = new ToolStripMenuItem("Filter") { Image = Resources.Filter_16x };
            mnuFilter.Click += MnuFilter_Click;
            ContextMenuStrip.Items.Add(mnuFilter);
        }

        public void AddInstanceActionsContextMenu()
        {
            ContextMenuStrip ??= new ContextMenuStrip();
            if (!DBADashUser.IsAdmin) return;

            var mnuInstanceActions = new ToolStripMenuItem("Instance Actions") { Image = Resources.DatabaseSettings_16x };

            var mnuAddAlertBlackout = new ToolStripMenuItem("Add Alert Blackout")
            { Image = Resources.NotificationAlertMute_16x };
            mnuAddAlertBlackout.Click += MnuAddAlertBlackout_Click;

            var mnuHidden = new ToolStripMenuItem("Hidden");
            mnuHidden.Checked = !IsVisibleInSummary;
            mnuHidden.CheckOnClick = true;
            mnuHidden.Click += HideInstance_Click;

            var mnuRename = new ToolStripMenuItem("Rename") { Image = Resources.Rename_16x };
            mnuRename.Click += MnuRename_Click;

            var mnuDelete = new ToolStripMenuItem("Delete") { Image = Resources.DeleteDatabase_16x };
            mnuDelete.Click += MnuDelete_Click;

            switch (Type)
            {
                case TreeType.Instance:
                    mnuInstanceActions.DropDownItems.AddRange(new ToolStripItem[] { mnuAddAlertBlackout, mnuHidden, mnuRename, mnuDelete });
                    break;

                case TreeType.AzureDatabase:
                    mnuInstanceActions.DropDownItems.AddRange(new ToolStripItem[] { mnuDelete });
                    break;
            }
            ContextMenuStrip.Items.Add(mnuInstanceActions);
        }

        private async void MnuAddAlertBlackout_Click(object sender, EventArgs e)
        {
            var blackout = new BlackoutPeriod()
            {
                ApplyToInstance = InstanceName,
                ApplyToInstanceID = InstanceID,
                StartDate = DateHelper.AppNow,
                EndDate = DateHelper.AppNow.AddHours(1),
                TimeZone = DateHelper.AppTimeZone
            };
            await AlertConfig.EditBlackout(blackout);
        }

        private void HideInstance_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;

            if (mnu.Checked && MessageBox.Show($@"Hide instance {InstanceName}?", @"Hide Instance", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
                mnu.Checked = false;
                return;
            }
            try
            {
                SharedData.UpdateShowInSummary(InstanceID, !mnu.Checked, Common.ConnectionString);
                switch (mnu.Checked)
                {
                    case true:
                        DBADashContext.HiddenInstanceIDs.Add(InstanceID);
                        break;

                    default:
                        DBADashContext.HiddenInstanceIDs.Remove(InstanceID);
                        break;
                }

                IsVisibleInSummary = !mnu.Checked;
                SetIcon();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void MnuRename_Click(object sender, EventArgs e)
        {
            var name = InstanceName;
            var msg = $"Rename ConnectionID: {Context.ConnectionID}";
            if (CommonShared.ShowInputDialog(ref name, msg) != DialogResult.OK) return;
            try
            {
                SharedData.UpdateAlias(instanceID, ref name, Common.ConnectionString);
                Context.InstanceName = name;
                instanceName = name;
                Text = name;
                RefreshNode();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void MnuDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show($@"Are you sure you want to mark {Context.InstanceName} ({Context.ConnectionID}) deleted?", @"Mark Deleted?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) !=
                DialogResult.Yes) return;
            try
            {
                SharedData.MarkInstanceDeleted(InstanceID, Common.ConnectionString);
                Parent.Nodes.Remove(this);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        public string FilterText = "";

        private void MnuFilter_Click(object sender, EventArgs e)
        {
            var filter = FilterText;
            if (CommonShared.ShowInputDialog(ref filter, "Filter") == DialogResult.OK)
            {
                Filter(filter);
            }
        }

        private readonly List<SQLTreeItem> unfilteredNodes = new();

        public void Filter(string filter)
        {
            Expand();
            if (unfilteredNodes.Count == 0)
            {
                foreach (SQLTreeItem itm in Nodes)
                {
                    unfilteredNodes.Add(itm)
;
                }
            }
            Nodes.Clear();
            foreach (var n in unfilteredNodes.Where(n => string.IsNullOrEmpty(filter) || n.Text.Contains(filter, StringComparison.CurrentCultureIgnoreCase)))
            {
                Nodes.Add(n);
            }
            if (unfilteredNodes.Count == Nodes.Count)
            {
                ImageIndex = 3;
                SelectedImageIndex = 3;
            }
            else
            {
                ImageIndex = 6;
                SelectedImageIndex = 6;
            }
            FilterText = filter;
        }

        private bool IsThisInstance(int InstanceID)
        {
            return IsInstanceOrAzureDB && this.InstanceID == InstanceID;
        }

        private bool IsThisInstance(string Instance)
        {
            return IsInstanceOrAzureDB && instanceName == Instance;
        }

        /// <summary>
        /// Find instance in the tree by ID
        /// </summary>
        public SQLTreeItem FindInstance(int _instanceID)
        {
            var n = this;
            while (n.IsChildOfInstanceOrAzureDB) // If we are inside an instance node, navigate up until we get the instance ID.
            {
                n = n.SQLTreeItemParent;
            }
            if ((new[] { TreeType.DBAChecks, TreeType.HADR, TreeType.Configuration }).Contains(n.Type)) // Navigate up a level for these types
            {
                n = n.SQLTreeItemParent;
            }
            if (n.IsThisInstance(_instanceID)) // Check if we have the instance
            {
                return n;
            }
            // Look down the tree to find the instance
            return FindChildInstance(_instanceID, n);
        }

        /// <summary>
        /// Find instance in the tree by name
        /// </summary>
        public SQLTreeItem FindInstance(string instance)
        {
            var n = this;
            while (n.IsChildOfInstanceOrAzureInstance) // If we are inside an instance node, navigate up until we get the instance ID.
            {
                n = n.SQLTreeItemParent;
            }
            if ((new[] { TreeType.DBAChecks, TreeType.HADR, TreeType.Configuration }).Contains(n.Type)) // Navigate up a level for these types
            {
                n = n.SQLTreeItemParent;
            }
            if (n.IsThisInstance(instance)) // Check if we have the instance
            {
                return n;
            }
            // Look down the tree to find the instance
            return FindChildInstance(instance, n);
        }

        /// <summary>
        /// Look down the tree to find instance in the tree by ID
        /// </summary>
        private static SQLTreeItem FindChildInstance(int instanceID, SQLTreeItem node)
        {
            foreach (SQLTreeItem child in node.Nodes)
            {
                if (child.IsInstanceOrAzureDB && child.InstanceID == instanceID)
                {
                    return child;
                }
                if (child.Type is TreeType.InstanceFolder or TreeType.AzureInstance)
                {
                    var find = FindChildInstance(instanceID, child);
                    if (find != null)
                    {
                        return find;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Look down the tree to find instance in the tree by name
        /// </summary>
        private static SQLTreeItem FindChildInstance(string instance, SQLTreeItem node)
        {
            if (node.IsInstanceOrAzureInstance && node.InstanceName == instance)
            {
                return node;
            }
            foreach (SQLTreeItem child in node.Nodes)
            {
                if (child.IsInstanceOrAzureInstance && child.InstanceName == instance)
                {
                    return child;
                }
                if (child.Type is TreeType.InstanceFolder or TreeType.AzureInstance)
                {
                    var find = FindChildInstance(instance, child);
                    if (find != null)
                    {
                        return find;
                    }
                }
            }
            return null;
        }

        public SQLTreeItem FindChildOfType(TreeType type)
        {
            return Nodes.Cast<SQLTreeItem>().FirstOrDefault(n => n.Type == type);
        }

        public void AddCommunityTools()
        {
            if (!Context.CanMessage) return;
            if (!DBADashUser.CommunityScripts) return;
            var communityTools = new SQLTreeItem("Community Tools", SQLTreeItem.TreeType.CommunityToolsFolder);
            var agent = Context.CollectAgent;

            var toolsList = DatabaseID > 0
                ? CommunityTools.CommunityTools.DatabaseLevelCommunityTools
                : CommunityTools.CommunityTools.CommunityToolsList;

            foreach (var n in toolsList.OrderBy(tool => tool.ReportName).Select(tool => new SQLTreeItem(tool.ProcedureName, SQLTreeItem.TreeType.CommunityTool)
            {
                Report = tool
            }))
            {
                if (agent.IsAllowAllScripts || agent.AllowedScripts.Contains(n.Report.ProcedureName))
                {
                    communityTools.Nodes.Add(n);
                }
            }
            if (communityTools.Nodes.Count > 0)
                Nodes.Add(communityTools);
        }

        public void AddCustomToolsFolder()
        {
            if (!Context.CanMessage) return;
            if (!DBADashUser.IsInRole("CustomTools") && !DBADashUser.IsAdmin) return;
            var customTools = new SQLTreeItem("Custom Tools", TreeType.CustomToolsFolder);
            customTools.AddDummyNode();
            Nodes.Add(customTools);
        }

        public async Task AddCustomToolsAsync()
        {
            Nodes.Clear();
            var tools = await CommonData.GetCustomToolsAsync(Context.InstanceID);
            if (tools == null) return;
            foreach (var tool in tools)
            {
                if (!tool.IsDatabaseLevel && DatabaseID > 0 && Context.AzureInstanceIDs.Count == 0) continue;
                if (!DBADashUser.IsInRole(tool.ReportVisibilityRole) && !DBADashUser.IsAdmin) continue;
                var n = new SQLTreeItem(tool.ReportName, TreeType.CustomTool) { Report = tool };
                Nodes.Add(n);
            }
        }
    }
}