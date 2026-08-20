using DBADash;
using DBADash.XE;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    [TestClass]
    public class XESessionFilterTests
    {
        // ---- blank / empty ----------------------------------------------------------------------

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow(" , , ")]
        public void Blank_allows_nothing(string pattern)
        {
            var f = XESessionFilter.Parse(pattern);
            Assert.IsTrue(f.IsEmpty);
            Assert.IsFalse(f.IsAllowed("system_health"));
            Assert.IsFalse(f.IsAllowed("anything"));
        }

        // ---- star = all -------------------------------------------------------------------------

        [TestMethod]
        public void Star_allows_everything()
        {
            var f = XESessionFilter.Parse("*");
            Assert.IsFalse(f.IsEmpty);
            Assert.IsTrue(f.IsAllowed("system_health"));
            Assert.IsTrue(f.IsAllowed("MyCustomSession"));
        }

        // ---- exact allow list -------------------------------------------------------------------

        [TestMethod]
        public void Exact_tokens_allow_only_listed()
        {
            var f = XESessionFilter.Parse("SessionA, SessionB");
            Assert.IsTrue(f.IsAllowed("SessionA"));
            Assert.IsTrue(f.IsAllowed("SessionB"));
            Assert.IsFalse(f.IsAllowed("SessionC"));
        }

        [TestMethod]
        public void Matching_is_case_insensitive_and_trims_whitespace()
        {
            var f = XESessionFilter.Parse("  system_health  ");
            Assert.IsTrue(f.IsAllowed("SYSTEM_HEALTH"));
        }

        // ---- deny always wins -------------------------------------------------------------------

        [TestMethod]
        [DataRow("*,-system_health")]
        [DataRow("-system_health,*")] // order must not matter - deny wins regardless
        public void Deny_beats_allow_regardless_of_order(string pattern)
        {
            var f = XESessionFilter.Parse(pattern);
            Assert.IsFalse(f.IsAllowed("system_health"));
            Assert.IsTrue(f.IsAllowed("AnotherSession"));
        }

        [TestMethod]
        public void Deny_without_matching_allow_still_blocks()
        {
            // system_health is denied and never allowed; the explicit allow only covers SessionA.
            var f = XESessionFilter.Parse("SessionA,-system_health");
            Assert.IsTrue(f.IsAllowed("SessionA"));
            Assert.IsFalse(f.IsAllowed("system_health"));
            Assert.IsFalse(f.IsAllowed("SessionB"));
        }

        [TestMethod]
        public void Deny_only_allows_nothing()
        {
            var f = XESessionFilter.Parse("-system_health");
            Assert.IsFalse(f.IsEmpty);
            Assert.IsFalse(f.IsAllowed("system_health"));
            Assert.IsFalse(f.IsAllowed("SessionA")); // no allow token means nothing is permitted
        }

        // ---- glob wildcards ---------------------------------------------------------------------

        [TestMethod]
        public void Glob_star_protects_a_family_of_sessions()
        {
            var f = XESessionFilter.Parse("*,-system_*");
            Assert.IsFalse(f.IsAllowed("system_health"));
            Assert.IsFalse(f.IsAllowed("system_anything"));
            Assert.IsTrue(f.IsAllowed("MyCustomSession"));
        }

        [TestMethod]
        public void Glob_star_can_allow_a_prefix()
        {
            var f = XESessionFilter.Parse("DBADash_*");
            Assert.IsTrue(f.IsAllowed("DBADash_AdHoc"));
            Assert.IsTrue(f.IsAllowed("DBADash_Something"));
            Assert.IsFalse(f.IsAllowed("OtherSession"));
        }

        [TestMethod]
        public void Glob_question_mark_matches_single_char()
        {
            var f = XESessionFilter.Parse("Session?");
            Assert.IsTrue(f.IsAllowed("SessionA"));
            Assert.IsFalse(f.IsAllowed("SessionAB"));
        }

        [TestMethod]
        public void Lone_dash_token_is_ignored()
        {
            var f = XESessionFilter.Parse("*, - ");
            Assert.IsTrue(f.IsAllowed("anything"));
        }

        // ---- CollectionConfig integration -------------------------------------------------------

        [TestMethod]
        public void Config_recommended_default_protects_health_sessions_only()
        {
            var cfg = new CollectionConfig
            {
                ManageXESessions = CollectionConfig.DefaultManageXESessions,
                WatchXESessions = CollectionConfig.DefaultWatchXESessions
            };
            Assert.IsTrue(cfg.AllowManageXE);
            Assert.IsFalse(cfg.CanManageXESession("system_health"));
            Assert.IsFalse(cfg.CanManageXESession("AlwaysOn_health"));
            Assert.IsFalse(cfg.CanManageXESession("telemetry_xevents"));
            Assert.IsTrue(cfg.CanManageXESession("MyTrace"));
            // Watch default allows everything, including the protected-from-stop health sessions.
            Assert.IsTrue(cfg.CanWatchXESession("system_health"));
            Assert.IsTrue(cfg.CanWatchXESession("MyTrace"));
        }

        [TestMethod]
        public void Config_watch_only_disables_start_stop_but_keeps_feature_on()
        {
            var cfg = new CollectionConfig { ManageXESessions = "", WatchXESessions = "*" };
            Assert.IsTrue(cfg.AllowManageXE); // feature on -> list/script/watch offered
            Assert.IsFalse(cfg.CanManageXESession("MyTrace")); // but no start/stop
            Assert.IsTrue(cfg.CanWatchXESession("MyTrace"));
        }

        [TestMethod]
        public void Config_both_blank_disables_the_feature()
        {
            var cfg = new CollectionConfig { ManageXESessions = null, WatchXESessions = null };
            Assert.IsFalse(cfg.AllowManageXE);
        }
    }
}
