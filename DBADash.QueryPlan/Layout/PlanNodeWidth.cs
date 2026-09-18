using System;

namespace DBADash.QueryPlan.Layout
{
    /// <summary>
    /// How wide operator nodes are allowed to be - a preset for <see cref="PlanLayoutOptions.MinNodeWidth"/>
    /// and <see cref="PlanLayoutOptions.MaxNodeWidth"/>, applied with <see cref="PlanLayoutOptions.SetNodeWidth"/>.
    ///
    /// A choice because the right width depends on what the plan is being read for.  A deep plan fits
    /// the screen at a legible zoom only when its columns are narrow; a plan full of long three part
    /// index names is only readable when they are not cut short.  Presets rather than a number, so
    /// the minimum and maximum move together and cannot be set the wrong way round.
    /// </summary>
    public enum PlanNodeWidth
    {
        /// <summary>
        /// The narrowest: the icon above the text rather than beside it, so the text has the node's
        /// whole width and long names wrap - see <see cref="PlanLayoutOptions.IconAboveText"/>.
        /// Nodes come out taller, which a plan has far more room for than width.
        /// </summary>
        Stacked,

        /// <summary>
        /// As narrow as a node can usefully be: the glyph and the start of the operator's name.  For
        /// getting the shape of a very deep plan on screen; the tooltip has the rest.
        /// </summary>
        SuperCompact,

        /// <summary>Narrow columns, so a deep plan fits.  Long names are cut short sooner.</summary>
        Compact,

        /// <summary>The default: most operator and object names fit.</summary>
        Normal,

        /// <summary>Room for longer object and index names.</summary>
        Wide,

        /// <summary>Wide enough that names are rarely cut short, at the cost of a much wider plan.</summary>
        Widest
    }

    public static class PlanNodeWidths
    {
        /// <summary>The narrowest and widest a node can be at a preset.</summary>
        public static (double Min, double Max) Range(PlanNodeWidth width) => width switch
        {
            PlanNodeWidth.Stacked => (56, 110),
            PlanNodeWidth.SuperCompact => (60, 130),
            PlanNodeWidth.Compact => (90, 190),
            PlanNodeWidth.Normal => (120, 300),
            PlanNodeWidth.Wide => (150, 450),
            PlanNodeWidth.Widest => (180, 800),
            _ => throw new ArgumentOutOfRangeException(nameof(width), width, null)
        };
    }
}
