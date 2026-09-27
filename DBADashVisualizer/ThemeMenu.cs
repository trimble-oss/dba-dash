using DBADashGUI.Theme;
using DBADashGUI.Viewers;
using DBADashSharedGUI;
using Microsoft.Win32;

namespace DBADashVisualizer
{
    /// <summary>
    /// The stand-alone viewer's theme: resolved at start-up from the saved choice, falling back to Windows' own
    /// light/dark setting when nothing has been chosen, and switched live from a Theme item on the viewers'
    /// Settings menu - the viewer has no other settings screen to put it on.
    /// </summary>
    internal static class ThemeMenu
    {
        private const string SettingKey = "Theme";

        /// <summary>Applied once at start-up, before any window is shown.</summary>
        internal static void ApplyAtStartUp() => ThemeExtensions.CurrentTheme = Resolve(Saved());

        /// <summary>The Theme item for a viewer's Settings menu - Follow Windows, Default, White or Dark.</summary>
        internal static ToolStripItem MenuItem()
        {
            var menu = new ToolStripMenuItem("Theme");

            var auto = new ToolStripMenuItem("Follow Windows", null, (_, _) => Choose(null));
            var basic = new ToolStripMenuItem("Default", null, (_, _) => Choose(ThemeType.Default));
            var white = new ToolStripMenuItem("White", null, (_, _) => Choose(ThemeType.White));
            var dark = new ToolStripMenuItem("Dark", null, (_, _) => Choose(ThemeType.Dark));
            menu.DropDownItems.AddRange([auto, new ToolStripSeparator(), basic, white, dark]);

            // Read as the menu opens: the theme can be changed from another window the moment this one shows.
            menu.DropDownOpening += (_, _) =>
            {
                var saved = Saved();
                auto.Checked = saved is null;
                basic.Checked = saved == ThemeType.Default;
                white.Checked = saved == ThemeType.White;
                dark.Checked = saved == ThemeType.Dark;
            };

            return menu;
        }

        /// <summary>The saved choice, or null when there isn't one - which includes "Follow Windows", stored as a
        /// value that isn't one of <see cref="ThemeType"/>'s so it resolves the same way as no choice at all.</summary>
        private static ThemeType? Saved() =>
            Enum.TryParse(ViewerSettings.Store.Get(SettingKey) as string, ignoreCase: true, out ThemeType parsed)
                ? parsed
                : null;

        private static BaseTheme Resolve(ThemeType? saved) =>
            (saved ?? (WindowsUsesDarkTheme() ? ThemeType.Dark : ThemeType.Default)) switch
            {
                ThemeType.Dark => new DarkTheme(),
                ThemeType.White => new WhiteTheme(),
                _ => new BaseTheme()
            };

        private static bool WindowsUsesDarkTheme()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is 0;
        }

        /// <summary>Saves the choice - null to go back to following Windows - and re-themes every window already
        /// open, so switching takes effect straight away rather than needing a restart.</summary>
        private static void Choose(ThemeType? type)
        {
            try
            {
                ViewerSettings.Store.Set(SettingKey, type?.ToString() ?? "Auto");
                ViewerSettings.Save();

                var theme = Resolve(type);
                ThemeExtensions.CurrentTheme = theme;

                foreach (var form in Application.OpenForms.Cast<Form>().ToList()) form.ApplyTheme(theme);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Unable to change the theme");
            }
        }
    }
}
