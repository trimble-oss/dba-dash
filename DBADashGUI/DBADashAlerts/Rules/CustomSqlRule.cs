using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;

namespace DBADashGUI.DBADashAlerts.Rules
{
    /// <summary>
    /// A custom alert rule backed by a user-authored stored procedure in the UserAlert schema.
    /// The rule is deliberately thin: the proc owns the criteria, the instances (it returns InstanceID)
    /// and the message; the rule supplies Priority, notification GroupID, enable/disable and optional
    /// tag/instance scoping.  See Alert.CustomSqlAlert_Upd and Alert.CustomSqlProcs_Get.
    /// </summary>
    internal class CustomSqlRule : AlertRuleBase
    {
        public override RuleTypes RuleType => RuleTypes.CustomSql;

        [Description("The stored procedure in the UserAlert schema that defines this alert. " +
                     "It must return exactly three columns: InstanceID (int), AlertKey (nvarchar) and AlertMessage (nvarchar). " +
                     "Procs are created by a db_ddladmin/db_owner and only reference the repository database.")]
        [DisplayName("Procedure"), Category("Custom SQL")]
        [Editor(typeof(CustomSqlProcSelect), typeof(UITypeEditor))]
        public string ProcName { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        [Browsable(false)]
        public override string AlertKey => ProcName ?? string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        [Browsable(false)]
        public override decimal? Threshold => null;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        [Browsable(false)]
        public override int? EvaluationPeriodMins => null;

        public override (bool isValid, string message) Validate()
        {
            if (string.IsNullOrWhiteSpace(ProcName))
            {
                return (false, "A UserAlert procedure must be selected");
            }

            CustomSqlProc proc;
            try
            {
                proc = CustomSqlProc.Cached.FirstOrDefault(p => string.Equals(p.ProcName, ProcName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                return (false, $"Unable to validate UserAlert procedures: {ex.Message}");
            }

            if (proc == null)
            {
                return (false, $"Procedure UserAlert.{ProcName} was not found. It must exist in the UserAlert schema.");
            }
            if (!proc.IsValidSchema)
            {
                return (false, $"Procedure UserAlert.{ProcName} does not return the expected result set " +
                               "(exactly: InstanceID int, AlertKey nvarchar, AlertMessage nvarchar). " +
                               "Fix the procedure or verify it returns a single, describable result set.");
            }
            return (true, string.Empty);
        }
    }
}
