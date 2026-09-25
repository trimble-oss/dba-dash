namespace DBADashGUI.Viewers
{
    /// <summary>
    /// Keeps the viewers' settings with the GUI's other user settings, so they persist the way they always have and are
    /// shared with anything else that reads them.
    /// </summary>
    internal sealed class GuiViewerSettingsStore : IViewerSettingsStore
    {
        public object Get(string name) => Properties.Settings.Default[name];

        public void Set(string name, object value) => Properties.Settings.Default[name] = value;

        public void Save() => Properties.Settings.Default.Save();
    }
}
