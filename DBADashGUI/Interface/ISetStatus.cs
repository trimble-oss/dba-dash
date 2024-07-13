using System.Drawing;

namespace DBADashGUI.Interface
{
    public interface ISetStatus : IRefreshData
    {
        public void SetStatus(string message, string tooltip, Color color);
    }
}