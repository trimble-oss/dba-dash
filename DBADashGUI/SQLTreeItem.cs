using DBADashGUI.CustomReports;
using Microsoft.SqlServer.Management.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DBADash;
using DBADashGUI.Properties;

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
            CommunityTool
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

        private HashSet<int> _RegularInstanceIDs;
        private HashSet<int> _AzureInstanceIDs;
        private HashSet<int> _InstanceIDs;
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
                InternalContext ??= new DBADashContext()
                {
                    InstanceIDs = InstanceIDs,
                    AzureInstanceIDs = AzureInstanceIDs,
                    RegularInstanceIDs = RegularInstanceIDs,
                    ObjectID = ObjectID,
                    ObjectName = ObjectName,
                    InstanceID = InstanceID,
                    DatabaseID = DatabaseID,
                    InstanceName = InstanceName,
                    JobID = JobID,
                    JobStepID = JobStepID,
                    Type = Type,
                    DatabaseName = DatabaseName,
                    ParentType = Parent == null ? TreeType.DBADashRoot : SQLTreeItemParent.Type,
                    DriveName = DriveName,
                    Report = Report,
                    ElasticPoolName = ElasticPoolName,
                    MasterInstanceID = MasterInstanceID
                };
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
        /// Populates lists of instance IDs: InstanceIDs, RegularInstanceIDs and AzureInstanceIDs
        /// No work is performed if list of IDs has already been populated.
        /// </summary>
        private void GetInstanceIDs()
        {
            if (_InstanceIDs == null)
            {
                // Note dupes are possible but HashSet maintains a distinct list
                _RegularInstanceIDs = new HashSet<int>();
                _AzureInstanceIDs = new HashSet<int>();
                _InstanceIDs = new HashSet<int>();
                if (Type is TreeType.DBADashRoot or TreeType.AzureInstance or TreeType.InstanceFolder)
                {
                    foreach (SQLTreeItem itm in Nodes) // Look down the tree for Instance/AzureDB nodes
                    {
                        if (itm.InstanceID > 0) // We have an instance to add
                        {
                            if (itm.Type == TreeType.AzureDatabase)
                            {
                                _AzureInstanceIDs.Add(itm.instanceID);
                            }
                            else
                            {
                                _RegularInstanceIDs.Add(itm.instanceID);
                            }
                            _InstanceIDs.Add(itm.InstanceID);
                        }
                        if (itm.Type is TreeType.AzureInstance or TreeType.InstanceFolder)
                        {
                            // We need to get the InstanceIDs from next level down
                            _InstanceIDs.UnionWith(itm.InstanceIDs);
                            _AzureInstanceIDs.UnionWith(itm.AzureInstanceIDs);
                            _RegularInstanceIDs.UnionWith(itm.RegularInstanceIDs);
                        }
                    }
                }
                else if (Type == TreeType.AzureDatabase) // Return a list with a single ID of the AzureDB
                {
                    _AzureInstanceIDs.Add(instanceID);
                    _InstanceIDs.Add(instanceID);
                }
                else if (Type == TreeType.Instance) // Return a list with a single ID of the Instance
                {
                    _RegularInstanceIDs.Add(instanceID);
                    _InstanceIDs.Add(instanceID);
                }
                else if (Type == TreeType.ElasticPool)
                {
                    _AzureInstanceIDs = Parent.Nodes.Cast<SQLTreeItem>().Where(n => n.Type == TreeType.AzureDatabase && string.Equals(n.ElasticPoolName, ElasticPoolName, StringComparison.InvariantCultureIgnoreCase)).Select(n => n.InstanceID).ToHashSet();
                    _InstanceIDs.UnionWith(_AzureInstanceIDs);
                }
            }
        }

        /// <summary>
        /// Return a list of instance IDs associated with the current node in the tree.  Includes AzureDB.
        /// </summary>
        public HashSet<int> InstanceIDs
        {
            get
            {
                if (Type is TreeType.DBADashRoot or TreeType.AzureInstance or TreeType.InstanceFolder or TreeType.Instance or TreeType.AzureDatabase or TreeType.ElasticPool)
                {
                    GetInstanceIDs();
                    return _InstanceIDs;
                }
                else
                {
                    return SQLTreeItemParent.InstanceIDs;
                }
            }
        }

        /// <summary>
        /// Return a list of instance IDs associated with the current node in the tree.  Excludes AzureDB.
        /// </summary>
        public HashSet<int> RegularInstanceIDs
        {
            get
            {
                if (Type is TreeType.DBADashRoot or TreeType.AzureInstance or TreeType.InstanceFolder or TreeType.Instance or TreeType.AzureDatabase or TreeType.ElasticPool)
                {
                    GetInstanceIDs();
                    return _RegularInstanceIDs;
                }
                else
                {
                    return SQLTreeItemParent.RegularInstanceIDs;
                }
            }
        }

        /// <summary>
        /// Return a list of AzureDB instance IDs associated with the current node in the tree.
        /// </summary>
        public HashSet<int> AzureInstanceIDs
        {
            get
            {
                if (Type is TreeType.DBADashRoot or TreeType.AzureInstance or TreeType.InstanceFolder or TreeType.Instance or TreeType.AzureDatabase or TreeType.ElasticPool)
                {
                    GetInstanceIDs();
                    return _AzureInstanceIDs;
                }
                else
                {
                    return SQLTreeItemParent.AzureInstanceIDs;
                }
            }
        }

        public int MasterInstanceID
        {
            get
            {
                if (Type is TreeType.ElasticPool or TreeType.AzureInstance)
                {
                    return CommonData.Instances.Rows.Cast<DataRow>()
                        .Where(r => string.Equals((string)r["Instance"], InstanceName, StringComparison.InvariantCultureIgnoreCase) && string.Equals((string)r["AzureDBName"].DBNullToNull(), "master", StringComparison.InvariantCultureIgnoreCase))
                        .Select(r => (int)r["InstanceID"])
                        .FirstOrDefault(0);
                }
                else
                {
                    return 0;
                }
            }
        }

        public string FullName() => string.IsNullOrEmpty(_schemaName) ? _objectName : _schemaName + "." + _objectName;

        public SQLTreeItem(string objectName, string schemaName, TreeType type) : base()
        {
            _objectName = objectName;
            _schemaName = schemaName;
            Type = type;
            Text = FullName();
            SetIcon();
        }

        public SQLTreeItem(string objectName, TreeType type) : base()
        {
            _objectName = objectName;
            Text = objectName;
            Type = type;
            SetIcon();
        }

        public SQLTreeItem(string objectName, TreeType type, Dictionary<string, object> attributes) : base()
        {
            _objectName = objectName;
            Text = objectName;
            Type = type;
            _attributes = attributes;
            SetIcon();
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

        private bool hasInstanceName;
        private string instanceName;
        private int instanceID;

        public int InstanceID
        {
            get => instanceID > 0 ? instanceID : Parent == null ? 0 : SQLTreeItemParent.InstanceID;
            set => instanceID = value;
        }

        public TreeType Type;

        public string InstanceName
        {
            get
            {
                if (hasInstanceName)
                {
                    return instanceName;
                }
                var n = this;
                do
                {
                    if (n.Type is TreeType.Instance or TreeType.AzureInstance)
                    {
                        instanceName = n.ObjectName;
                        hasInstanceName = true;
                    }
                    else if (n.Type == TreeType.DBADashRoot)
                    {
                        instanceName = string.Empty;
                        hasInstanceName = true;
                    }
                    else
                    {
                        n = n.SQLTreeItemParent;
                    }
                }
                while (n.Parent != null && !hasInstanceName);
                hasInstanceName = true;
                return instanceName;
            }
        }

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
            SQLTreeItem dummyNode = new("", "", TreeType.DummyNode);
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
            if (ContextMenuStrip.Items.Cast<ToolStripMenuItem>().Any(mnu => mnu.Text == @"Refresh")) return;
            var mnuRefresh = new ToolStripMenuItem("Refresh") { Image = Resources._112_RefreshArrow_Green_16x16_72 };
            ContextMenuStrip.Items.Add(mnuRefresh);
            mnuRefresh.Click += MnuRefresh_Click;
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
                    mnuInstanceActions.DropDownItems.AddRange(new ToolStripItem[] { mnuHidden, mnuRename, mnuDelete });
                    break;

                case TreeType.AzureDatabase:
                    mnuInstanceActions.DropDownItems.AddRange(new ToolStripItem[] { mnuDelete });
                    break;
            }
            ContextMenuStrip.Items.Add(mnuInstanceActions);
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
                IsVisibleInSummary = !mnu.Checked;
                SetIcon();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
}