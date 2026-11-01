#nullable enable
using DBADash.Deadlock;
using DBADash.Deadlock.Analysis;
using DBADash.Deadlock.Model;
using DBADash.QueryPlan;
using DBADash.QueryPlan.Model;
using DBADashAI.Mcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.AI
{
    /// <summary>
    /// The built-in AI panels' masking: two options, on by default and for the request in hand, and the plan or deadlock
    /// masked before its payload is built - so the preview, the parameter list and the findings show what goes, not what
    /// the plan held.  The AI service refuses a request masked less than its own setting, which a db_owner sets in
    /// Options > Connect AI Tools.
    /// </summary>
    internal static class AiMasking
    {
        public const string ParametersText = "Mask parameter values";
        public const string LiteralsText = "Mask literals";
        public const string MetadataText = "Metadata only";

        public const string ParametersTip = "Replace the values parameters were compiled for and ran with by placeholders such as (value A) - " +
                                            "the same placeholder for the same value, so a plan compiled for one value and run with another still says so - " +
                                            "and the values passed to procedures and sp_executesql in statement text by ?.";

        public const string LiteralsTip = "Replace the literals in statement text, plans and object definitions with ?.  An application that " +
                                          "passes its data as parameters may only have its own constants here.";

        public const string MetadataTip = "Withhold statement text, input buffers, parameter values and object definitions altogether, and " +
                                          "replace logins, hosts and application names by stable codes such as login#3fa9c1d2, host#... and app#....  A plan keeps its operators, " +
                                          "estimates and costs; a deadlock its locks, resources and wait order.";

        /// <summary>
        /// The masking options for an Options menu: parameter values, literals and metadata only, the levels Options >
        /// Connect AI Tools offers.  Metadata only covers the other two, which show ticked and can't be changed while it is
        /// on.  <paramref name="changed"/> runs when any is clicked.
        /// </summary>
        public static MaskToggles Toggles(Action changed, AiDataMask? start = null)
        {
            if (start is not null) return new MaskToggles(changed, start.Value);

            // The built-in AI panels: a plan or deadlock viewer, or the performance analysis window, has options of its
            // own, and each starts where the last change in any of them left off - until a restart, which starts masked.
            MaskToggles toggles = null!;
            toggles = new MaskToggles(() =>
            {
                _builtInChoice = toggles.Mask;
                changed();
            }, _builtInChoice);
            return toggles;
        }

        /// <summary>The last choice in a built-in AI panel's options.  UI thread only.</summary>
        private static AiDataMask _builtInChoice = AiDataMask.Default;

        internal sealed class MaskToggles
        {
            public ToolStripMenuItem Parameters { get; }
            public ToolStripMenuItem Literals { get; }
            public ToolStripMenuItem Metadata { get; }

            public MaskToggles(Action changed, AiDataMask start)
            {
                Parameters = Item(ParametersText, ParametersTip, start.Parameters, changed);
                Literals = Item(LiteralsText, LiteralsTip, start.Literals, changed);
                Metadata = Item(MetadataText, MetadataTip, start.MetadataOnly, () =>
                {
                    ShowMetadata();
                    changed();
                });
                ShowMetadata();
            }

            public ToolStripItem[] Items => [Parameters, Literals, Metadata];

            /// <summary>What is ticked.</summary>
            public AiDataMask Mask => Metadata.Checked ? AiDataMask.Metadata : new AiDataMask(Parameters.Checked, Literals.Checked);

            /// <summary>Metadata only implies the other two: they show ticked and can't be changed, and come back as they were.</summary>
            private bool _parametersBefore;
            private bool _literalsBefore;

            private void ShowMetadata()
            {
                var on = Metadata.Checked;
                if (on && Parameters.Enabled)
                {
                    (_parametersBefore, _literalsBefore) = (Parameters.Checked, Literals.Checked);
                    Parameters.Checked = Literals.Checked = true;
                }
                else if (!on && !Parameters.Enabled)
                {
                    (Parameters.Checked, Literals.Checked) = (_parametersBefore, _literalsBefore);
                }

                Parameters.Enabled = Literals.Enabled = !on;
            }

            private static ToolStripMenuItem Item(string text, string toolTip, bool on, Action clicked) =>
                new(text, null, (_, _) => clicked()) { CheckOnClick = true, Checked = on, ToolTipText = toolTip + "  For this request only." };
        }

        /// <summary>The statement, re-parsed from its own XML masked - everything the payload works out from it is then masked too.</summary>
        public static (ExecutionPlan Plan, PlanStatement Statement) MaskPlan(ExecutionPlan plan, PlanStatement statement, AiDataMask mask)
        {
            if (mask.IsNone)
            {
                return (plan, statement);
            }

            var masked = PlanParser.Parse(AiDataMasker.MaskXml(PlanParser.ExtractStatementXml(null, statement), mask));
            return (masked, masked.Statements[0]);
        }

        /// <summary>The graph, re-parsed from its XML masked.</summary>
        public static DeadlockGraph MaskGraph(DeadlockGraph graph, AiDataMask mask) =>
            mask.IsNone ? graph : DeadlockParser.Parse(AiDataMasker.MaskXml(graph.Xml, mask))[0];

        public static IReadOnlyList<DeadlockObjectDefinition> MaskDefinitions(IReadOnlyList<DeadlockObjectDefinition> definitions, AiDataMask mask) =>
            mask.IsNone
                ? definitions
                : definitions.Select(d => new DeadlockObjectDefinition
                {
                    Name = d.Name,
                    Database = d.Database,
                    ObjectType = d.ObjectType,
                    AsAt = d.AsAt,
                    Ddl = AiDataMasker.MaskSql(d.Ddl, mask),
                    Truncated = d.Truncated
                }).ToList();

        /// <summary>True to go ahead: masking is on, or the user said yes to sending without it.</summary>
        public static bool ConfirmUnmasked(AiDataMask mask, IWin32Window? owner) =>
            !mask.IsNone ||
            MessageBox.Show(owner,
                "Masking is off for this request: statement text and plans go to the AI provider with their parameter values and literals.  Continue?",
                "AI analysis", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }
}
