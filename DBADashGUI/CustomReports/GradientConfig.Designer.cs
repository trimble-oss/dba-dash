namespace DBADashGUI.CustomReports
{
    partial class GradientConfig
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            label4 = new System.Windows.Forms.Label();
            txtColor2 = new System.Windows.Forms.TextBox();
            label5 = new System.Windows.Forms.Label();
            txtColor1 = new System.Windows.Forms.TextBox();
            pnlColor1 = new System.Windows.Forms.Panel();
            pnlColor2 = new System.Windows.Forms.Panel();
            numSteps = new System.Windows.Forms.NumericUpDown();
            label2 = new System.Windows.Forms.Label();
            txtMinValue = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            txtMaxValue = new System.Windows.Forms.TextBox();
            bttnOK = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            bttnColor1Increase = new System.Windows.Forms.Button();
            bttnColor1Decrease = new System.Windows.Forms.Button();
            bttnColor2Increase = new System.Windows.Forms.Button();
            bttnColor2Decrease = new System.Windows.Forms.Button();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)numSteps).BeginInit();
            SuspendLayout();
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(12, 20);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(57, 20);
            label4.TabIndex = 30;
            label4.Text = "Color 1";
            toolTip1.SetToolTip(label4, "First color in gradient pattern");
            // 
            // txtColor2
            // 
            txtColor2.Location = new System.Drawing.Point(180, 50);
            txtColor2.Name = "txtColor2";
            txtColor2.Size = new System.Drawing.Size(125, 27);
            txtColor2.TabIndex = 32;
            txtColor2.TextChanged += TxtColor2_TextChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(12, 53);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(57, 20);
            label5.TabIndex = 31;
            label5.Text = "Color 2";
            toolTip1.SetToolTip(label5, "Second color in gradient pattern");
            // 
            // txtColor1
            // 
            txtColor1.Location = new System.Drawing.Point(180, 17);
            txtColor1.Name = "txtColor1";
            txtColor1.Size = new System.Drawing.Size(125, 27);
            txtColor1.TabIndex = 29;
            txtColor1.TextChanged += TxtColor1_TextChanged;
            // 
            // pnlColor1
            // 
            pnlColor1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pnlColor1.Cursor = System.Windows.Forms.Cursors.Hand;
            pnlColor1.Location = new System.Drawing.Point(311, 17);
            pnlColor1.Name = "pnlColor1";
            pnlColor1.Size = new System.Drawing.Size(23, 23);
            pnlColor1.TabIndex = 33;
            toolTip1.SetToolTip(pnlColor1, "Click to select color");
            pnlColor1.Click += SetColor1;
            // 
            // pnlColor2
            // 
            pnlColor2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pnlColor2.Cursor = System.Windows.Forms.Cursors.Hand;
            pnlColor2.Location = new System.Drawing.Point(311, 53);
            pnlColor2.Name = "pnlColor2";
            pnlColor2.Size = new System.Drawing.Size(23, 23);
            pnlColor2.TabIndex = 34;
            toolTip1.SetToolTip(pnlColor2, "Click to select color");
            pnlColor2.Click += SetColor2;
            // 
            // numSteps
            // 
            numSteps.Location = new System.Drawing.Point(180, 87);
            numSteps.Name = "numSteps";
            numSteps.Size = new System.Drawing.Size(154, 27);
            numSteps.TabIndex = 38;
            numSteps.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(12, 89);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(45, 20);
            label2.TabIndex = 39;
            label2.Text = "Steps";
            toolTip1.SetToolTip(label2, "Number of steps in gradient pattern");
            // 
            // txtMinValue
            // 
            txtMinValue.Location = new System.Drawing.Point(180, 120);
            txtMinValue.Name = "txtMinValue";
            txtMinValue.Size = new System.Drawing.Size(154, 27);
            txtMinValue.TabIndex = 40;
            txtMinValue.Text = "0.00";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(12, 123);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(74, 20);
            label3.TabIndex = 41;
            label3.Text = "Min Value";
            toolTip1.SetToolTip(label3, "Minimum value.  This value or lower is assigned Color 1.   Values between min and max are assigjned a color between color 1 and color 2.");
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(12, 156);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(77, 20);
            label6.TabIndex = 42;
            label6.Text = "Max Value";
            toolTip1.SetToolTip(label6, "Maximum value.  This value or higher are assigned color 2.  Values between min and max are assigjned a color between color 1 and color 2.");
            // 
            // txtMaxValue
            // 
            txtMaxValue.Location = new System.Drawing.Point(180, 153);
            txtMaxValue.Name = "txtMaxValue";
            txtMaxValue.Size = new System.Drawing.Size(154, 27);
            txtMaxValue.TabIndex = 43;
            txtMaxValue.Text = "1.00";
            // 
            // bttnOK
            // 
            bttnOK.Location = new System.Drawing.Point(240, 205);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(94, 29);
            bttnOK.TabIndex = 44;
            bttnOK.Text = "&OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += BttnOK_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Location = new System.Drawing.Point(140, 205);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 45;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // bttnColor1Increase
            // 
            bttnColor1Increase.Image = Properties.Resources.IncreaseImageBrightness_16x;
            bttnColor1Increase.Location = new System.Drawing.Point(340, 17);
            bttnColor1Increase.Name = "bttnColor1Increase";
            bttnColor1Increase.Size = new System.Drawing.Size(31, 29);
            bttnColor1Increase.TabIndex = 47;
            toolTip1.SetToolTip(bttnColor1Increase, "Increase Brightness");
            bttnColor1Increase.UseVisualStyleBackColor = true;
            bttnColor1Increase.Click += BttnColor1Increase_Click;
            // 
            // bttnColor1Decrease
            // 
            bttnColor1Decrease.Image = Properties.Resources.ReduceImageBrightness_16x;
            bttnColor1Decrease.Location = new System.Drawing.Point(377, 17);
            bttnColor1Decrease.Name = "bttnColor1Decrease";
            bttnColor1Decrease.Size = new System.Drawing.Size(31, 29);
            bttnColor1Decrease.TabIndex = 46;
            toolTip1.SetToolTip(bttnColor1Decrease, "Decrease Brightness");
            bttnColor1Decrease.UseVisualStyleBackColor = true;
            bttnColor1Decrease.Click += BttnColor1Decrease_Click;
            // 
            // bttnColor2Increase
            // 
            bttnColor2Increase.Image = Properties.Resources.IncreaseImageBrightness_16x;
            bttnColor2Increase.Location = new System.Drawing.Point(340, 49);
            bttnColor2Increase.Name = "bttnColor2Increase";
            bttnColor2Increase.Size = new System.Drawing.Size(31, 29);
            bttnColor2Increase.TabIndex = 49;
            toolTip1.SetToolTip(bttnColor2Increase, "Increase Brightness");
            bttnColor2Increase.UseVisualStyleBackColor = true;
            bttnColor2Increase.Click += BttnColor2Increase_Click;
            // 
            // bttnColor2Decrease
            // 
            bttnColor2Decrease.Image = Properties.Resources.ReduceImageBrightness_16x;
            bttnColor2Decrease.Location = new System.Drawing.Point(377, 49);
            bttnColor2Decrease.Name = "bttnColor2Decrease";
            bttnColor2Decrease.Size = new System.Drawing.Size(31, 29);
            bttnColor2Decrease.TabIndex = 48;
            toolTip1.SetToolTip(bttnColor2Decrease, "Decrease Brightness");
            bttnColor2Decrease.UseVisualStyleBackColor = true;
            bttnColor2Decrease.Click += BttnColor2Decrease_Click;
            // 
            // GradientConfig
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(419, 253);
            Controls.Add(bttnColor2Increase);
            Controls.Add(bttnColor2Decrease);
            Controls.Add(bttnColor1Increase);
            Controls.Add(bttnColor1Decrease);
            Controls.Add(bttnCancel);
            Controls.Add(bttnOK);
            Controls.Add(txtMaxValue);
            Controls.Add(label6);
            Controls.Add(label3);
            Controls.Add(txtMinValue);
            Controls.Add(label2);
            Controls.Add(numSteps);
            Controls.Add(label4);
            Controls.Add(txtColor2);
            Controls.Add(label5);
            Controls.Add(txtColor1);
            Controls.Add(pnlColor1);
            Controls.Add(pnlColor2);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Name = "GradientConfig";
            Text = "Gradient Config";
            Load += GradientConfig_Load;
            ((System.ComponentModel.ISupportInitialize)numSteps).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtColor2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtColor1;
        private System.Windows.Forms.Panel pnlColor1;
        private System.Windows.Forms.Panel pnlColor2;
        private System.Windows.Forms.NumericUpDown numSteps;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtMinValue;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtMaxValue;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnColor1Increase;
        private System.Windows.Forms.Button bttnColor1Decrease;
        private System.Windows.Forms.Button bttnColor2Increase;
        private System.Windows.Forms.Button bttnColor2Decrease;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}