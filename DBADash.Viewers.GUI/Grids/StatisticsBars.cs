using DBADashGUI.CustomReports;
using DBADashSharedGUI;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// The data bars the statistics tables start with, so the statement or table doing the most stands out without
    /// reading every row - set on a table's columns, for <see cref="GridViewerControl"/> to show.  The same bars a user
    /// adds from a grid's menu, so they can be changed or removed the same way.
    /// </summary>
    internal static class StatisticsBars
    {
        /// <summary>
        /// Time and I/O, where more is worse: blue, amber from a fifth of the scale, red from half - the plan viewer's
        /// bars.  <paramref name="maximum"/> fixes the full bar, e.g. at 100 for a percentage.
        /// </summary>
        public static void MoreIsWorse(DataColumn column, decimal? maximum = null)
        {
            var settings = DataBarSettings.MoreIsWorse();
            settings.Maximum = maximum;
            Set(column, settings);
        }

        /// <summary>A count that's neither good nor bad - rows affected - in one colour.</summary>
        public static void Neutral(DataColumn column) =>
            Set(column, new DataBarSettings { ColorMode = DataBarColorMode.Solid, Color = DashColors.BlueLight });

        /// <summary>A change from the first run: red for more, green for less.</summary>
        public static void Change(DataColumn column) =>
            Set(column, new DataBarSettings { ColorMode = DataBarColorMode.PositiveNegative });

        /// <summary>
        /// Puts <paramref name="columns"/> on one scale - the highest value in any of them - so the same figure in each
        /// run's column is the same length of bar, rather than each run scaled to its own highest.
        /// </summary>
        public static void ShareScale(DataTable table, IReadOnlyCollection<string> columns)
        {
            var maximum = table.Rows.Cast<DataRow>()
                .SelectMany(r => columns.Select(c => r[c]))
                .Where(v => v is long)
                .Select(v => (decimal)(long)v)
                .DefaultIfEmpty()
                .Max();
            if (maximum <= 0) return;
            foreach (var name in columns)
            {
                if (table.Columns[name]?.ExtendedProperties[GridViewerControl.DataBarProperty] is DataBarSettings settings)
                {
                    settings.Maximum = maximum;
                }
            }
        }

        private static void Set(DataColumn column, DataBarSettings settings) =>
            column.ExtendedProperties[GridViewerControl.DataBarProperty] = settings;
    }
}
