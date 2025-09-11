using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DBADash;
using DBADashGUI.Theme;

namespace DBADashGUI.CustomReports
{
    public partial class Pickers : Form
    {
        public CustomReport Report;
        private Picker SelectedPicker;
        private DataTable dtPickerItems;
        private List<Picker> PickerList;
        private bool IsBinding;

        public Pickers()
        {
            InitializeComponent();
            AddColumns();
            this.ApplyTheme();
        }

        private void AddColumns()
        {
            dgv.Columns.Clear();
            dgv.Columns.AddRange(
                    new DataGridViewTextBoxColumn() { Name="Value", HeaderText = "Value", DataPropertyName = "Value", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, DisplayIndex = 0},
                    new DataGridViewTextBoxColumn() {Name="Display", HeaderText = "Display", DataPropertyName = "Display", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, DisplayIndex = 1},
                    new DataGridViewButtonColumn() { Name="Up", HeaderText = "", Text = "↑", UseColumnTextForButtonValue  = true,AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells, DisplayIndex = 2},
                    new DataGridViewButtonColumn() { Name ="Down",HeaderText = "", Text = "↓",UseColumnTextForButtonValue = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells, DisplayIndex = 3}
                );
            dgv.ApplyTheme();
        }

        private void Pickers_Load(object sender, EventArgs e)
        {
            if (Report?.UserParams == null || !Report.UserParams.Any()) return;
            Report.Pickers ??= new();
            PickerList = Report.Pickers.DeepCopy();

            cboParams.DataSource = Report.UserParams.ToList();
            cboParams.DisplayMember = "ParamName";
            cboParams.SelectedIndex = 0;
        }

        private void CboParams_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedParam = (Param)cboParams.SelectedItem;
            if (selectedParam == null) return;
            SelectedPicker = PickerList.FirstOrDefault(p => p.ParameterName.TrimStart('@') == selectedParam.ParamName.TrimStart('@'));

            if (SelectedPicker == null)
            {
                SelectedPicker = new Picker { ParameterName = cboParams.Text, Name = selectedParam.ParamName.TrimStart('@'), DataType = selectedParam.ParamClrType };
                PickerList.Add(SelectedPicker);
            }
            if (SelectedPicker is DBPicker dbPicker)
            {
                optQuery.Checked = true;
                txtProcedureName.Text = dbPicker.StoredProcedureName;
                txtValueColumn.Text = dbPicker.ValueColumn;
                txtDisplayColumn.Text = dbPicker.DisplayColumn;
            }
            else if (SelectedPicker.IsText)
            {
                optText.Checked = true;
            }
            else
            {
                txtProcedureName.Text = string.Empty;
                txtValueColumn.Text = "Value";
                txtDisplayColumn.Text = "Display";
                optStandard.Checked = true;
                LoadPickerItems();
            }

            chkMenuBar.Checked = SelectedPicker.MenuBar;
            lblDataType.Text = "Data Type: " + SelectedPicker.DataType.Name;
            SetMode();
            txtDefault.Text = SelectedPicker.DefaultValue?.ToString() ?? "";
            txtName.Text = SelectedPicker.Name;
        }

        private void SetMode()
        {
            pnlQuery.Visible = optQuery.Checked;
            dgv.Visible = optStandard.Checked;
        }

