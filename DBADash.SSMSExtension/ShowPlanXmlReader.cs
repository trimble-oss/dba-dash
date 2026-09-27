using System.Reflection;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// Reads the ShowPlan XML back out of SSMS's own graphical execution plan control.
    ///
    /// SSMS keeps the control that renders a plan (Microsoft.SqlServer.Management.UI.VSIntegration.
    /// Editors.ShowPlan.ShowPlanControl, confirmed directly against the SSMS 22 install rather than
    /// assumed from any other tool's source) around for as long as the tab is open, including a
    /// GetShowPlanXml() method that hands the XML straight back - it's just not public, so getting at
    /// it means reflection rather than a supported API. That's expected: SSMS has no supported API for
    /// this at all, for either a plan or a deadlock graph.
    /// </summary>
    internal static class ShowPlanXmlReader
    {
        public const string ShowPlanControlTypeName =
            "Microsoft.SqlServer.Management.UI.VSIntegration.Editors.ShowPlan.ShowPlanControl";

        /// <summary>
        /// Returns the plan XML from the currently focused execution plan tab, or null when nothing
        /// focused is a plan tab, or a future SSMS build has changed the control's shape.
        /// </summary>
        public static string TryRead()
        {
            var control = FocusedControlFinder.Find(ShowPlanControlTypeName);
            if (control == null) return null;

            var method = control.GetType().GetMethod(
                "GetShowPlanXml",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: System.Type.EmptyTypes,
                modifiers: null);

            return method?.Invoke(control, null) as string;
        }
    }
}
