﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Performance
{
    public partial class PropertyGridDialog : Form
    {
        public string Title
        {
            get => this.Text;
            set => this.Text = value;
        }

        public object SelectedObject
        {
            get => propertyGrid1.SelectedObject;
            set => propertyGrid1.SelectedObject = value;
        }

        public PropertyGridDialog()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}