using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class BuildReference : UserControl, IRefreshData
    {
        private readonly List<string> Links = new() { "https://dataplat.github.io/builds", "https://sqlserverupdates.com/", "https://sqlserverbuilds.blogspot.com/" };

        public BuildReference()
        {
            InitializeComponent();
            dgv.RegisterClearFilter(tsClearFilter);
        }

        private readonly List<DataGridViewColumn> ColumnsList = new()
        {
            new DataGridViewLinkColumn()  { DataPropertyName = "Name", HeaderText = "Name", ReadOnly = true, Name = "colName",Width=70, SortMode  = DataGridViewColumnSortMode.Automatic},
            new DataGridViewTextBoxColumn() { DataPropertyName = "Version", HeaderText = "Version", ReadOnly = true, Name = "colVersion", Width=100, SortMode = DataGridViewColumnSortMode.Programmatic },
            new DataGridViewTextBoxColumn() { DataPropertyName = "Major", HeaderText = "Major", ReadOnly = true, Name = "colMajor", Width=70 },
            new DataGridViewTextBoxColumn() { DataPropertyName = "Minor", HeaderText = "Minor", ReadOnly = true, Name = "colMinor",Width=70  },
            new DataGridViewTextBoxColumn() { DataPropertyName = "Build", HeaderText = "Build", ReadOnly = true, Name = "colBuild",Width=70  },
            new DataGridViewTextBoxColumn() { DataPropertyName = "Revision", HeaderText = "Revision", ReadOnly = true, Name = "colRevision",Width=70  },
            new DataGridViewTextBoxColumn() { DataPropertyName = "CU", HeaderText = "CU", ReadOnly = true, Name = "colCU",Width=70   },
            new DataGridViewTextBoxColumn() { DataPropertyName = "SP", HeaderText = "SP", ReadOnly = true, Name = "colSP" ,Width=70  },
            new DataGridViewLinkColumn() { DataPropertyName = "SupportedUntil", HeaderText = "Supported Until (Extended)", ReadOnly = true, Name = "colSupportedUntil",Width=100, SortMode = DataGridViewColumnSortMode.Automatic},
            new DataGridViewLinkColumn() { DataPropertyName = "MainstreamEndDate", HeaderText = "Supported Until (Mainstream)", ReadOnly = true, Name = "colMainstreamEndDate",Width=100, SortMode =  DataGridViewColumnSortMode.Automatic},
            new DataGridViewTextBoxColumn() { DataPropertyName = "CUBehind", HeaderText = "CU Behind", ReadOnly = true, Name = "colCUBehind", Width = 70},
            new DataGridViewTextBoxColumn() { DataPropertyName = "SPBehind", HeaderText = "SP Behind", ReadOnly = true, Name = "colSPBehind", Width=70   },
            new DataGridViewLinkColumn() { DataPropertyName = "KBList", HeaderText = "KB List", ReadOnly = true, Name = "colKBList", DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)},Width=150, SortMode  = DataGridViewColumnSortMode.Automatic   },
            new DataGridViewCheckBoxColumn() { DataPropertyName = "IsCurrentBuild", HeaderText = "Is Current Build", ReadOnly = true, Name = "colIsCurrentBuild", DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)  },Width=70, SortMode  = DataGridViewColumnSortMode.Automatic   }
        };

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedVersion
        {
            get
            {
                return versionToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>().Where(itm => itm.Checked).Select(itm => (string)itm.Tag).FirstOrDefault(string.Empty);
            }
            set
            {
                foreach (var item in versionToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
                {
                    item.Checked = (string)item.Tag == value;
                }
                SetRowFilter();
            }
        }

        private void SetRowFilter()
        {
            if (dgv.DataSource == null) return;
            var dv = (DataView)dgv.DataSource;
            var version = SelectedVersion;
            var filter = string.Empty;
            if (!string.IsNullOrEmpty(version))
            {
                filter = $"Name = '{version}'";
            }
            if (latestToolStripMenuItem.Checked)
            {
                filter += (string.IsNullOrEmpty(filter) ? "" : " AND ") + "IsCurrentBuild=1";
            }
            dv.RowFilter = filter;
            dv.Sort = "Major DESC,Minor DESC,Build DESC,Revision DESC";
        }

        public static DataTable GetBuildReference()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.BuildReference_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        public void RefreshData()
        {
            var dt = GetBuildReference();
            dgv.DataSource = new DataView(dt);
            var versionItems = new List<ToolStripItem>();
            var all = new ToolStripMenuItem("{All}") { Tag = string.Empty };
            all.Click += Version_Click;
            versionItems.Add(all);
            foreach (var name in dt.AsEnumerable().Select(r => r["Name"] as string).Distinct().OrderBy(s => s))
            {
                var item = new ToolStripMenuItem(name) { Tag = name };
                item.Click += Version_Click;
                versionItems.Add(item);
            }
            versionToolStripMenuItem.DropDownItems.AddRange(versionItems.ToArray());
            SetRowFilter();
        }

        private void Version_Click(object sender, EventArgs e)
        {
            var selected = (ToolStripMenuItem)sender;
            foreach (var item in versionToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = item == selected;
            }
            SetRowFilter();
        }

        private void LatestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRowFilter();
        }

        private void BuildReference_Load(object sender, EventArgs e)
        {
            dgv.AutoGenerateColumns = false;
            dgv.Columns.AddRange(ColumnsList.ToArray());
            dgv.ApplyTheme();
            var list = new List<ToolStripItem>();
            foreach (var link in Links)
            {
                var item = new ToolStripMenuItem(link);
                item.Click += delegate
                {
                    CommonShared.OpenURL(link);
                };
                list.Add(item);
            }
            tsLinks.DropDownItems.AddRange(list.ToArray());
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            switch (dgv.Columns[e.ColumnIndex].Name)
            {
                case "colKBList":
                    {
                        if (dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is not string KBs) return;
                        foreach (var KB in KBs.Split(","))
                        {
                            CommonShared.OpenURL("https://support.microsoft.com/help/" + KB.Trim());
                        }

                        break;
                    }
                case "colName":
                    {
                        var name = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
                        latestToolStripMenuItem.Checked = false;
                        SelectedVersion = name;
                        break;
                    }
                case "colMainstreamEndDate":
                case "colSupportedUntil":
                    var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                    var url = row["LifecycleUrl"] as string;
                    if (string.IsNullOrEmpty(url)) return;
                    CommonShared.OpenURL(url);
                    break;
            }
        }

        private void Dgv_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].Name == "colVersion")
            {
                var sort = "Major DESC,Minor DESC,Build DESC,Revision DESC";
                var dv = (DataView)dgv.DataSource;
                dv.Sort = dv.Sort == sort ? sort.Replace(" DESC", " ASC") : sort;
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(dgv);
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsReset_Click(object sender, EventArgs e)
        {
            SelectedVersion = string.Empty;
            latestToolStripMenuItem.Checked = true;
            SetRowFilter();
        }
    }
}