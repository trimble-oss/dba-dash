using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DBADash.Test
{
    /// <summary>
    /// A new alert type has to be wired up in several places: its Alert.*Alert_Upd proc, a call from Alert.Alerts_Upd and a
    /// RuleTypes member in the GUI (saved to Alert.Rules.Type, which the proc filters on).  Missing one doesn't fail anywhere -
    /// the rule saves but never alerts - so check they agree.  The DBADashDB build catches a proc missing from the sqlproj.
    /// </summary>
    [TestClass]
    public class AlertRegistrationTests
    {
        private static readonly Regex TypeDeclaration = new(@"DECLARE\s+@Type\s+VARCHAR\(50\)\s*=\s*'(?<type>[^']+)'", RegexOptions.IgnoreCase);

        private static string AlertProcFolder => Path.Combine(MessageAuthorizationTests.FindRepositoryRoot(), "DBADashDB", "Alert", "Procedures");

        /// <summary>Alert proc name (without schema) => the alert type it processes.</summary>
        internal static Dictionary<string, string> ReadAlertProcs()
        {
            var procs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.GetFiles(AlertProcFolder, "*Alert_Upd.sql"))
            {
                var match = TypeDeclaration.Match(File.ReadAllText(file));
                Assert.IsTrue(match.Success, $"{Path.GetFileName(file)} doesn't declare @Type");
                procs[Path.GetFileNameWithoutExtension(file)] = match.Groups["type"].Value;
            }
            Assert.IsTrue(procs.Count > 0, $"No alert procs were found in {AlertProcFolder}");
            return procs;
        }

        private static HashSet<string> ReadGuiRuleTypes()
        {
            var path = Path.Combine(MessageAuthorizationTests.FindRepositoryRoot(), "DBADashGUI", "DBADashAlerts", "Rules", "AlertRuleBase.cs");
            var match = Regex.Match(File.ReadAllText(path), @"public enum RuleTypes\s*\{(?<members>[^}]*)\}");
            Assert.IsTrue(match.Success, $"RuleTypes enum wasn't found in {path}");
            return match.Groups["members"].Value.Split(',').Select(m => m.Trim()).Where(m => m.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void EveryAlertProcIsCalledFromAlertsUpd()
        {
            var alertsUpd = File.ReadAllText(Path.Combine(AlertProcFolder, "Alerts_Upd.sql"));
            var missing = ReadAlertProcs().Keys.Where(p => !Regex.IsMatch(alertsUpd, $@"EXEC\s+Alert\.{p}\b", RegexOptions.IgnoreCase)).ToList();
            Assert.AreEqual(0, missing.Count, "Alert procs not called from Alert.Alerts_Upd: " + string.Join(", ", missing));
        }

        [TestMethod]
        public void GuiRuleTypesMatchAlertProcs()
        {
            var guiTypes = ReadGuiRuleTypes();
            var procTypes = ReadAlertProcs().Values.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // SQL Agent alerts are evaluated as they're imported rather than by Alert.Alerts_Upd
            var importProc = Path.Combine(MessageAuthorizationTests.FindRepositoryRoot(), "DBADashDB", "dbo", "Stored Procedures", "Alerts_Upd.sql");
            var importType = TypeDeclaration.Match(File.ReadAllText(importProc));
            Assert.IsTrue(importType.Success, "dbo.Alerts_Upd doesn't declare @Type");
            procTypes.Add(importType.Groups["type"].Value);

            var noProc = guiTypes.Except(procTypes, StringComparer.OrdinalIgnoreCase).ToList();
            var noRuleType = procTypes.Except(guiTypes, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.AreEqual(0, noProc.Count, "RuleTypes with no alert proc processing them: " + string.Join(", ", noProc));
            Assert.AreEqual(0, noRuleType.Count, "Alert proc types with no RuleTypes member: " + string.Join(", ", noRuleType));
        }
    }
}
