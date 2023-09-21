﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Drawing;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using DocumentFormat.OpenXml.Office.Word;

namespace DBADashGUI
{
    /// <summary>
    /// Custom ToolStipDrownDownButton that shows status filters
    /// </summary>
    [ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip)]
    public class StatusFilterToolStrip : ToolStripDropDownButton
    {
        public StatusFilterToolStrip()
        {
            AddMenuItems();
            SetText();
        }

        public event EventHandler UserChangedStatusFilter;

        public override ToolStripItemDisplayStyle DisplayStyle { get => base.DisplayStyle; set => base.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText; }
        public override Image Image { get => base.Image; set => base.Image = Properties.Resources.FilterCircle_16x_Colors; }

        public bool OK { get => (MenuOK.Checked && OKVisible) || AllUnchecked; set => MenuOK.Checked = value; }
        public bool Warning { get => (MenuWarning.Checked && WarningVisible) || AllUnchecked; set => MenuWarning.Checked = value; }
        public bool Critical { get => (MenuCritical.Checked && CriticalVisible) || AllUnchecked; set => MenuCritical.Checked = value; }
        public bool NA { get => (MenuNA.Checked && NAVisible) || AllUnchecked; set => MenuNA.Checked = value; }
        public bool Acknowledged { get => (MenuAcknowledged.Checked && AcknowledgedVisible) || AllUnchecked; set => MenuAcknowledged.Checked = value; }

        public bool AllChecked => (MenuOK.Checked || !OKVisible) && (MenuWarning.Checked || !WarningVisible) && (MenuCritical.Checked || !CriticalVisible) && (MenuNA.Checked | !NAVisible) && (MenuAcknowledged.Checked || !AcknowledgedVisible);
        public bool AnyChecked => (MenuOK.Checked && OKVisible) || (MenuWarning.Checked && WarningVisible) || (MenuCritical.Checked && CriticalVisible) || (MenuNA.Checked && NAVisible) || (MenuAcknowledged.Checked && AcknowledgedVisible);
        public bool AllUnchecked => !AnyChecked;

        private readonly ToolStripMenuItem MenuOK = new() { Text = "OK", BackColor = DBADashStatus.DBADashStatusEnum.OK.GetBackColor(), ForeColor = DBADashStatus.DBADashStatusEnum.OK.GetBackColor().ContrastColor(), CheckOnClick = true };
        private readonly ToolStripMenuItem MenuWarning = new() { Text = "Warning", BackColor = DBADashStatus.DBADashStatusEnum.Warning.GetBackColor(), ForeColor = DBADashStatus.DBADashStatusEnum.Warning.GetBackColor().ContrastColor(), CheckOnClick = true };
        private readonly ToolStripMenuItem MenuCritical = new() { Text = "Critical", BackColor = DBADashStatus.DBADashStatusEnum.Critical.GetBackColor(), ForeColor = DBADashStatus.DBADashStatusEnum.Critical.GetBackColor().ContrastColor(), CheckOnClick = true };
        private readonly ToolStripMenuItem MenuNA = new() { Text = "N/A", BackColor = DBADashStatus.DBADashStatusEnum.NA.GetBackColor(), ForeColor = DBADashStatus.DBADashStatusEnum.NA.GetBackColor().ContrastColor(), CheckOnClick = true };
        private readonly ToolStripMenuItem MenuAcknowledged = new() { Text = "Acknowledged", BackColor = DBADashStatus.DBADashStatusEnum.Acknowledged.GetBackColor(), ForeColor = DBADashStatus.DBADashStatusEnum.Acknowledged.GetBackColor().ContrastColor(), CheckOnClick = true };
        private readonly ToolStripMenuItem MenuCheckAll = new() { Text = "Check ALL" };

        public bool AcknowledgedVisible { get => MenuAcknowledged.Available; set => MenuAcknowledged.Available = value; }
        public bool CriticalVisible { get => MenuCritical.Available; set => MenuCritical.Available = value; }
        public bool WarningVisible { get => MenuWarning.Available; set => MenuWarning.Available = value; }
        public bool NAVisible { get => MenuNA.Available; set => MenuNA.Available = value; }
        public bool OKVisible { get => MenuOK.Available; set => MenuOK.Available = value; }

        public SqlParameter[] GetSQLParams()
        {
            List<SqlParameter> sqlParams = new();
            if (CriticalVisible) { sqlParams.Add(new SqlParameter() { ParameterName = "IncludeCritical", DbType = System.Data.DbType.Boolean, Value = Critical }); }
            if (WarningVisible) { sqlParams.Add(new SqlParameter() { ParameterName = "IncludeWarning", DbType = System.Data.DbType.Boolean, Value = Warning }); }
            if (NAVisible) { sqlParams.Add(new SqlParameter() { ParameterName = "IncludeNA", DbType = System.Data.DbType.Boolean, Value = NA }); }
            if (AcknowledgedVisible) { sqlParams.Add(new SqlParameter() { ParameterName = "IncludeACK", DbType = System.Data.DbType.Boolean, Value = Acknowledged }); }
            if (OKVisible) { sqlParams.Add(new SqlParameter() { ParameterName = "IncludeOK", DbType = System.Data.DbType.Boolean, Value = OK }); }
            return sqlParams.ToArray();
        }

        private void CheckAll()
        {
            bool isChecked = !AllChecked;
            foreach (ToolStripMenuItem itm in this.DropDownItems.OfType<ToolStripMenuItem>())
            {
                if (itm != MenuCheckAll)
                {
                    itm.Checked = isChecked && itm.Available;
                }
            }
        }

        private void AddMenuItems()
        {
            MenuCritical.CheckedChanged += StatusFilterChanged;
            MenuOK.CheckedChanged += StatusFilterChanged;
            MenuWarning.CheckedChanged += StatusFilterChanged;
            MenuAcknowledged.CheckedChanged += StatusFilterChanged;
            MenuNA.CheckedChanged += StatusFilterChanged;

            MenuCritical.Click += Status_Click;
            MenuOK.Click += Status_Click;
            MenuWarning.Click += Status_Click;
            MenuAcknowledged.Click += Status_Click;
            MenuNA.Click += Status_Click;

            MenuCheckAll.Click += MenuCheckAll_Click;
            this.DropDownItems.AddRange(new ToolStripItem[] { MenuCritical, MenuWarning, MenuNA, MenuOK, MenuAcknowledged, new ToolStripSeparator(), MenuCheckAll });
        }

        private void Status_Click(object sender, EventArgs e)
        {
            UserChangedStatusFilter.Invoke(this, EventArgs.Empty);
        }

        private void MenuCheckAll_Click(object sender, EventArgs e)
        {
            CheckAll();
            UserChangedStatusFilter.Invoke(this, EventArgs.Empty);
        }

        private void StatusFilterChanged(object sender, EventArgs e)
        {
            SetText();
        }

        private void SetText()
        {
            MenuCheckAll.Text = AllChecked ? "Uncheck ALL" : "Check ALL";
            if (AllChecked || !AnyChecked)
            {
                Text = "ALL";
                Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Regular);
            }
            else if (AnyChecked)
            {
                Text = ((Critical ? ",Critical" : "") + (Warning ? ",Warning" : "") + (OK ? ",OK" : "") + (NA ? ",NA" : "") + (Acknowledged ? ",Acknowledged" : ""))[1..];
                Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold);
            }
            foreach (ToolStripMenuItem itm in this.DropDownItems.OfType<ToolStripMenuItem>())
            {
                itm.Font = itm.Checked ? new Font(itm.Font, System.Drawing.FontStyle.Bold) : new Font(itm.Font, System.Drawing.FontStyle.Regular);
            }
        }
    }
}