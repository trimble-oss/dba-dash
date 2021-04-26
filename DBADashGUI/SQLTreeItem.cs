using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI
{
    public class SQLTreeItem : TreeNode
    {

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
            HADR
        }

        private List<Int32> _childInstanceIDs;

        public List<Int32> ChildInstanceIDs { 
            get {
                
                if (_childInstanceIDs == null)
                {
                    _childInstanceIDs = new List<Int32>();
                    if( (instanceID==0) && (this.Type == TreeType.DBADashRoot || this.Type == TreeType.Instance || this.Type == TreeType.AzureInstance))
                    {
                        foreach (SQLTreeItem itm in this.Nodes)
                        {
                            if (itm.InstanceID > 0)
                            {
                                _childInstanceIDs.Add(itm.InstanceID);
                            }
                        }
                    }
                    
                }
                return _childInstanceIDs;
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
                return _schemaName + "." +  _objectName;
            }
        }
        public SQLTreeItem(string objectName, string schemaName, TreeType type) : base()
        {
            _objectName = objectName;
            _schemaName = schemaName;
            Type = type;
            Text = FullName();
            setIcon();

        }
        public SQLTreeItem(string objectName, TreeType type) : base()
        {
            _objectName = objectName;
            Text = objectName;
            this.Type = type;
            setIcon();
        }

        public SQLTreeItem(string objectName,string schemaName,string type)
        {
            switch (type)
            {
                case "P":
                    Type = TreeType.StoredProcedure;
                    break;
                case "V":
                    Type = TreeType.View;
                    break;
                case "IF":
                    Type = TreeType.InlineFunction;
                    break;
                case "U":
                    Type = TreeType.Table;
                    break;
                case "TF":
                    Type = TreeType.TableFunction;
                    break;
                case "FN":
                    Type = TreeType.ScalarFunction;
                    break;
                case "AF":
                    Type = TreeType.AggregateFunction;
                    break;
                case "DTR":
                    Type = TreeType.DatabaseTrigger;
                    break;
                case "CLR":
                    Type = TreeType.CLRAssembly;
                    break;
                case "FT":
                    Type = TreeType.CLRTableFunction;
                    break;
                case "FS":
                    Type = TreeType.CLRScalarFunction;
                    break;
                case "TYP":
                    Type = TreeType.UserDefinedDataType;
                    break;
                case "TT":
                    Type = TreeType.UserDefinedTableType;
                    break;
                case "UTY":
                    Type = TreeType.UserDefinedType;
                    break;
                case "XSC":
                    Type = TreeType.XMLSchemaCollection;
                    break;
                case "SO":
                    Type = TreeType.SequenceObject;
                    break;
                case "PC":
                    Type = TreeType.CLRProcedure;
                    break;
                case "TR":
                    Type = TreeType.Trigger;
                    break;
                case "TA":
                    Type = TreeType.CLRTrigger;
                    break;
                 default:
                    throw new ArgumentOutOfRangeException();
            }
            _objectName = objectName;
            _schemaName = schemaName;
            Text = FullName();
            setIcon();
        }

        private bool hasInstanceName=false;
        private string instanceName=null;
        private Int32 instanceID = 0;
        public Int32 InstanceID { 
            get {
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
                        return ((SQLTreeItem)this.Parent).InstanceID;
                    }
                }
            } 
            set {
                instanceID = value;
            } 
        }

        public TreeType Type;
        public string InstanceName {
            get
            {
                if (hasInstanceName)
                {
                    return instanceName;
                }
                var n = this;
                do
                {
                    if (n.Type == TreeType.Instance || n.Type== TreeType.AzureInstance)
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
                        n = (SQLTreeItem)n.Parent;
                    }
                }
                while (n.Parent != null && !hasInstanceName);
                hasInstanceName = true;
                return instanceName;
            }
        }
        public string _objectName;
        public string _schemaName;
        private Int32 _databaseID=-1;
        private string databaseName;
        public string ObjectName { get { return _objectName; } set { _objectName = value; this.Name = FullName(); } }
        public string SchemaName { get { return _schemaName; } set { _schemaName = value; this.Name = FullName(); } }
        public Int64 ObjectID { get; set; }
        public Int32 DatabaseID { 
            get { 
                if (Type== TreeType.DBADashRoot || Type == TreeType.Instance)
                {
                    return -1;
                }
                else if (_databaseID!=-1)
                {
                    return _databaseID;
                }
                else
                {
                    _databaseID = ((SQLTreeItem)Parent).DatabaseID;
                    return _databaseID;
                }
            } 
            set {
                _databaseID = value;
            } 
        }

        public string DatabaseName
        {
            get
            {
                if (Type == TreeType.DBADashRoot || Type == TreeType.Instance || Type == TreeType.AzureInstance)
                {
                    return String.Empty;
                }
                else if(Type== TreeType.Database || Type == TreeType.AzureDatabase)
                {
                    return this.ObjectName;
                }
                else if (databaseName != string.Empty && databaseName != null)
                {
                    return databaseName;
                }
                else
                {
                    databaseName= ((SQLTreeItem)Parent).DatabaseName;
                    return databaseName;
                }
            }
            set
            {
                databaseName = value;
            }
        }


        private void setIcon()
        {
            switch (this.Type)
            {
                case TreeType.DBADashRoot:
                    ImageIndex = 0;
                    break;
                case TreeType.Instance:
                    ImageIndex = 1;
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
                default:
                    ImageIndex = 5;
                    break;
            }
            SelectedImageIndex = ImageIndex;
        }

    private SQLTreeItem newFolder(string name,string tag,bool addDummyNode)
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
            SQLTreeItem dummyNode = new SQLTreeItem("", "", TreeType.DummyNode);
            this.Nodes.Add(dummyNode);
        }

        public void AddDatabaseFolders()
        {
         
            var nTables = newFolder("Tables","U",true);
            var nViews = newFolder("Views","V",true);
            var nProgrammability = newFolder("Programmability", null, false);
            var nStoredProcs = newFolder("Stored Procedures","P,PC",true);         
            var nTableFunctions = newFolder("Table Functions", "IF,TF,FT", true);
            var nScalarFunctions = newFolder("Scalar Functions","FN,FS",true);
            var nAggFunctions = newFolder("Aggregate Functions", "AF",true);
            var nDBTriggers = newFolder("Database Triggers", "DTR",true);
            var nAssemblies = newFolder("Assemblies", "CLR", true);
            var nTypes = newFolder("Types", "", false);
            var nTableTypes = newFolder("User-Defined Table Types", "TT", true);
            var nDataTypes = newFolder("User-Defined Data Types", "TYP", true);
            var nUserDefinedTypes = newFolder("User-Defined Types", "UTY", true);
            var nXML = newFolder("XML Schema Collections", "XSC", true);
            var nSeq = newFolder("Sequences", "SO", true);
            var nTriggers = newFolder("Triggers", "TA,TR", true);

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
            addContextMenu(nStoredProcs);
            addContextMenu(nAggFunctions);
            addContextMenu(nTableFunctions);
            addContextMenu(nScalarFunctions);
            addContextMenu(nDBTriggers);
            addContextMenu(nTriggers);
            addContextMenu(nAssemblies);
            addContextMenu(nTableTypes);
            addContextMenu(nDataTypes);
            addContextMenu(nUserDefinedTypes);
            addContextMenu(nXML);
            addContextMenu(nViews);
            addContextMenu(nTables);
            addContextMenu(nTypes);
            addContextMenu(nSeq);
           

            this.Nodes.Add(nTables);
            this.Nodes.Add(nViews);
            this.Nodes.Add(nProgrammability);
   
            this.Nodes.Add(nTypes);
            this.Nodes.Add(nSeq);
 
        }

        private void addContextMenu(SQLTreeItem n)
        {
            var ctxMnu = new ContextMenu();
            var mnuFilter = ctxMnu.MenuItems.Add("Filter");
            mnuFilter.Click += MnuFilter_Click;
            mnuFilter.Tag = n;
            n.ContextMenu = ctxMnu;
        }
        public String FilterText = "";
    
        private void MnuFilter_Click(object sender, EventArgs e)
        {
            var itm = (SQLTreeItem)((MenuItem)sender).Tag;
            string filter = itm.FilterText;
            if (Common.ShowInputDialog(ref filter,"Filter") == DialogResult.OK)
            {
                itm.Filter(filter);
            }
        }

        readonly List<SQLTreeItem> unfilteredNodes = new List<SQLTreeItem>();

        public void Filter(string filter)
        {
         
            this.Expand();
            if (unfilteredNodes.Count == 0)
            {
                foreach(SQLTreeItem itm in this.Nodes)
                {
                    unfilteredNodes.Add(itm)
;                }
            }
            this.Nodes.Clear();
            foreach(SQLTreeItem n in unfilteredNodes)
            {
                if (filter==null || filter=="" || n.Text.ToLower().Contains(filter.ToLower()))
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

    }
}
