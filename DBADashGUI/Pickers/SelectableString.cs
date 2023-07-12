using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.Pickers
{
    public class SelectableString : ISelectable
    {
        public SelectableString(string name)
        {
            Name = name;
            IsVisible = true; 
        }

        public SelectableString(string name,bool isVisible)
        {
            Name = name;
            IsVisible = IsVisible;
        }

        public string Name { get; set; }
        public bool IsVisible { get; set; }
    }
}
