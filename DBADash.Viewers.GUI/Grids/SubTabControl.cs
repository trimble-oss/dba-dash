using DBADashGUI.Theme;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// Tabs within a document - a query's Results, Messages and Statistics, or a comparison's runs - each a control
    /// filling its page.
    /// </summary>
    internal sealed class SubTabControl : ThemedTabControl
    {
        public SubTabControl()
        {
            Dock = DockStyle.Fill;
            Padding = new Point(12, 4);
        }

        /// <summary>Adds a tab showing <paramref name="content"/>, docked to fill it.</summary>
        public void AddTab(string text, Control content)
        {
            content.Dock = DockStyle.Fill;
            var page = new TabPage(text);
            page.Controls.Add(content);
            TabPages.Add(page);
        }

        public string[] TabNames => TabPages.Cast<TabPage>().Select(p => p.Text).ToArray();

        /// <summary>Shows the tab with this text - TabControl.SelectTab goes by the page's Name.  False if there isn't one.</summary>
        public bool SelectTabByText(string name)
        {
            var page = TabPages.Cast<TabPage>().FirstOrDefault(p => p.Text == name);
            if (page == null) return false;
            SelectedTab = page;
            return true;
        }
    }
}
