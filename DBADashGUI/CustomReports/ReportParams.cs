using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    public partial class ReportParams : Form
    {
        public List<CustomSqlParameter> Params;

        public ReportParams()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private static string DateTimePattern => CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;

        private readonly DateTimePicker dtp = new() { Visible = false, Format = DateTimePickerFormat.Custom, CustomFormat = DateTimePattern }; // DateTimePicker

        private void ReportParams_Load(object sender, EventArgs e)
        {
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn() { Name = "ParamName", HeaderText = "Parameter Name", ReadOnly = true, Width = 200, DefaultCellStyle = { BackColor = DashColors.NotApplicable, ForeColor = Color.Black } },
                new DataGridViewCheckBoxColumn() { Name = "PassNull", HeaderText = "Pass NULL", Width = 70 },
                new DataGridViewCheckBoxColumn() { Name = "Default", HeaderText = "Default", Width = 70 },
                new DataGridViewTextBoxColumn() { Name = "Value", HeaderText = "Value", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable }

                );

            foreach (var param in Params.Where(p => !CustomReport.SystemParamNames.Contains(p.Param.ParameterName, StringComparer.CurrentCultureIgnoreCase)))
            {
                var row = dgv.Rows.Add(new object[] { param.Param.ParameterName, (param.Param.Value == DBNull.Value) && !param.UseDefaultValue, param.UseDefaultValue, param.Param.Value });
                SetRowReadOnly(row);
            }

            dgv.CellBeginEdit += Dgv_CellBeginEdit;
            dgv.CellEndEdit += Dgv_CellEndEdit;
            dgv.Controls.Add(dtp); //
            dtp.TextChanged += new EventHandler(Dtp_OnTextChange);
            dgv.ColumnWidthChanged += Dgv_ColumnWidthChanged;
            dgv.RowsAdded += Dgv_RowsAdded;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.CellValueChanged += Dgv_CellValueChanged;
        }

        private void Dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex is 1 or 2 && e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 1 && (bool)dgv.Rows[e.RowIndex].Cells[1].Value && (bool)dgv.Rows[e.RowIndex].Cells[2].Value)
                {
                    dgv.Rows[e.RowIndex].Cells[2].Value = false;
                }
                else if (e.ColumnIndex == 2 && (bool)dgv.Rows[e.RowIndex].Cells[1].Value && (bool)dgv.Rows[e.RowIndex].Cells[2].Value)
                {
                    dgv.Rows[e.RowIndex].Cells[1].Value = false;
                }
                SetRowReadOnly(e.RowIndex);
            }
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex is 1 or 2 && e.RowIndex >= 0)
            {
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void SetRowReadOnly(int rowIndex)
        {
            var ro = (bool)dgv.Rows[rowIndex].Cells[1].Value || (bool)dgv.Rows[rowIndex].Cells[2].Value;
            dgv.Rows[rowIndex].Cells[3].ReadOnly = ro;
            dgv.Rows[rowIndex].Cells[3].Style.BackColor = ro
                ? DashColors.NotApplicable
                : DBADashUser.SelectedTheme.BackgroundColor;
            dgv.Rows[rowIndex].Cells[3].Style.ForeColor = ro
                ? Color.Black
                : DBADashUser.SelectedTheme.ForegroundColor;
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                SetRowReadOnly(idx);
            }
        }

        /// <summary>
        /// Provides a DateTime picker for editing if cell is DateTime
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dgv_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (dgv.CurrentCell.Value is not DateTime) return;
            dtp.Location = dgv.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false).Location;
            dtp.Size = dgv.CurrentCell.Size;
            dtp.Visible = true;

            if (dgv.CurrentCell.Value != null)
            {
                dtp.Value = (DateTime)dgv.CurrentCell.Value;
            }
        }

        /// <summary>
        /// Keep size of DateTime picker control consistent with size of column during editing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dgv_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            if (!dtp.Visible) return;
            var rect = dgv.GetCellDisplayRectangle(dgv.CurrentCell.ColumnIndex, dgv.CurrentCell.RowIndex, false);
            dtp.Location = rect.Location;
            dtp.Size = rect.Size;
        }

        /// <summary>
        /// Hide DateTime picker control when editing is complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dgv_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            dtp.Visible = false;
        }

        private void Dtp_OnTextChange(object sender, EventArgs e)
        {
            dgv.CurrentCell.Value = dtp.Value;
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var p = Params.First(p => p.Param.ParameterName == (string)row.Cells[0].Value);
                p.Param.Value = (bool)row.Cells[1].Value ? DBNull.Value : row.Cells[3].Value;
                p.UseDefaultValue = (bool)row.Cells[2].Value;
            }
            this.DialogResult = DialogResult.OK;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}