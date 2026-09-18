using System;

namespace DBADash.QueryPlan.Layout
{
    /// <summary>
    /// How much room is left between columns of nodes - a preset for
    /// <see cref="PlanLayoutOptions.ColumnSpacing"/>, applied with <see cref="PlanLayoutOptions.SetColumnSpacing"/>.
    ///
    /// The gap is paid once per column, so on a deep plan it is as much of the width as the nodes
    /// are.  Tight gets a deep plan on screen, at the cost of row count labels crowding the arrows'
    /// corners; wider gaps give thick arrows and their labels room to be read.
    /// </summary>
    public enum PlanColumnSpacing
    {
        /// <summary>Just room for the arrows.  Row count labels may crowd their corners.</summary>
        Tight,

        /// <summary>The default: room for a row count label and the corner of a thick arrow.</summary>
        Normal,

        /// <summary>More room between columns, so the arrows and their labels stand apart.</summary>
        Wide,

        /// <summary>Plenty of room, for reading the arrows rather than fitting the plan.</summary>
        Widest
    }

    public static class PlanColumnSpacings
    {
        /// <summary>The clear space between columns at a preset.</summary>
        public static double Spacing(PlanColumnSpacing spacing) => spacing switch
        {
            PlanColumnSpacing.Tight => 28,
            PlanColumnSpacing.Normal => 48,
            PlanColumnSpacing.Wide => 80,
            PlanColumnSpacing.Widest => 120,
            _ => throw new ArgumentOutOfRangeException(nameof(spacing), spacing, null)
        };
    }
}
