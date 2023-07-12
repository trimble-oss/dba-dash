using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.Pickers
{
    public interface ISelectable
    {
        string Name { get; set; }
        bool IsVisible { get; set; }
    }
}
