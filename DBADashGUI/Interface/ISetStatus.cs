using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.Interface
{
    public interface ISetStatus : IRefreshData
    {
        public void SetStatus(string message, string tooltip, Color color);
    }
}