using Microsoft.SqlServer.Management.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI
{
    public class SQLTreeItem : TreeNode
    {
        private readonly Dictionary<string, object> _attributes;

        public Dictionary<string, object> Attributes
        {
            get
            {
                return _attributes;
            }
        }

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
            InstanceFolder
        }

        private DatabaseEngineEdition _engineEdition = DatabaseEngineEdition.Unknown;

        public DatabaseEngineEdition EngineEdition
        {
            get
            {
                if (_engineEdition == DatabaseEngineEdition.Unknown && Type != TreeType.DBADashRoot && this.Parent != null)
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

        private HashSet<int> _RegularInstanceIDs;
        private HashSet<int> _AzureInstanceIDs;
        private HashSet<int> _InstanceIDs;
        private DBADashContext InternalContext;
        public SQLTreeItem SQLTreeItemParent => Parent.AsSQLTreeItem();
        private bool IsChildOfInstanceOrAzureDB => InstanceID > 0 && !IsInstanceOrAzureDB;
        private bool IsChildOfInstanceOrAzureInstance => InstanceID > 0 && !IsInstanceOrAzureInstance;
        private bool IsInstanceOrAzureDB => Type == TreeType.Instance || Type == TreeType.AzureDatabase;
        private bool IsInstanceOrAzureInstance => Type == TreeType.Instance || Type == TreeType.AzureInstance;

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
                    ParentType = Parent == null ? TreeType.DBADashRoot : SQLTreeItemParent.Type
                };
                return InternalContext;
            }
        }

        public Guid JobID
        {
            get
            {
                if (this.Type == SQLTreeItem.TreeType.AgentJob)
                {
                    return (Guid)Tag;
                }
                else if (this.Type == SQLTreeItem.TreeType.AgentJobStep)
                {
                    return (Guid)Parent.Tag;
                }
                else
                {
                    return Guid.Empty;
                }
            }
        }

        public int JobStepID
        {
            get
            {
                if (this.Type == SQLTreeItem.TreeType.AgentJobStep)
                {
                    return (int)Tag;
                }
                else
                {
                    return -1;
                }
            }
        }

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
                    foreach (SQLTreeItem itm in this.Nodes) // Look down the tree for Instance/AzureDB nodes
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
                    _AzureInstanceIDs.Add(this.instanceID);
                    _InstanceIDs.Add(this.instanceID);
                }
                else if (Type == TreeType.Instance) // Return a list with a single ID of the Instance
                {
                    _RegularInstanceIDs.Add(this.instanceID);
                    _InstanceIDs.Add(this.instanceID);
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
                if (Type is TreeType.DBADashRoot or TreeType.AzureInstance or TreeType.InstanceFolder or TreeType.Instance or TreeType.AzureDatabase)
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
                if (Type is TreeType.DBADashRoot or TreeType.AzureInstance or TreeType.InstanceFolder or TreeType.Instance or TreeType.AzureDatabase)
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
                if (Type is TreeType.DBADashRoot or TreeType.AzureInstance or TreeType.InstanceFolder or TreeType.Instance or TreeType.AzureDatabase)
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

        public string FullName()
        {
            if (_schemaName == null || _schemaName.Length == 0)
            {
                return _objectName;
            }
            else
            {
                return _schemaName + "." + _objectName;
            }
        }

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

        private bool hasInstanceName = false;
        private string instanceName = null;
        private Int32 instanceID = 0;

        public Int32 InstanceID
        {
            get
            {
                if (instanceID > 0)
                {
                    return instanceID;
                }
                else
                {
                    if (this.Parent == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return SQLTreeItemParent.InstanceID;
                    }
                }
            }
            set
            {
                instanceID = value;
            }
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
        private Int32 _databaseID = -1;
        private string databaseName;

        public string ObjectName
        { get { return _objectName; } set { _objectName = value; this.Name = FullName(); } }

        public string SchemaName
        { get { return _schemaName; } set { _schemaName = value; this.Name = FullName(); } }

        public Int64 ObjectID { get; set; }

        public Int32 DatabaseID
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
            set
            {
                _databaseID = value;
            }
        }

        public string DatabaseName
        {
            get
            {
                if (Type is TreeType.DBADashRoot or TreeType.Instance or TreeType.AzureInstance)
                {
                    return String.Empty;
                }
                else if (Type is TreeType.Database or TreeType.AzureDatabase)
                {
                    return databaseName;
                }
                else if (databaseName != string.Empty && databaseName != null)
                {
                    return databaseName;
                }
                else
                {
                    databaseName = SQLTreeItemParent.DatabaseName;
                    return databaseName;
                }
            }
            set
            {
                databaseName = value;
            }
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
                    ImageIndex = 5;
                    break;

                case TreeType.InlineFunction:
                    ImageIndex = 5;
                    break;

                case TreeType.ScalarFunction:
                    ImageIndex = 5;
                    break;

                case TreeType.TableFunction:
                    ImageIndex = 5;
                    break;

                case TreeType.AggregateFunction:
                    ImageIndex = 5;
                    break;

                case TreeType.DatabaseTrigger:
                    ImageIndex = 5;
                    break;

                case TreeType.CLRAssembly:
                    ImageIndex = 5;
                    break;

                case TreeType.Folder:
                    ImageIndex = 3;
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
                    ImageIndex = 3;
                    break;

                case TreeType.HADR:
                    ImageIndex = 3;
                    break;

                case TreeType.DBAChecks:
                    ImageIndex = 10;
                    break;

                case TreeType.Tags:
                    ImageIndex = 11;
                    break;

                case TreeType.AgentJobStep:
                    string subsystem = (string)_attributes["subsystem"];
                    if (subsystem == "CmdExec")
                    {
                        ImageIndex = 12;
                    }
                    else if (subsystem == "PowerShell")
                    {
                        ImageIndex = 15;
                    }
                    else if (subsystem == "Ssis")
                    {
                        ImageIndex = 14;
                    }
                    else if (subsystem == "AnalysisQuery")
                    {
                        ImageIndex = 13;
                    }
                    else
                    {
                        ImageIndex = 16;
                    }
                    break;

                case TreeType.AgentJob:
                    if (Attributes.Count > 0 && (bool)Attributes["enabled"])
                    {
                        ImageIndex = 5;
                    }
                    else
                    {
                        ImageIndex = 17;
                    }
                    break;

                case TreeType.InstanceFolder:
                    ImageIndex = 20;
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
            this.Nodes.Add(dummyNode);
            AddRefreshContextMenu(this);
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
            AddContextMenu(nStoredProcs);
            AddContextMenu(nAggFunctions);
            AddContextMenu(nTableFunctions);
            AddContextMenu(nScalarFunctions);
            AddContextMenu(nDBTriggers);
            AddContextMenu(nTriggers);
            AddContextMenu(nAssemblies);
            AddContextMenu(nTableTypes);
            AddContextMenu(nDataTypes);
            AddContextMenu(nUserDefinedTypes);
            AddContextMenu(nXML);
            AddContextMenu(nViews);
            AddContextMenu(nTables);
            AddContextMenu(nTypes);
            AddContextMenu(nSeq);

            this.Nodes.Add(nTables);
            this.Nodes.Add(nViews);
            this.Nodes.Add(nProgrammability);

            this.Nodes.Add(nTypes);
            this.Nodes.Add(nSeq);
        }

        private void AddRefreshContextMenu(SQLTreeItem n)
        {
            var ctxMnu = new ContextMenuStrip();

            var mnuRefresh = new ToolStripMenuItem("Refresh");
            ctxMnu.Items.Add(mnuRefresh);
            mnuRefresh.Click += MnuRefresh_Click;
            mnuRefresh.Tag = n;
            n.ContextMenuStrip = ctxMnu;
        }

        private void MnuRefresh_Click(object sender, EventArgs e)
        {
            var itm = (SQLTreeItem)((ToolStripMenuItem)sender).Tag;
            var isExpanded = itm.IsExpanded;
            itm.Nodes.Clear();
            itm.AddDummyNode();
            itm.Collapse();
            if (isExpanded)
            {
                itm.Expand();
            }
        }

        private void AddContextMenu(SQLTreeItem n)
        {
            var ctxMnu = new ContextMenuStrip();
            var mnuFilter = ctxMnu.Items.Add("Filter");
            mnuFilter.Click += MnuFilter_Click;
            mnuFilter.Tag = n;
            n.ContextMenuStrip = ctxMnu;
        }

        public String FilterText = "";

        private void MnuFilter_Click(object sender, EventArgs e)
        {
            var itm = (SQLTreeItem)((ToolStripMenuItem)sender).Tag;
            string filter = itm.FilterText;
            if (Common.ShowInputDialog(ref filter, "Filter") == DialogResult.OK)
            {
                itm.Filter(filter);
            }
        }

        private readonly List<SQLTreeItem> unfilteredNodes = new();

        public void Filter(string filter)
        {
            this.Expand();
            if (unfilteredNodes.Count == 0)
            {
                foreach (SQLTreeItem itm in this.Nodes)
                {
                    unfilteredNodes.Add(itm)
;
                }
            }
            this.Nodes.Clear();
            foreach (SQLTreeItem n in unfilteredNodes)
            {
                if (filter == null || filter == "" || n.Text.ToLower().Contains(filter.ToLower()))
                {
                    this.Nodes.Add(n);
                }
            }
            if (unfilteredNodes.Count == this.Nodes.Count)
            {
                this.ImageIndex = 3;
                this.SelectedImageIndex = 3;
            }
            else
            {
                this.ImageIndex = 6;
                this.SelectedImageIndex = 6;
            }
            this.FilterText = filter;
        }

        private bool IsThisInstance(int InstanceID)
        {
            return IsInstanceOrAzureDB && this.InstanceID == InstanceID;
        }

        private bool IsThisInstance(string Instance)
        {
            return IsInstanceOrAzureDB && this.instanceName == Instance;
        }

        /// <summary>
        /// Find instance in the tree by ID
        /// </summary>
        public SQLTreeItem FindInstance(int InstanceID)
        {
            SQLTreeItem n = this;
            while (n.IsChildOfInstanceOrAzureDB) // If we are inside an instance node, navigate up until we get the instance ID.
            {
                n = n.SQLTreeItemParent;
            }
            if ((new TreeType[] { TreeType.DBAChecks, TreeType.HADR, TreeType.Configuration }).Contains(n.Type)) // Navigate up a level for these types
            {
                n = n.SQLTreeItemParent;
            }
            if (n.IsThisInstance(InstanceID)) // Check if we have the instance
            {
                return n;
            }
            // Look down the tree to find the instance
            return FindChildInstance(InstanceID, n);
        }

        /// <summary>
        /// Find instance in the tree by name
        /// </summary>
        public SQLTreeItem FindInstance(string instance)
        {
            SQLTreeItem n = this;
            while (n.IsChildOfInstanceOrAzureInstance) // If we are inside an instance node, navigate up until we get the instance ID.
            {
                n = n.SQLTreeItemParent;
            }
            if ((new TreeType[] { TreeType.DBAChecks, TreeType.HADR, TreeType.Configuration }).Contains(n.Type)) // Navigate up a level for these types
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
                if (child.Type is SQLTreeItem.TreeType.InstanceFolder or SQLTreeItem.TreeType.AzureInstance)
                {
                    SQLTreeItem find = FindChildInstance(instanceID, child);
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
                if (child.Type is SQLTreeItem.TreeType.InstanceFolder or SQLTreeItem.TreeType.AzureInstance)
                {
                    SQLTreeItem find = FindChildInstance(instance, child);
                    if (find != null)
                    {
                        return find;
                    }
                }
            }
            return null;
        }
    }
}