using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.CustomReports
{
    public partial class GradientConfig : Form
    {
        public GradientConfig()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public List<CellHighlightingRule> CellHighlightingRules { get; } = new();

        private void SetColor1(object sender, EventArgs e)
        {
            Common.ShowColorDialog(pnlColor1, txtColor1);
        }

        private void SetColor2(object sender, EventArgs e)
        {
            Common.ShowColorDialog(pnlColor2, txtColor2);
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtMinValue.Text, out var minValue))
            {
                MessageBox.Show("Invalid minimum value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!decimal.TryParse(txtMaxValue.Text, out var maxValue))
            {
                MessageBox.Show("Invalid maximum value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (minValue >= maxValue)
            {
                MessageBox.Show("Minimum value must be less than maximum value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            for (var i = 0; i < numSteps.Value; i++)
            {
                var value = minValue + (maxValue - minValue) * i / (numSteps.Value - 1);

                var backColor = Color.FromArgb(
                    (int)(pnlColor1.BackColor.R +
                          (pnlColor2.BackColor.R - pnlColor1.BackColor.R) * i / (numSteps.Value - 1)),
                    (int)(pnlColor1.BackColor.G +
                          (pnlColor2.BackColor.G - pnlColor1.BackColor.G) * i / (numSteps.Value - 1)),
                    (int)(pnlColor1.BackColor.B +
                          (pnlColor2.BackColor.B - pnlColor1.BackColor.B) * i / (numSteps.Value - 1)));
                CellHighlightingRules.Add(new CellHighlightingRule
                {
                    ConditionType = CellHighlightingRule.ConditionTypes.LessThan,
                    Value1 = value.ToString(),
                    BackColor = backColor,
                    ForeColor = backColor.ContrastColorTrimble()
                });
            }
            CellHighlightingRules.Add(new CellHighlightingRule
            {
                ConditionType = CellHighlightingRule.ConditionTypes.All,
                BackColor = pnlColor2.BackColor,
                ForeColor = pnlColor2.BackColor.ContrastColorTrimble()
            });

            this.DialogResult = DialogResult.OK;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void GradientConfig_Load(object sender, EventArgs e)
        {
            pnlColor1.BackColor = DashColors.White;
            pnlColor2.BackColor = DashColors.TrimbleBlueDark;
            txtColor1.Text = pnlColor1.BackColor.ToHexString();
            txtColor2.Text = pnlColor2.BackColor.ToHexString();
        }

        private void TxtColor1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                pnlColor1.BackColor = ColorTranslator.FromHtml(txtColor1.Text);
            }
            catch
            {
                // ignored
            }
        }

        private void TxtColor2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                pnlColor2.BackColor = ColorTranslator.FromHtml(txtColor2.Text);
            }
            catch
            {
                // ignored
            }
        }

        private static float ColorBrightnessIncrement => Common.ColorBrightnessIncrement;

        private void BttnColor1Increase_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlColor1, txtColor1, ColorBrightnessIncrement);
        }

        private void BttnColor1Decrease_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlColor1, txtColor1, -ColorBrightnessIncrement);
        }

        private void BttnColor2Increase_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlColor2, txtColor2, ColorBrightnessIncrement);
        }

        private void BttnColor2Decrease_Click(object sender, EventArgs e)
        {
            Common.AdjustColorBrightness(pnlColor2, txtColor2, -ColorBrightnessIncrement);
        }
    }
}