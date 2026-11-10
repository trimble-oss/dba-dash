using DBADashGUI.CustomReports;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.CollectionDates
{
    /// <summary>
    /// The DBA Dash ErrorLog - errors logged by the service while collecting and importing (dbo.CollectionErrorLog).
    /// Opens on counts by instance (or by source for one instance), with the context split into what kind of error it is
    /// (error, warning, retry) and where it happened (collect, WMI, import, other) - see dbo.CollectionErrorClassify.
    /// Clicking an instance, source or count narrows the report in place, and Back goes up again.  What to do about the
    /// errors, where there's something to suggest, is a card at the top.
    /// </summary>
    internal class ErrorLogView : CustomReportView
    {
        private const string GroupByParameter = "@GroupBy";

        private readonly ToolStripButton tsAckErrors = new("Acknowledge Errors")
        {
            Alignment = ToolStripItemAlignment.Right,
            Visible = false,
            ToolTipText = "Clear the errors logged so far from the summary page - for instances on a service version that doesn't report collection runs.  Collection Health is the summary's status otherwise."
        };

        public ErrorLogView()
        {
            Report = ReportInstance;
            PreventReportOverwrite = true;
            tsAckErrors.Click += (_, _) => AcknowledgeErrors();
            ToolStrip.Items.Add(tsAckErrors);
        }

        /// <summary>Acknowledge Errors - offered at the root, with no tag filter, as before</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool AckErrors
        {
            get => tsAckErrors.Visible;
            set => tsAckErrors.Visible = value;
        }

        private static void AcknowledgeErrors()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.AcknowledgeErrors", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.ExecuteNonQuery();
            MessageBox.Show("Errors up to current date acknowledged and cleared from summary page.", "Errors acknowledged", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// From the tree, a row per instance - or per source where one instance is selected, as a row per instance would be
        /// a single row.  A drill-down sets its own.
        /// </summary>
        protected override void OnContextChanged(bool isDrillDown)
        {
            if (isDrillDown || context == null) return;
            var singleInstance = context.InstanceID > 0 || context.InstanceIDs.Count == 1;
            Report.Pickers.First(p => p.ParameterName == GroupByParameter).DefaultValue = singleInstance ? "Source" : "Instance";
        }

        /// <summary>
        /// Narrows the report in place.  The filters already set are kept, and the click adds to them.  <paramref name="groupBy"/>
        /// is the view it goes to - None for the errors.
        /// </summary>
        private static SystemDrillDownLinkColumnInfo Drill(string groupBy, Dictionary<string, string> columns, string errorType = null, string errorStage = null)
        {
            var values = new Dictionary<string, object>();
            if (groupBy != null) values[GroupByParameter] = groupBy;
            if (errorType != null) values["@ErrorType"] = errorType;
            if (errorStage != null) values["@ErrorStage"] = errorStage;
            return new SystemDrillDownLinkColumnInfo
            {
                ReportFactory = () => ReportInstance,
                DrillDownMode = DrillDownMode.ExistingWindow,
                CarryUserParameters = true,
                CarryDrillDownDates = true, // Last Failure opens on the failure's dates - narrowing it keeps them
                ParameterValues = values,
                ColumnToParameterMap = columns
            };
        }

        /// <summary>A count: the errors it counts - for the instance or source on the row (whichever the rows are per)</summary>
        private static SystemDrillDownLinkColumnInfo CountLink(string errorType = null, string errorStage = null) =>
            Drill("None", new Dictionary<string, string>
            {
                ["@InstanceDisplayName"] = "InstanceDisplayName",
                ["@ErrorSource"] = "ErrorSource"
            }, errorType, errorStage);

        private static ColumnMetadata Count(string alias, string description, string errorType = null, string errorStage = null, CellHighlightingRuleSet highlighting = null) => new()
        {
            Alias = alias,
            FormatString = "N0",
            Description = description + "  Click to list them.",
            Link = CountLink(errorType, errorStage),
            Highlighting = highlighting
        };

        /// <summary>The count's status colour when it's more than 0</summary>
        private static CellHighlightingRuleSet CountHighlight(string column, DBADashStatusEnum status) => new(column)
        {
            Rules = new List<CellHighlightingRule>
            {
                new() { ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = "0", Status = status }
            }
        };

        private static CellHighlightingRuleSet TypeHighlight() => new("ErrorType")
        {
            Rules = new List<CellHighlightingRule>
            {
                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "Error", Status = DBADashStatusEnum.Critical },
                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "Warning", Status = DBADashStatusEnum.Warning }
            }
        };

        private static ColumnMetadata Hidden() => new() { Visible = false };

        /// <summary>
        /// The list and the two summaries return different columns.  Without this, a link column the result doesn't have
        /// is added anyway, as an action link showing its name.  The columns are left in the order the procedure returns
        /// them - a fixed display order would count the columns the result doesn't have.
        /// </summary>
        private static CustomReportResult LinksNeedTheirColumns(CustomReportResult result)
        {
            foreach (var column in result.Columns.Values.Where(c => c.Link != null))
            {
                column.RequiresDataColumn = true;
            }
            return result;
        }

        private const string TypeDescription = "Error, Warning (the collection carried on) or Retry (from an attempt that was retried - the collection carried on).";
        private const string StageDescription = "Collect (from the instance), WMI (collecting through WMI), Import (into the repository) or Other (the service's own work - alert processing, maintenance).";

        /// <summary>The errors for a collection (and instance) over the date range - for the Collection Health report's error counts</summary>
        public static SystemDrillDownLinkColumnInfo ErrorsFor(bool includeRetries) => new()
        {
            ReportFactory = () => ReportInstance,
            DrillDownMode = DrillDownMode.NewWindow,
            ParameterValues = new Dictionary<string, object>
            {
                [GroupByParameter] = "None",
                ["@IncludeRetries"] = includeRetries
            },
            ColumnToParameterMap = new Dictionary<string, string>
            {
                ["@ErrorSource"] = "Reference",
                ["@InstanceDisplayName"] = "InstanceDisplayName"
            }
        };

        /// <summary>
        /// The errors for a collection's last failure - for the Collection Health report's Last Failure.  The row's ErrorsFrom and
        /// ErrorsTo set the dates, so they're found whatever the report's period: a weekly collection that failed at the weekend
        /// has nothing in the last hour.
        /// </summary>
        public static SystemDrillDownLinkColumnInfo LastFailureErrors()
        {
            var link = ErrorsFor(includeRetries: false);
            link.ColumnToParameterMap["@FromDate"] = "ErrorsFrom";
            link.ColumnToParameterMap["@ToDate"] = "ErrorsTo";
            return link;
        }

        public static SystemReport ReportInstance => new()
        {
            ViewType = typeof(ErrorLogView),
            ReportName = "DBA Dash ErrorLog",
            Description = "Errors the DBA Dash service logged collecting from your instances and importing into the repository.  Click an instance, source or count to narrow it down, and Back to go up again.",
            SchemaName = "dbo",
            ProcedureName = "CollectionErrorLog_Get",
            QualifiedProcedureName = "dbo.CollectionErrorLog_Get",
            CanEditReport = false,
            ShowInsights = true, // What to do about the errors (dbo.CollectionErrorInsight)
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = LinksNeedTheirColumns(new CustomReportResult
                {
                    ResultName = "Errors",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["Instance"] = Hidden(),
                        ["InstanceID"] = Hidden(),
                        ["InstanceDisplayName"] = new()
                        {
                            Alias = "Instance",
                            Description = "Click to narrow to the instance.  \"(No instance)\" is the service's own work - alert processing, maintenance.",
                            Link = Drill(null, new Dictionary<string, string>
                            {
                                ["@InstanceDisplayName"] = "InstanceDisplayName",
                                [GroupByParameter] = "DrillGroupBy"
                            })
                        },
                        ["ErrorSource"] = new()
                        {
                            Alias = "Source",
                            Description = "The collection (or part of the service) that logged the error.  Click to list its errors.",
                            Link = Drill("None", new Dictionary<string, string> { ["@ErrorSource"] = "ErrorSource" })
                        },
                        ["ErrorDate"] = new() { Alias = "Date" },
                        ["ErrorContext"] = new()
                        {
                            Alias = "Context",
                            Description = "Where the error happened, as the service logged it.  Click to list the errors with this context.",
                            Link = Drill("None", new Dictionary<string, string> { ["@ErrorContext"] = "ErrorContext" })
                        },
                        ["ErrorType"] = new()
                        {
                            Alias = "Type",
                            Description = TypeDescription + "  Click to list this type.",
                            Highlighting = TypeHighlight(),
                            Link = Drill("None", new Dictionary<string, string> { ["@ErrorType"] = "ErrorType" })
                        },
                        ["ErrorStage"] = new()
                        {
                            Alias = "Stage",
                            Description = StageDescription + "  Click to list this stage.",
                            Link = Drill("None", new Dictionary<string, string> { ["@ErrorStage"] = "ErrorStage" })
                        },
                        ["ErrorMessage"] = new() { Alias = "Message", Link = new TextLinkColumnInfo { TargetColumn = "ErrorMessage" } },
                        // A row per instance only - the card at the top says it for the report as a whole
                        ["Suggestion"] = new()
                        {
                            Alias = "Suggestion",
                            Description = "What to do about the instance's errors, where there's something to suggest (dbo.CollectionErrorSuggestion) - WMI failing for 2 or more collections, say.  Click for help.",
                            Link = new UrlLinkColumnInfo { TargetColumn = "HelpUrl" },
                            Highlighting = new CellHighlightingRuleSet("Suggestion")
                            {
                                Rules = new List<CellHighlightingRule>
                                {
                                    new() { ConditionType = CellHighlightingRule.ConditionTypes.IsNotNull, Status = DBADashStatusEnum.Warning }
                                }
                            }
                        },
                        ["HelpUrl"] = Hidden(),
                        ["SourceCount"] = new()
                        {
                            Alias = "Sources",
                            FormatString = "N0",
                            Description = "How many sources logged errors for the instance.  Click to see them.",
                            Link = Drill("Source", new Dictionary<string, string> { ["@InstanceDisplayName"] = "InstanceDisplayName" })
                        },
                        ["InstanceCount"] = new()
                        {
                            Alias = "Instances",
                            FormatString = "N0",
                            Description = "How many instances the source logged errors for.  Click to see them.",
                            Link = Drill("Instance", new Dictionary<string, string> { ["@ErrorSource"] = "ErrorSource" })
                        },
                        ["ErrorCount"] = Count("Total", "All the errors.  The type columns add up to this, and so do the stage columns."),
                        ["Errors"] = Count("Errors", "Errors that weren't a warning or a retry.", errorType: "Error", highlighting: CountHighlight("Errors", DBADashStatusEnum.Critical)),
                        ["Warnings"] = Count("Warnings", "Warnings - [Warning] in the context, or a message that starts with \"Warning\".  The collection carried on.", errorType: "Warning", highlighting: CountHighlight("Warnings", DBADashStatusEnum.Warning)),
                        ["Retries"] = Count("Retries", "Errors from attempts that were retried ([Retry] in the context).  The collection carried on - it failed only if there's an error too.", errorType: "Retry"),
                        ["Collect"] = Count("Collect", "Collecting from the instance, except through WMI.", errorStage: "Collect"),
                        ["WMI"] = Count("WMI", "Collecting through WMI.", errorStage: "WMI"),
                        ["Import"] = Count("Import", "Importing into the repository.", errorStage: "Import"),
                        ["Other"] = Count("Other", "The service's own work - alert processing, maintenance, custom SQL alerts.", errorStage: "Other"),
                        ["FirstError"] = new() { Alias = "First Error" },
                        ["LastError"] = new() { Alias = "Last Error" },
                        ["LastMessage"] = new() { Alias = "Last Message", Description = "The latest error's message (the first 1000 characters).  Click to see it all.", Link = new TextLinkColumnInfo { TargetColumn = "LastMessageFull" } },
                        ["LastMessageFull"] = Hidden(),
                        ["DrillGroupBy"] = Hidden()
                    }
                })
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@InstanceID", ParamType = "INT" },
                    new() { ParamName = "@FromDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@ToDate", ParamType = "DATETIME2" },
                    new() { ParamName = GroupByParameter, ParamType = "VARCHAR" },
                    new() { ParamName = "@IncludeRetries", ParamType = "BIT" },
                    new() { ParamName = "@ErrorType", ParamType = "VARCHAR" },
                    new() { ParamName = "@ErrorStage", ParamType = "VARCHAR" },
                    new() { ParamName = "@InstanceDisplayName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@ErrorSource", ParamType = "VARCHAR" },
                    new() { ParamName = "@ErrorContext", ParamType = "VARCHAR" },
                    new() { ParamName = "@ErrorMessage", ParamType = "VARCHAR" }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = GroupByParameter,
                    Name = "View",
                    Description = "Row per instance: the errors for each instance, split by type and by where they happened.\n" +
                                  "Row per source: the same for each collection (or part of the service) that logged errors.\n" +
                                  "Row per error: every error.",
                    PickerItems = new Dictionary<object, string>
                    {
                        ["Instance"] = "Row per instance",
                        ["Source"] = "Row per source",
                        ["None"] = "Row per error"
                    },
                    DefaultValue = "Instance",
                    MenuBar = true
                },
                new()
                {
                    ParameterName = "@IncludeRetries",
                    Name = "Retries",
                    Description = "Errors from attempts that were retried.  The collection carried on, so they only matter if a collection retries often.",
                    PickerItems = new Dictionary<object, string>
                    {
                        [true] = "Shown",
                        [false] = "Hidden"
                    },
                    DefaultValue = true,
                    MenuBar = true,
                    DataType = typeof(bool)
                },
                new()
                {
                    ParameterName = "@ErrorType",
                    Name = "Type",
                    Description = TypeDescription,
                    PickerItems = new Dictionary<object, string>
                    {
                        [""] = "All",
                        ["Error"] = "Error",
                        ["Warning"] = "Warning",
                        ["Retry"] = "Retry"
                    },
                    DefaultValue = "", // All - and the picker is bold once a type is picked
                    MenuBar = true
                },
                new()
                {
                    ParameterName = "@ErrorStage",
                    Name = "Stage",
                    Description = StageDescription,
                    PickerItems = new Dictionary<object, string>
                    {
                        [""] = "All",
                        ["Collect"] = "Collect",
                        ["WMI"] = "WMI",
                        ["Import"] = "Import",
                        ["Other"] = "Other"
                    },
                    DefaultValue = "",
                    MenuBar = true
                },
                new() { ParameterName = "@InstanceDisplayName", Name = "Instance", IsText = true, MenuBar = true, Description = "The instance's name, exactly.  (No instance) for the service's own work." },
                new() { ParameterName = "@ErrorSource", Name = "Source", IsText = true, MenuBar = true, Description = "The source, exactly - a collection such as Drives, or Alert" },
                new() { ParameterName = "@ErrorContext", Name = "Context", IsText = true, Description = "The context, exactly - Collect:WMI, say" },
                new() { ParameterName = "@ErrorMessage", Name = "Message", IsText = true, Description = "Part of the message" }
            }
        };
    }
}
