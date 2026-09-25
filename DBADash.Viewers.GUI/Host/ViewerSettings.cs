using System.Globalization;
using System.Runtime.CompilerServices;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// Where the viewers keep their settings - the layout, edge width and so on the user has chosen.  The host decides:
    /// the DBA Dash GUI keeps them with its other user settings, so a choice made in one place carries to the other, and
    /// the stand-alone viewer keeps its own.
    /// </summary>
    public interface IViewerSettingsStore
    {
        /// <summary>The stored value, or null when there isn't one.</summary>
        object Get(string name);

        void Set(string name, object value);

        void Save();
    }

    /// <summary>
    /// The viewers' settings, typed.  Each falls back to its default when the store has no value, so the viewers behave
    /// the same with a store that starts out empty.
    /// </summary>
    public static class ViewerSettings
    {
        /// <summary>
        /// Replace at start-up to keep the settings somewhere durable.  The default holds them for the life of the
        /// process, which is all a host that doesn't supply a store gets.
        /// </summary>
        public static IViewerSettingsStore Store { get; set; } = new MemorySettingsStore();

        public static string DeadlockLayoutStyle { get => Get("Ring"); set => Set(value); }
        public static string QueryPlanEdgeWidth { get => Get("Rows"); set => Set(value); }
        public static string QueryPlanEdgeWidthBasis { get => Get("Actual"); set => Set(value); }
        public static string QueryPlanOperatorTime { get => Get("Own"); set => Set(value); }
        public static bool QueryPlanShowOperatorDescriptions { get => Get(true); set => Set(value); }
        public static string QueryPlanNodeWidth { get => Get("Normal"); set => Set(value); }
        public static bool QueryPlanUniformColumnWidths { get => Get(true); set => Set(value); }
        public static string QueryPlanColumnSpacing { get => Get("Normal"); set => Set(value); }
        public static string QueryPlanVerticalLayout { get => Get("FirstChildAligned"); set => Set(value); }
        public static int QueryPlanPropertyRowLines { get => Get(3); set => Set(value); }
        public static bool QueryPlanWrapObjectNames { get => Get(false); set => Set(value); }
        public static bool QueryPlanShowNodeIds { get => Get(false); set => Set(value); }
        public static int QueryPlanMinFitZoom { get => Get(100); set => Set(value); }

        public static void Save() => Store.Save();

        private static T Get<T>(T fallback, [CallerMemberName] string name = null)
        {
            try
            {
                var stored = Store.Get(name);
                if (stored is T value) return value;

                // A store that keeps settings as text or JSON hands numbers back as another numeric type.
                return stored is IConvertible
                    ? (T)Convert.ChangeType(stored, typeof(T), CultureInfo.InvariantCulture)
                    : fallback;
            }
            catch
            {
                // A store that can't be read is not a reason to stop a plan opening.
                return fallback;
            }
        }

        private static void Set<T>(T value, [CallerMemberName] string name = null) => Store.Set(name, value);

        private sealed class MemorySettingsStore : IViewerSettingsStore
        {
            private readonly Dictionary<string, object> _values = new();

            public object Get(string name) => _values.GetValueOrDefault(name);

            public void Set(string name, object value) => _values[name] = value;

            public void Save()
            {
            }
        }
    }
}
