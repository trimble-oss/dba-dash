using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace DBADash.QueryPlan.Live
{
    /// <summary>
    /// Records that a plan was captured while the query was still running. The statistics XML itself has nothing that
    /// says so — once saved it is an ordinary showplan — so a comment is written into it. Comments are not part of
    /// the showplan schema, which is what lets SSMS open the file, and they survive the counter refresh because that
    /// rewrites the same document.
    /// </summary>
    public static class PlanLiveMark
    {
        /// <summary>The text a saved live plan carries, so opening the file again can still tell what it was.</summary>
        public const string Token = "DBADashLivePlan";

        private const string CommentText = " DBADashLivePlan. Waits are from the first capture; operator counters may be from a later refresh. ";

        /// <summary><paramref name="planXml"/> with the live-plan comment on its ShowPlanXML element, or unchanged when it already has one.</summary>
        public static string Apply(string planXml)
        {
            if (string.IsNullOrWhiteSpace(planXml) || IsMarked(planXml)) return planXml;

            XDocument document;
            try
            {
                document = XDocument.Parse(planXml);
            }
            catch (XmlException)
            {
                return planXml;
            }

            var showPlan = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "ShowPlanXML");
            if (showPlan is null) return planXml;

            showPlan.AddFirst(new XComment(CommentText));
            return document.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>True when <paramref name="planXml"/> carries the live-plan comment. The token in statement text does not count.</summary>
        public static bool IsMarked(string planXml)
        {
            if (string.IsNullOrWhiteSpace(planXml) || !planXml.Contains(Token, StringComparison.Ordinal)) return false;

            try
            {
                return XDocument.Parse(planXml).DescendantNodes().OfType<XComment>()
                    .Any(comment => comment.Value.Contains(Token, StringComparison.Ordinal));
            }
            catch (XmlException)
            {
                return false;
            }
        }
    }
}