        private void LoadPickerItems()
        {
            IsBinding = true;
            SelectedPicker.PickerItems ??= new();
            dtPickerItems = new DataTable();
            dtPickerItems.Columns.Add("Value", SelectedPicker.DataType);
            dtPickerItems.Columns.Add("Display", typeof(string));
            try
            {
                foreach (var kvp in SelectedPicker.PickerItems)
                {
                    var key = kvp.Key == null || kvp.Key.ToString() == "" ? null : kvp.Key;
                    dtPickerItems.Rows.Add(key, kvp.Value);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading picker items");
            }

            dgv.DataSource = dtPickerItems;
            IsBinding = false;
        }

        private void RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (SelectedPicker == null || IsBinding) return;
            try
            {
                SavePickerItems();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private Dictionary<object, string> PickerItems => dtPickerItems?.AsEnumerable()
            .ToDictionary(
                row => row.Field<object>("Value") ?? DBNull.Value,
                row => row.Field<string>("Display")
            );

        private void SavePickerItems()
        {
            SelectedPicker.PickerItems = PickerItems;
        }

        private void TxtName_Validated(object sender, EventArgs e)
        {
            SelectedPicker.Name = txtName.Text;
        }

        private void TxtDefault_Validating(object sender, CancelEventArgs e)
        {
            if (txtDefault.Text == string.Empty) return;
            try
            {
                var changeType = Convert.ChangeType(txtDefault.Text, SelectedPicker.DataType);
            }
            catch (Exception)
            {
                e.Cancel = true;
                MessageBox.Show($@"Invalid {SelectedPicker.DataType} value", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtDefault_Validated(object sender, EventArgs e)
        {
            SelectedPicker.DefaultValue = txtDefault.Text == string.Empty ? DBNull.Value : Convert.ChangeType(txtDefault.Text, SelectedPicker.DataType);
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void Save_Click(object sender, EventArgs e)
        {
            Report.Pickers = PickerList.Where(p => p.PickerItems?.Count > 0 || p.IsText).ToList();
            Report.Pickers = Report.Pickers.Count != 0 ? Report.Pickers : null;
            Report.Update();
            this.DialogResult = DialogResult.OK;
        }

        private void RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (dgv.DataSource == null || IsBinding) return;
            if (dgv.Rows.Count <= e.RowIndex || e.RowIndex == -1) return;
            if (dgv.Rows[e.RowIndex].IsNewRow) return; // Check if it's the new row placeholder
            if(e.ColumnIndex > 1) return; // Only validate the key & value columns

            var keyCell = dgv.Rows[e.RowIndex].Cells["Value"];
            if (keyCell.Value == null) return; // Check if the cell is initialized

            var keyValue = Convert.ToString(keyCell.Value);

            var isDuplicate = dgv.Rows.Cast<DataGridViewRow>()
                .Any(r => !r.IsNewRow && r.Index != e.RowIndex && string.Equals(Convert.ToString(r.Cells["Value"].Value), keyValue, StringComparison.Ordinal));

            if (isDuplicate)
            {
                e.Cancel = true;
                MessageBox.Show(@"Duplicate value", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Type_Change(object sender, EventArgs e)
        {
            SetMode();
            SelectedPicker = optQuery.Checked ? new DBPicker() { ParameterName = cboParams.Text, Name = txtName.Text.TrimStart('@'), DisplayColumn = txtDisplayColumn.Text, ValueColumn = txtValueColumn.Text, StoredProcedureName = txtProcedureName.Text }
                : new Picker() { ParameterName = cboParams.Text, Name = txtName.Text.TrimStart('@'), PickerItems = PickerItems, IsText = optText.Checked };
            PickerList.RemoveAll(p => string.Equals(p.ParameterName.TrimStart('@'), SelectedPicker.ParameterName.TrimStart('@'), StringComparison.OrdinalIgnoreCase));
            PickerList.Add(SelectedPicker);
        }

        private void TxtProcedureName_Validated(object sender, EventArgs e)
        {
            if (SelectedPicker is DBPicker dbPicker)
                dbPicker.StoredProcedureName = txtProcedureName.Text;
        }

        private void TxtValueColumn_Validated(object sender, EventArgs e)
        {
            if (SelectedPicker is DBPicker dbPicker)
                dbPicker.ValueColumn = txtValueColumn.Text;
        }

        private void TxtDisplayColumn_Validated(object sender, EventArgs e)
        {
            if (SelectedPicker is DBPicker dbPicker)
                dbPicker.DisplayColumn = txtDisplayColumn.Text;
        }

        private void DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            CommonShared.ShowExceptionDialog(e.Exception);
        }

        private void MenuBar_Click(object sender, EventArgs e)
        {
            SelectedPicker.MenuBar = chkMenuBar.Checked;
        }

        private void CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var columnHeader = dgv.Columns[e.ColumnIndex].Name;

            switch (columnHeader)
            {
                case "Up":
                    MoveDataTableRowUp(e.RowIndex);
                    break;
                case "Down":
                    MoveDataTableRowDown(e.RowIndex);
                    break;
            }
        }

        private void MoveDataTableRowDown(int rowIndex)
        {
            MoveDataTableRowUp(rowIndex + 1);
            dgv.ClearSelection();
            dgv.Rows[rowIndex+1].Selected = true;
        }

        private void MoveDataTableRowUp(int rowIndex)
        {
            if (dgv.DataSource is not DataTable dataTable || rowIndex <= 0 || rowIndex >= dataTable.Rows.Count) return;
            dgv.DataSource = null;
            // Get the row to be moved.
            var rowToMove = dataTable.Rows[rowIndex];

            // Create a new DataRow with the same data.
            var newRow = dataTable.NewRow();
            newRow.ItemArray = rowToMove.ItemArray;

            // Remove the original row from the table first.
            // This frees up the primary key value.
            dataTable.Rows.Remove(rowToMove);

            // Now, insert the new row at the new position.
            dataTable.Rows.InsertAt(newRow, rowIndex - 1);
            AddColumns();
            dgv.DataSource = dataTable;
            dgv.ClearSelection();
            dgv.Rows[rowIndex-1].Selected = true;
        }

    }
}