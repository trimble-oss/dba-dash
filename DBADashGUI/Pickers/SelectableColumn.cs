using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Pickers
{
    public class SelectableColumn : ISelectable
    {
        private readonly DataGridViewColumn _column;

        public SelectableColumn(DataGridViewColumn column)
        {
            _column = column;
        }

        public string Name
        {
            get => _column.HeaderText;
            set => _column.HeaderText = value;
        }

        public bool IsVisible
        {
            get => _column.Visible;
            set => _column.Visible = value;
        }
    }
}
