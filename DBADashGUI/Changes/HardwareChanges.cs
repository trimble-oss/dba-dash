using DBADashGUI.CustomReports;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class HardwareChanges : UserControl, ISetContext
    {
        public HardwareChanges()
        {
            InitializeComponent();
        }

        private List<int> InstanceIDs;

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.RegularInstanceIDs.ToList();
            RefreshData();
        }

        public void RefreshData()
        {
            dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
            dgvHardware.Columns[0].Frozen = Common.FreezeKeyColumn;
            RefreshHistory();
            RefreshHardware();
        }

        private void RefreshHistory()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.HostUpgradeHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = dt;
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        private void RefreshHardware()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Hardware_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                dgvHardware.AutoGenerateColumns = false;
                dgvHardware.DataSource = dt;
                dgvHardware.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvHardware);
        }

        private void TsRefreshHardware_Click(object sender, EventArgs e)
        {
            RefreshHardware();
        }

        private void TsRefreshHistory_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsCopyHistory_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private readonly CellHighlightingRuleSetCollection HighlightingRules = new()
        {
            {
                "colInstantFileInitialization", new CellHighlightingRuleSet("colInstantFileInitialization")
                {
                    Rules = new List<CellHighlightingRule>
                    {
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                            Value1 = true.ToString(),
                            Status = DBADashStatus.DBADashStatusEnum.OK,
                        },
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                            Value1 = false.ToString(),
                            Status = DBADashStatus.DBADashStatusEnum.Warning,
                        },
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.All,
                            Status = DBADashStatus.DBADashStatusEnum.NA
                        }
                    }
                }
            },
            {
                "colOfflineSchedulers", new CellHighlightingRuleSet("colOfflineSchedulers")
                {
                    Rules = new List<CellHighlightingRule>
                    {
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                            Value1 = 0.ToString(),
                            Status = DBADashStatus.DBADashStatusEnum.OK,
                        },
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan,
                            Value1 = 0.ToString(),
                            Status = DBADashStatus.DBADashStatusEnum.Critical,
                        },
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.All,
                            Status = DBADashStatus.DBADashStatusEnum.NA
                        }
                    }
                }
            },
            {
                "colPowerPlan", new CellHighlightingRuleSet("ActivePowerPlanGUID",true)
                {
                    Rules = new List<CellHighlightingRule>
                    {
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.IsNull,
                            Status = DBADashStatus.DBADashStatusEnum.NA,
                        },
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                            Value1 = Common.HighPerformancePowerPlanGUID.ToString(),
                            Status = DBADashStatus.DBADashStatusEnum.OK,
                        },
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.All,
                            Status = DBADashStatus.DBADashStatusEnum.Warning
                        }
                    }
                }
            },
            {
                "colPriority", new CellHighlightingRuleSet("os_priority_class",true)
                {
                    Rules = new List<CellHighlightingRule>
                    {
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.IsNull,
                            Status = DBADashStatus.DBADashStatusEnum.NA,
                        },
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                            Value1 = 32.ToString(),
                            Status = DBADashStatus.DBADashStatusEnum.OK,
                        },
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.All,
                            Status = DBADashStatus.DBADashStatusEnum.Warning
                        }
                    }
                }
            },
            {
                "colAffinity", new CellHighlightingRuleSet("colAffinity")
                {
                    Rules = new List<CellHighlightingRule>
                    {
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.IsNull,
                            Status = DBADashStatus.DBADashStatusEnum.NA,
                        },
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                            Value1 = "AUTO",
                            Status = DBADashStatus.DBADashStatusEnum.OK,
                        },
                        new()
                        {
                            ConditionType = CellHighlightingRule.ConditionTypes.All,
                            Status = DBADashStatus.DBADashStatusEnum.Warning
                        }
                    }
                }
            }
        };

        private void DgvHardware_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            HighlightingRules.FormatRowsAdded(dgvHardware, e);
        }

        private void TsExcelHistory_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvHardware);
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            dgvHardware.PromptColumnSelection();
        }
    }
}