using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace DBADash.QueryPlan.Live
{
    /// <summary>
    /// Puts a statement's text onto a live plan that arrived without one. sys.dm_exec_query_statistics_xml often leaves
    /// StatementText blank, and the plan viewer reads the query from that attribute.
    /// </summary>
    public static class PlanLiveStatementText
    {
        /// <summary>
        /// <paramref name="planXml"/> with <paramref name="statementText"/> written onto the one statement that has none.
        /// A plan that already carries its text is returned unchanged, and so is one where more than one statement is
        /// blank: the text belongs to a single statement, and there is no way to tell which of several it is.
        /// </summary>
        public static string Apply(string planXml, string statementText)
        {
            if (string.IsNullOrWhiteSpace(planXml) || string.IsNullOrWhiteSpace(statementText)) return planXml;

            XDocument document;
            try
            {
                document = XDocument.Parse(planXml);
            }
            catch (XmlException)
            {
                return planXml;
            }

            var missing = document.Descendants()
                .Where(e => e.Name.LocalName.StartsWith("Stmt", StringComparison.Ordinal))
                .Where(e => string.IsNullOrWhiteSpace(e.Attributes().FirstOrDefault(a => a.Name.LocalName == "StatementText")?.Value))
                .Take(2)
                .ToList();

            if (missing.Count != 1) return planXml;

            missing[0].SetAttributeValue("StatementText", statementText);
            return document.ToString(SaveOptions.DisableFormatting);
        }
    }
}
