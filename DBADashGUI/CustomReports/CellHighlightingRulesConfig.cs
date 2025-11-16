using DBADash;
using DBADashGUI.Properties;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    public partial class CellHighlightingRulesConfig : Form
    {
        public CellHighlightingRulesConfig()
        {
            InitializeComponent();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public KeyValuePair<string, CellHighlightingRuleSet> CellHighlightingRules { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public DataGridViewColumnCollection ColumnList { get; set; }

        public DataGridViewColumn FormattedColumn => ColumnList.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Name == CellHighlightingRules.Key);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public object CellValue { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool CellValueIsNull { get; set; }

        private void CellHighlightingRulesConfig_Load(object sender, EventArgs e)
        {
            dgv.Columns.AddRange(new DataGridViewTextBoxColumn() { DataPropertyName = "RuleDescription", HeaderText = "Rule", Width = 200 },
                new DataGridViewTextBoxColumn() { Name = "Preview", HeaderText = "Preview", Width = 70, ReadOnly = true },
                new DataGridViewTextBoxColumn() { Name = "Status", HeaderText = "Status", Width = 80, DataPropertyName = "Status", ReadOnly = true },
                new DataGridViewLinkColumn() { Name = "ForeColor", DataPropertyName = "ForeColor", HeaderText = "Fore Color", Width = 80 },
                new DataGridViewLinkColumn() { Name = "BackColor", DataPropertyName = "BackColor", HeaderText = "Back Color", Width = 80 },
                new DataGridViewLinkColumn() { Name = "ForeColorDark", DataPropertyName = "ForeColorDark", HeaderText = "Fore Color (Dark)", Width = 80, Visible = false },
                new DataGridViewLinkColumn() { Name = "BackColorDark", DataPropertyName = "BackColorDark", HeaderText = "Back Color (Dark)", Width = 80, Visible = false },
                new DataGridViewLinkColumn() { Name = "SetFont", DataPropertyName = "Font", HeaderText = "Font", Text = "Set Font", Width = 100 },
                new DataGridViewButtonColumn() { Name = "ClearFont", DataPropertyName = "Font", HeaderText = "Clear Font", Text = "Clear Font", Width = 50, ToolTipText = "Clear Font" },
                new DataGridViewButtonColumn() { Name = "Delete", Text = "Delete", HeaderText = "Delete", Width = 60, ToolTipText = "Delete" },
                new DataGridViewButtonColumn() { Name = "MoveUp", HeaderText = "Move Up", Width = 50, Text = "Move Up", ToolTipText = "Move Up" },
                new DataGridViewButtonColumn() { Name = "MoveDown", HeaderText = "Move Down", Width = 50, Text = "Move Down", ToolTipText = "Move Down" },
                new DataGridViewButtonColumn() { Name = "Copy", HeaderText = "Copy", Width = 50, Text = "Copy", ToolTipText = "Copy" }

            );
            tsPaste.Enabled = SavedRules != null;

            cboConditionType.DataSource = new BindingSource()
            {
                DataSource = Enum.GetValues(typeof(CellHighlightingRule.ConditionTypes))
            };

            cboStatus.DataSource = new BindingSource
            {
                DataSource = Enum.GetValues(typeof(DBADashStatus.DBADashStatusEnum))
            };

            dgv.AutoGenerateColumns = false;
            RefreshGrid();
            cboTargetColumn.SelectedIndexChanged -= CboTargetColumn_SelectedIndexChanged;
            cboTargetColumn.DataSource = ColumnList.Cast<DataGridViewColumn>()
                .Select(column => new { Display = column.HeaderText.Replace("\n", " "), Value = column.Name })
                .ToList();
            cboTargetColumn.DisplayMember = "Display";
            cboTargetColumn.ValueMember = "Value";

            cboTargetColumn.SelectedIndexChanged += CboTargetColumn_SelectedIndexChanged;
            cboTargetColumn.SelectedValue = CellHighlightingRules.Value.TargetColumn;
            txtColumn.Text = CellHighlightingRules.Key;
            if (CellValue != null)
            {
                txtValue1.Text = CellValue.ToString() ?? string.Empty;
                cboConditionType.SelectedItem = CellHighlightingRule.ConditionTypes.Equals;
            }
            else if (CellValueIsNull)
            {
                cboConditionType.SelectedItem = CellHighlightingRule.ConditionTypes.IsNull;
            }
            IsStatusColumn.Checked = CellHighlightingRules.Value.IsStatusColumn;
            UpdateIsStatusColumn();
            this.ApplyTheme();
        }

        private void RefreshGrid()
        {
            dgv.DataSource = CellHighlightingRules.Value.Rules.Count == 0 ? null : new BindingList<CellHighlightingRule>(CellHighlightingRules.Value.Rules);
            dgv.ApplyTheme();
        }

        private void PnlForeColor_Click(object sender, EventArgs e)
        {
            Common.ShowColorDialog(pnlForeColor, txtForeColor);
        }

        private void PnlBackColor_Click(object sender, EventArgs e)
        {
            Common.ShowColorDialog(pnlBackColor, txtBackColor);
        }

        private void PnlForeColorDark_Click(object sender, EventArgs e)
        {
            Common.ShowColorDialog(pnlForeColorDark, txtForeColorDark);
        }

        private void PnlBackColorDark_Click(object sender, EventArgs e)
        {
            Common.ShowColorDialog(pnlBackColorDark, txtBackColorDark);
        }

        private void TxtForeColor_TextChanged(object sender, EventArgs e)
        {
            try
            {
                pnlForeColor.BackColor = ColorTranslator.FromHtml(txtForeColor.Text);
            }
            catch
            {
                // ignored
            }
        }

        private void TxtBackColor_TextChanged(object sender, EventArgs e)
        {
            try
            {
                pnlBackColor.BackColor = ColorTranslator.FromHtml(txtBackColor.Text);
            }
            catch
            {
                // ignored
            }
        }

        private void TxtForeColorDark_TextChanged(object sender, EventArgs e)
        {
            try
            {
                pnlForeColorDark.BackColor = ColorTranslator.FromHtml(txtForeColorDark.Text);
            }
            catch
            {
                // ignored
            }
        }

        private void TxtBackColorDark_TextChanged(object sender, EventArgs e)
        {
            try
            {
                pnlBackColorDark.BackColor = ColorTranslator.FromHtml(txtBackColorDark.Text);
            }
            catch
            {
                // ignored
            }
        }

        private void CboConditionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var conditionType = (CellHighlightingRule.ConditionTypes)cboConditionType.SelectedItem;
            txtValue1.Visible = conditionType != CellHighlightingRule.ConditionTypes.All && conditionType != CellHighlightingRule.ConditionTypes.IsNull && conditionType != CellHighlightingRule.ConditionTypes.IsNotNull;
            chkCaseSensitive.Visible = txtValue1.Visible;
            txtValue2.Visible = conditionType == CellHighlightingRule.ConditionTypes.Between;
            lblAnd.Visible = conditionType == CellHighlightingRule.ConditionTypes.Between;
        }

        private void KeyPressAllowNumericOnly(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != '-')
                e.Handled = true;

            switch (e.KeyChar)
            {
                // only allow one decimal point
                case '.' when ((TextBox)sender).Text.IndexOf('.') > -1:
                // only allow one minus sign
                case '-' when ((TextBox)sender).Text.IndexOf('-') > -1:
                    e.Handled = true;
                    break;
            }
        }

        private void BttnAdd_Click(object sender, EventArgs e)
        {
            var rule = GetHighlightingRule();

            if (!rule.RuleIsValid())
            {
                MessageBox.Show("Invalid rule: Please check the rule condition", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
            var ruleIndex = CellHighlightingRules.Value.Rules.FindIndex(r => r.RuleDescription == rule.RuleDescription);

            if (ruleIndex != -1) // Replace existing rule
            {
                if (MessageBox.Show("The rule already exists. Replace?", "Replace", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return;
                CellHighlightingRules.Value.Rules[ruleIndex] = rule;
            }
            else
            {
                CellHighlightingRules.Value.Rules.Add(rule);
            }
            RefreshGrid();
        }

        private bool RuleExists(CellHighlightingRule rule)
        {
            return CellHighlightingRules.Value.Rules.Any(r => r.RuleDescription == rule.RuleDescription);
        }

        private CellHighlightingRule GetHighlightingRule()
        {
            bool statusFormat = tabFormatting.SelectedTab == tabStatus;
            CellHighlightingRule rule = new()
            {
                ConditionType = (CellHighlightingRule.ConditionTypes)cboConditionType.SelectedItem,
                Status = statusFormat ? (DBADashStatus.DBADashStatusEnum)cboStatus.SelectedItem : null,
                Value1 = txtValue1.Text,
                Value2 = txtValue2.Text,
                BackColor = string.IsNullOrEmpty(txtBackColor.Text) || statusFormat ? Color.Empty : pnlBackColor.BackColor,
                ForeColor = string.IsNullOrEmpty(txtForeColor.Text) || statusFormat ? Color.Empty : pnlForeColor.BackColor,
                BackColorDark = string.IsNullOrEmpty(txtBackColorDark.Text) || statusFormat || !chkConfigureDark.Checked ? Color.Empty : pnlBackColorDark.BackColor,
                ForeColorDark = string.IsNullOrEmpty(txtForeColorDark.Text) || statusFormat || !chkConfigureDark.Checked ? Color.Empty : pnlForeColorDark.BackColor,
                CaseSensitive = chkCaseSensitive.Checked,
                Font = statusFormat ? null : selectedFont
            };
            return rule;
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtValue1.Text))
            {
                var rule = GetHighlightingRule();
                if (rule.RuleIsValid() && !RuleExists(rule))
                {
                    switch (MessageBox.Show("The configured rule hasn't been added yet.  Did you want to add it?",
                                "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning))
                    {
                        case DialogResult.Yes:
                            CellHighlightingRules.Value.Rules.Add(rule);
                            break;

                        case DialogResult.No:
                            break;

                        case DialogResult.Cancel:
                            return;
                    }
                }
            }
            DialogResult = DialogResult.OK;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            try
            {
                var columnName = dgv.Columns[e.ColumnIndex].Name;
                var readOnly = dgv.Rows[e.RowIndex].ReadOnly;
                switch (columnName)
                {
                    case "ForeColor":
                    case "BackColor":
                    case "ForeColorDark":
                    case "BackColorDark":
                        var color = (Color)(e.Value ?? Color.Empty);
                        e.Value = readOnly ? string.Empty : color == Color.Empty ? "Empty" : color.ToHexString();
                        break;

                    case "ClearFont":
                    case "Delete":
                    case "MoveUp":
                    case "MoveDown":
                    case "Copy":
                        var cell = dgv[e.ColumnIndex, e.RowIndex];
                        cell.ToolTipText = cell.OwningColumn.ToolTipText;
                        break;

                    case "SetFont":
                        var font = (Font)e.Value;
                        e.Value = readOnly ? string.Empty : font == null ? "{Default}" : font.GetDescription();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;

                var columnName = dgv.Columns[e.ColumnIndex].Name;
                switch (columnName)
                {
                    case "Delete":
                        DeleteRule(e.RowIndex);
                        break;

                    case "MoveUp":
                        MoveRuleUp(e.RowIndex);
                        break;

                    case "MoveDown":
                        MoveRuleDown(e.RowIndex);
                        break;

                    case "ForeColor":
                    case "BackColor":
                    case "ForeColorDark":
                    case "BackColorDark":
                        UpdateColor(e.RowIndex, columnName);
                        break;

                    case "SetFont":
                        UpdateFont(e.RowIndex);
                        break;

                    case "ClearFont":
                        UpdateFont(e.RowIndex, true);
                        break;

                    case "Copy":
                        CopyRule(e.RowIndex);
                        break;
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void CopyRule(int rowIndex)
        {
            var rule = CellHighlightingRules.Value.Rules[rowIndex];
            cboConditionType.SelectedItem = rule.ConditionType;
            txtValue1.Text = rule.Value1;
            txtValue2.Text = rule.Value2;
            txtForeColor.Text = rule.ForeColor == Color.Empty ? string.Empty : rule.ForeColor.ToHexString();
            txtBackColor.Text = rule.BackColor == Color.Empty ? string.Empty : rule.BackColor.ToHexString();
            txtForeColorDark.Text = rule.ForeColorDark == Color.Empty ? string.Empty : rule.ForeColorDark.ToHexString();
            txtBackColorDark.Text = rule.BackColorDark == Color.Empty ? string.Empty : rule.BackColorDark.ToHexString();
            tabFormatting.SelectedTab = rule.Status == null ? tabCustom : tabStatus;
            chkCaseSensitive.Checked = rule.CaseSensitive;
            SetSelectedFont(rule.Font);
            bttnClearFont.Enabled = selectedFont != null;
        }

        private void UpdateFont(int rowIndex, bool clear = false)
        {
            var rule = CellHighlightingRules.Value.Rules[rowIndex];
            if (clear)
            {
                rule.Font = null;
                RefreshGrid();
                return;
            }
            var frm = new FontDialog() { Font = rule.Font ?? dgv.DefaultCellStyle.Font };
            if (frm.ShowDialog() == DialogResult.OK)
            {
                rule.Font = frm.Font;
                RefreshGrid();
            }
        }

        private void DeleteRule(int rowIndex)
        {
            CellHighlightingRules.Value.Rules.RemoveAt(rowIndex);
            RefreshGrid();
        }

        private void MoveRuleUp(int rowIndex)
        {
            if (rowIndex <= 0) return;
            SwapRules(rowIndex, rowIndex - 1);
        }

        private void MoveRuleDown(int rowIndex)
        {
            var rules = CellHighlightingRules.Value.Rules;
            if (rowIndex >= rules.Count - 1) return;
            SwapRules(rowIndex, rowIndex + 1);
        }

        private void SwapRules(int index1, int index2)
        {
            var rules = CellHighlightingRules.Value.Rules;
            (rules[index1], rules[index2]) = (rules[index2], rules[index1]);
            RefreshGrid();
        }

        private void UpdateColor(int rowIndex, string columnName)
        {
            var cellValue = (Color)dgv.Rows[rowIndex].Cells[columnName].Value;
            var color = Common.ShowColorDialog(cellValue);
            if (!color.HasValue) return;

            switch (columnName)
            {
                case "ForeColor":
                    CellHighlightingRules.Value.Rules[rowIndex].ForeColor = color.Value;
                    break;

                case "BackColor":
                    CellHighlightingRules.Value.Rules[rowIndex].BackColor = color.Value;
                    break;

                case "ForeColorDark":
                    CellHighlightingRules.Value.Rules[rowIndex].ForeColorDark = color.Value;
                    break;

                case "BackColorDark":
                    CellHighlightingRules.Value.Rules[rowIndex].BackColorDark = color.Value;
                    break;
            }

            RefreshGrid();
        }

        private Font selectedFont;

        private void SetFont_Click(object sender, EventArgs e)
        {
            var defaultFont = FormattedColumn is DataGridViewLinkColumn
                ? new Font(dgv.DefaultCellStyle.Font, FontStyle.Underline)
                : dgv.DefaultCellStyle.Font;
            var frm = new FontDialog() { Font = selectedFont ?? defaultFont };
            if (frm.ShowDialog() == DialogResult.OK)
            {
                SetSelectedFont(frm.Font);
            }
        }

        private void SetSelectedFont(Font font)
        {
            selectedFont = font;
            bttnClearFont.Enabled = font != null;
            lblFont.Text = font == null ? "{Default}" : selectedFont.GetDescription();
            lblFont.Font = font ?? dgv.DefaultCellStyle.Font;
            toolTip1.SetToolTip(lblFont, font == null ? "" : font.ToString());
        }

        private void ClearFont_Click(object sender, EventArgs e)
        {
            SetSelectedFont(null);
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            SavedRules = CellHighlightingRules.Value.DeepCopy();
            tsPaste.Enabled = SavedRules != null;
        }

        private void TsPaste_Click(object sender, EventArgs e)
        {
            if (SavedRules == null) return;
            if (CellHighlightingRules.Value.Rules.Count > 0)
            {
                if (MessageBox.Show("Overwrite existing rules?", "Overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return;
            }
            CellHighlightingRules = new(CellHighlightingRules.Key, SavedRules.DeepCopy());
            CellHighlightingRules.Value.TargetColumn = cboTargetColumn.SelectedValue?.ToString();
            RefreshGrid();
        }

        private void CboTargetColumn_SelectedIndexChanged(object sender, EventArgs e)
        {
            CellHighlightingRules.Value.TargetColumn = cboTargetColumn.SelectedValue?.ToString();
        }

        private void TsGradient_Click(object sender, EventArgs e)
        {
            var frm = new GradientConfig();
            if (frm.ShowDialog() != DialogResult.OK) return;
            CellHighlightingRules.Value.IsStatusColumn = false;
            if (CellHighlightingRules.Value.Rules.Count != 0 && MessageBox.Show("Overwrite existing rules?", "Overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                CellHighlightingRules.Value.Rules = frm.CellHighlightingRules;
            }
            else
            {
                CellHighlightingRules.Value.Rules.AddRange(frm.CellHighlightingRules);
            }

            RefreshGrid();
        }

        private void TsClear_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Clear ALL rules?", "Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                DialogResult.Yes) return;
            CellHighlightingRules.Value.Rules.Clear();
            IsStatusColumn.Checked = false;
            RefreshGrid();
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                try
                {
                    if (idx >= CellHighlightingRules.Value.Rules.Count || idx < 0) continue;
                    var rule = CellHighlightingRules.Value.Rules[idx];
                    var foreColor = (Color)(((DataGridViewLinkCell)dgv.Rows[idx].Cells["ForeColor"]).Value ?? Color.Empty);
                    var backColor = (Color)(((DataGridViewLinkCell)dgv.Rows[idx].Cells["BackColor"]).Value ?? Color.Empty);
                    dgv.Rows[idx].Cells["Preview"].Value = "Preview";
                    if (rule.Status != null)
                    {
                        dgv.Rows[idx].Cells["Preview"].SetStatusColor((DBADashStatus.DBADashStatusEnum)rule.Status);
                        dgv.Rows[idx].Cells["Status"].SetStatusColor((DBADashStatus.DBADashStatusEnum)rule.Status);
                        dgv.Rows[idx].ReadOnly = true;
                    }
                    else
                    {
                        dgv.Rows[idx].Cells["Preview"].Style.BackColor = backColor;
                        dgv.Rows[idx].Cells["Preview"].Style.ForeColor = foreColor;
                        dgv.Rows[idx].Cells["Preview"].Style.Font = CellHighlightingRules.Value.Rules[idx].Font ?? dgv.DefaultCellStyle.Font;
                        dgv.Rows[idx].Cells["Status"].Style = dgv.DefaultCellStyle;
                        dgv.Rows[idx].ReadOnly = false;
                    }

                    dgv.Rows[idx].Cells["ForeColor"].Style.ForeColor = foreColor;
                    dgv.Rows[idx].Cells["BackColor"].Style.ForeColor = backColor;
                    dgv.Rows[idx].Cells["SetFont"].Style.Font = CellHighlightingRules.Value.Rules[idx].Font ?? dgv.DefaultCellStyle.Font;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private void Dgv_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            dgv.Cursor = dgv.Columns[e.ColumnIndex] is DataGridViewButtonColumn or DataGridViewLinkColumn ? Cursors.Hand : Cursors.Default;
        }

        private readonly Dictionary<string, Image> columnImages = new()
        {
            { "MoveUp", Resources.arrow_Up_16xLG },
            { "MoveDown", Resources.arrow_Down_16xLG },
            { "Delete", Resources.Close_red_16x},
            { "ClearFont", Resources.Eraser_16x},
            {"Copy", Resources.ASX_Copy_blue_16x }
        };

        private void Dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            try
            {
                const int imageHeight = 16;
                const int imageWidth = 16;

                if (e.RowIndex < 0)
                    return;

                var columnName = dgv.Columns[e.ColumnIndex].Name;
                if (!columnImages.TryGetValue(columnName, out var image)) return;
                // Paint the background
                e.PaintBackground(e.ClipBounds, true);

                // Calculate the image location
                var cellBounds = e.CellBounds;
                var imageLocation = new Point(
                    cellBounds.X + (cellBounds.Width - imageWidth) / 2,
                    cellBounds.Y + (cellBounds.Height - imageHeight) / 2);

                // Draw the image
                e.Graphics.DrawImage(image, imageLocation);

                e.Handled = true;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void ChkConfigureDark_CheckedChanged(object sender, EventArgs e)
        {
            lblForeColorDark.Visible = chkConfigureDark.Checked;
            lblBackColorDark.Visible = chkConfigureDark.Checked;
            txtForeColorDark.Visible = chkConfigureDark.Checked;
            txtBackColorDark.Visible = chkConfigureDark.Checked;
            pnlForeColorDark.Visible = chkConfigureDark.Checked;
            pnlBackColorDark.Visible = chkConfigureDark.Checked;
            dgv.Columns["ForeColorDark"].Visible = chkConfigureDark.Checked;
            dgv.Columns["BackColorDark"].Visible = chkConfigureDark.Checked;
            bttnForeColorDarkIncrease.Visible = chkConfigureDark.Checked;
            bttnForeColorDarkDecrease.Visible = chkConfigureDark.Checked;
            bttnBackColorDarkIncrease.Visible = chkConfigureDark.Checked;
            bttnBackColorDarkDecrease.Visible = chkConfigureDark.Checked;
        }

        private static float ColorBrightnessIncrement => Common.ColorBrightnessIncrement;

        private void BttnForeColorIncrease_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlForeColor, txtForeColor, ColorBrightnessIncrement);
        }

        private void BttnForeColorDecrease_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlForeColor, txtForeColor, -ColorBrightnessIncrement);
        }

        private void BttnBackColorIncrease_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlBackColor, txtBackColor, ColorBrightnessIncrement);
        }

        private void BttnBackColorDecrease_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlBackColor, txtBackColor, -ColorBrightnessIncrement);
        }

        private void BttnForColorDarkIncrease_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlForeColorDark, txtForeColorDark, ColorBrightnessIncrement);
        }

        private void BttnForeColorDarkDecrease_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlForeColorDark, txtForeColorDark, -ColorBrightnessIncrement);
        }

        private void BttnBackColorDarkIncrease_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlBackColorDark, txtBackColorDark, ColorBrightnessIncrement);
        }

        private void BttnBackColorDarkDecrease_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlBackColorDark, txtBackColorDark, -ColorBrightnessIncrement);
        }

        private void IsStatusColumn_CheckedChanged(object sender, EventArgs e)
        {
            UpdateIsStatusColumn();
            RefreshGrid();
        }

        private void UpdateIsStatusColumn()
        {
            CellHighlightingRules.Value.IsStatusColumn = IsStatusColumn.Checked;
            tabFormatting.Enabled = !IsStatusColumn.Checked;
            grpRule.Enabled = !IsStatusColumn.Checked;
            dgv.Enabled = !IsStatusColumn.Checked;
            bttnAdd.Enabled = !IsStatusColumn.Checked;
            chkConfigureDark.Enabled = !IsStatusColumn.Checked;
        }

        private void CboStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            var status = (DBADashStatus.DBADashStatusEnum)cboStatus.SelectedItem;
            cboStatus.BackColor = status.GetBackColor();
            cboStatus.ForeColor = status.GetForeColor();
        }

        /// <summary>
        /// For Copy/Paste
        /// </summary>
        private static CellHighlightingRuleSet SavedRules { get; set; }
    }
}