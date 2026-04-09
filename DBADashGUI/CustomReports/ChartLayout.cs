using DBADashGUI.Charts;
using DBADashGUI.Theme;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    public partial class ChartLayout : Form
    {
        private bool suppressEvents = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ChartCount { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Columns { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Rows { get; set; }

        public ChartLayout()
        {
            InitializeComponent();
        }

        private void Rows_Changed(object sender, EventArgs e)
        {
            if (suppressEvents) return;
            suppressEvents = true;
            var layout = ChartHelper.CalculateLayout(ChartCount, Convert.ToInt32(numRows.Value), null);
            numColumns.Value = layout.columns;
            UpdateSpan();
            suppressEvents = false;
        }

        private void Columns_Changed(object sender, EventArgs e)
        {
            if (suppressEvents) return;
            suppressEvents = true;
            var layout = ChartHelper.CalculateLayout(ChartCount, null, Convert.ToInt32(numColumns.Value));
            numRows.Value = layout.rows;
            UpdateSpan();
            suppressEvents = false;
        }

        private void ChartLayout_Load(object sender, EventArgs e)
        {
            if (ChartCount == 0)
            {
                MessageBox.Show("No charts to layout", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
                return;
            }
            suppressEvents = true;
            chkAuto.Checked = Rows == 0 && Columns == 0;
            numRows.Maximum = ChartCount;
            numColumns.Maximum = ChartCount;
            var layout = ChartHelper.CalculateLayout(ChartCount, Rows, Columns);
            numRows.Value = layout.rows;
            numColumns.Value = layout.columns;
            UpdateSpan();
            suppressEvents = false;
            this.ApplyTheme();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            Rows = chkAuto.Checked ? 0 : Convert.ToInt32(numRows.Value);
            Columns = chkAuto.Checked ? 0 : Convert.ToInt32(numColumns.Value);
            DialogResult = DialogResult.OK;
        }

        private void UpdateSpan()
        {
            var cols = Convert.ToInt32(numColumns.Value);
            var rows = Convert.ToInt32(numRows.Value);
            var span = (rows * cols) - ChartCount + 1;
            span = Math.Min(cols, span);
            lblSpan.Text = $"1st chart will span: {span} columns";
            lblSpan.Visible = span > 1;
        }

        private void Auto_CheckChanged(object sender, EventArgs e)
        {
            numColumns.Enabled = !chkAuto.Checked;
            if (chkAuto.Checked)
            {
                var layout = ChartHelper.CalculateLayout(ChartCount, null, null);
                numRows.Value = layout.rows;
                numColumns.Value = layout.columns;
                UpdateSpan();
            }
        }
    }
}