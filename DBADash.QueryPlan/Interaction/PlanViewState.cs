using DBADash.QueryPlan.Layout;

namespace DBADash.QueryPlan.Interaction
{
    /// <summary>
    /// A snapshot of where a <see cref="PlanViewController"/> is looking, so a temporary change can
    /// be undone exactly - including whether the view was still the fitted one, which decides whether
    /// resizing the window goes on re-fitting it.
    /// </summary>
    public readonly record struct PlanViewState(double Zoom, LayoutPoint Pan, bool IsUserAdjusted);
}
