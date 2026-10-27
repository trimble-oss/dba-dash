using DBADash;
using DBADashGUI.Charts;
using DBADashGUI.CustomReports;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Checks
{
    /// <summary>
    /// Service Broker monitoring - instances, databases (including conversations), queues and the transmission queue with their status, plus
    /// charts of queue depth, activity and open conversations over the selected date range.  On a database node the activity chart shows
    /// activations for the database as the message counters are instance wide.
    ///
    /// <para>The collection is off by default, so at instance level the tab is only shown where it's enabled or has data
    /// (see <see cref="DBADashContext.HasServiceBroker"/>).  Thresholds are configured at root, instance, database or queue level.</para>
    /// </summary>
    internal class ServiceBrokerView : CustomReportView
    {
        private const int DatabasesTableIndex = 1;
        private const int QueuesTableIndex = 2;
        private const int TransmissionQueueTableIndex = 3;
        private const int QueueDepthTableIndex = 4;
        private const int ActivityTableIndex = 5;
        private const int ConversationsTableIndex = 6;

        private ToolStripDropDownButton tsConfigure;
        private ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private ToolStripMenuItem configureDatabaseThresholdsToolStripMenuItem;

        public ServiceBrokerView()
        {
            Report = ReportInstance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
        }

        #region Toolbar

        private void AddToolbarButtons()
        {
            tsConfigure = new ToolStripDropDownButton("Configure")
            {
                Name = "tsConfigure",
                ToolTipText = "Configure Service Broker thresholds",
                Image = Properties.Resources.SettingsOutline_16x,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var configureRootThresholdsToolStripMenuItem = new ToolStripMenuItem("Configure Root Thresholds");
            configureRootThresholdsToolStripMenuItem.Click += (_, _) => ConfigureThresholds(-1, -1);

            configureInstanceThresholdsToolStripMenuItem = new ToolStripMenuItem("Configure Instance Thresholds");
            configureInstanceThresholdsToolStripMenuItem.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id))
                    ConfigureThresholds(id, -1);
            };

            configureDatabaseThresholdsToolStripMenuItem = new ToolStripMenuItem("Configure Database Thresholds");
            configureDatabaseThresholdsToolStripMenuItem.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id) && context.DatabaseID > 0)
                    ConfigureThresholds(id, context.DatabaseID);
            };

            tsConfigure.DropDownItems.AddRange(new ToolStripItem[]
            {
                configureRootThresholdsToolStripMenuItem,
                configureInstanceThresholdsToolStripMenuItem,
                configureDatabaseThresholdsToolStripMenuItem
            });

            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfigure);
        }

        private void UpdateToolbarState()
        {
            configureInstanceThresholdsToolStripMenuItem.Enabled = TryGetSingleInstanceId(out _);
            configureDatabaseThresholdsToolStripMenuItem.Enabled = TryGetSingleInstanceId(out _) && context?.DatabaseID > 0;
        }

        private bool TryGetSingleInstanceId(out int instanceId)
        {
            instanceId = -1;
            if (context == null) return false;
            if (context.InstanceID > 0)
            {
                instanceId = context.InstanceID;
                return true;
            }
            if (context.InstanceIDs.Count == 1)
            {
                instanceId = context.InstanceIDs.First();
                return true;
            }
            return false;
        }

        #endregion Toolbar

        #region Configuration

        public void ConfigureThresholds(int instanceID, int databaseID, string schemaName = "", string queueName = "")
        {
            using var frm = new ServiceBrokerThresholdConfig { InstanceID = instanceID, DatabaseID = databaseID, SchemaName = schemaName, QueueName = queueName };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        #endregion Configuration

        protected override void OnPostGridRefresh()
        {
            base.OnPostGridRefresh();
            UpdateToolbarState();
        }

        #region Custom links

        /// <summary>Edit thresholds for the queue or database the row is for.</summary>
        private sealed class EditThresholdLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not ServiceBrokerView view) return;
                var instanceId = (int)row.Cells["InstanceID"].Value;
                var databaseId = (int)row.Cells["DatabaseID"].Value;
                if (selectedTableIndex == QueuesTableIndex)
                {
                    // Queue names are only unique within a schema
                    view.ConfigureThresholds(instanceId, databaseId, (string)row.Cells["schema_name"].Value, (string)row.Cells["queue_name"].Value);
                }
                else
                {
                    view.ConfigureThresholds(instanceId, databaseId);
                }
            }
        }

        #endregion Custom links

        #region Report definition

        private static ColumnMetadata Hidden() => new() { Visible = false };

        private static CellHighlightingRuleSet Status(string statusColumn) => new(statusColumn) { IsStatusColumn = true };

        public static SystemReport ReportInstance => new()
        {
            ViewType = typeof(ServiceBrokerView),
            ReportName = "Service Broker",
            Description = "Service Broker queues, transmission queue, conversations and message activity.",
            SchemaName = "dbo",
            ProcedureName = "ServiceBrokerReport_Get",
            QualifiedProcedureName = "dbo.ServiceBrokerReport_Get",
            CanEditReport = false,
            ShowStatusFilter = true,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.ServiceBroker.ToString()
            },
            ChartVisible = true,
            TableVisible = true,
            ChartLayoutColumns = 3,
            Charts = new List<CustomReportChart>
            {
                new()
                {
                    TableIndex = QueueDepthTableIndex,
                    Title = "Queue Depth",
                    BindXAxisToDateRange = true,
                    Config = new ChartConfiguration
                    {
                        ChartType = ChartTypes.Line,
                        ChartTitle = "Queue Depth",
                        XColumn = "SnapshotDate",
                        MetricColumn = "MessageCount",
                        SeriesColumn = "Queue",
                        YAxisFormat = "N0",
                        YAxisMin = 0
                    }
                },
                new()
                {
                    TableIndex = ActivityTableIndex,
                    Title = "Activity",
                    BindXAxisToDateRange = true,
                    Config = new ChartConfiguration
                    {
                        ChartType = ChartTypes.Line,
                        ChartTitle = "Activity (per sec)",
                        XColumn = "SnapshotDate",
                        MetricColumn = "ValuePerSec",
                        SeriesColumn = "Measure",
                        YAxisFormat = "0.##",
                        YAxisMin = 0
                    }
                },
                new()
                {
                    TableIndex = ConversationsTableIndex,
                    Title = "Open Conversations",
                    BindXAxisToDateRange = true,
                    Config = new ChartConfiguration
                    {
                        ChartType = ChartTypes.Line,
                        ChartTitle = "Open Conversations",
                        XColumn = "SnapshotDate",
                        MetricColumn = "ConversationCount",
                        SeriesColumn = "DB",
                        YAxisFormat = "N0",
                        YAxisMin = 0
                    }
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Instances",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["ServiceBrokerStatus"] = Hidden(),
                        ["Issues"] = new()
                        {
                            Description = "What the status is based on: the number of queues and databases with a critical or warning status (see their grids for the details), then instance level issues - the Service Broker endpoint and corrupted messages in the last hour.",
                            Highlighting = Status("ServiceBrokerStatus")
                        },
                        ["queue_count"] = new() { Alias = "Queues", FormatString = "N0", Description = "Number of user queues" },
                        ["queue_message_count"] = new() { Alias = "Queue Messages", FormatString = "N0", Description = "Messages waiting in user queues" },
                        ["transmission_queue_count"] = new() { Alias = "Transmission Queue", FormatString = "N0", Description = "Messages waiting to be sent" },
                        ["conversation_count"] = new() { Alias = "Open Conversations", FormatString = "N0", Description = "Conversation endpoints that are not closed" },
                        ["sends_1hr"] = new() { Alias = "SENDs (1hr)", FormatString = "N0", Description = "SEND statements in the hour before the last collection" },
                        ["receives_1hr"] = new() { Alias = "RECEIVEs (1hr)", FormatString = "N0", Description = "RECEIVE statements in the hour before the last collection" },
                        ["enqueued_messages_1hr"] = new() { Alias = "Enqueued (1hr)", FormatString = "N0", Description = "Messages added to queues in the hour before the last collection" },
                        ["activation_errors_1hr"] = new() { Alias = "Activation Errors (1hr)", FormatString = "N0", Description = "Activation procedures that ended with an error in the hour before the last collection.  Highlighted against the database in the Databases grid." },
                        ["dropped_messages_1hr"] = new() { Alias = "Dropped Messages (1hr)", FormatString = "N0", Description = "Messages dropped on arrival in the hour before the last collection.  This includes duplicate & out of order messages resent by remote brokers as part of normal retries, so it isn't highlighted.  A message that can't be delivered stays in the sender's transmission queue." },
                        ["corrupted_messages_1hr"] = new() { Alias = "Corrupted Messages (1hr)", FormatString = "N0", Highlighting = Status("ErrorStatus") },
                        ["transaction_rollbacks_1hr"] = new() { Alias = "Broker Rollbacks (1hr)", FormatString = "N0", Description = "Rolled back transactions that contained Service Broker statements", Visible = false },
                        ["ErrorStatus"] = Hidden(),
                        ["endpoint_name"] = new() { Alias = "Endpoint", Description = "Service Broker endpoint.  Only needed for messaging between instances." },
                        ["endpoint_state"] = new() { Alias = "Endpoint State", Highlighting = Status("EndpointStatus") },
                        ["endpoint_port"] = new() { Alias = "Port", Visible = false },
                        ["connection_count"] = new() { Alias = "Connections", FormatString = "N0", Visible = false },
                        ["connected_count"] = new() { Alias = "Connected", FormatString = "N0", Visible = false },
                        ["login_failed_count"] = new() { Alias = "Failed Logins", FormatString = "N0", Description = "Service Broker endpoint connections that failed to log in", Highlighting = Status("EndpointStatus") },
                        ["last_activity_time"] = new() { Alias = "Last Connection Activity", Visible = false },
                        ["EndpointStatus"] = Hidden(),
                        ["SnapshotDate"] = new() { Alias = "Snapshot Date", Description = "Date the Service Broker data was collected" },
                        ["InstanceID"] = Hidden(),
                        // Shown when the collection hasn't run for the instance; the proc returns this instead.
                        ["Message"] = new()
                    }
                }.SetDisplayIndexBasedOnColumnOrder(),
                [DatabasesTableIndex] = new CustomReportResult
                {
                    ResultName = "Databases",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["DB"] = new() { Alias = "Database" },
                        ["DatabaseStatus"] = Hidden(),
                        ["Issues"] = new() { Description = "Database level issues.  Queue issues are shown in the queues grid.", Highlighting = Status("DatabaseStatus") },
                        ["is_broker_enabled"] = new() { Alias = "Broker Enabled" },
                        ["queue_count"] = new() { Alias = "Queues", FormatString = "N0" },
                        ["queue_message_count"] = new() { Alias = "Queue Messages", FormatString = "N0" },
                        ["transmission_queue_count"] = new() { Alias = "Transmission Queue", FormatString = "N0", Description = "Messages waiting to be sent", Highlighting = Status("TransmissionQueueStatus") },
                        ["transmission_queue_error_count"] = new() { Alias = "Transmission Queue (with status)", FormatString = "N0", Description = "Messages in the transmission queue with a transmission status.  This is usually the error from the last attempt to send the message.  See the Transmission Queue grid.", Highlighting = Status("TransmissionQueueStatus") },
                        ["TransmissionQueueAgeMins"] = new() { Alias = "Transmission Queue Age (mins)", FormatString = "N0", Description = "Age of the oldest message waiting to be sent", Highlighting = Status("TransmissionQueueStatus") },
                        ["TransmissionQueueStatus"] = Hidden(),
                        ["conversation_count"] = new() { Alias = "Open Conversations", FormatString = "N0", Description = "Conversation endpoints that are not closed.  A large number usually means conversations are not being ended.  Over 1,000,000 endpoints they aren't counted by state, so this is the total including closed endpoints.", Highlighting = Status("ConversationStatus") },
                        ["ConversationStatus"] = Hidden(),
                        ["conversing_count"] = new() { Alias = "Conversing", FormatString = "N0", Visible = false },
                        ["disconnected_inbound_count"] = new() { Alias = "Disconnected Inbound", FormatString = "N0", Description = "The other side has ended the conversation, but this side hasn't.  These build up if the conversation isn't ended on receiving an EndDialog message.", Highlighting = Status("ConversationStatus") },
                        ["disconnected_outbound_count"] = new() { Alias = "Disconnected Outbound", FormatString = "N0", Visible = false },
                        ["error_count"] = new() { Alias = "Error", FormatString = "N0", Description = "Conversations in the error state.  These stay until the application ends them with END CONVERSATION.", Highlighting = Status("ErrorConversationStatus") },
                        ["ErrorConversationStatus"] = Hidden(),
                        ["closed_count"] = new() { Alias = "Closed", FormatString = "N0", Description = "Closed conversations.  These are kept for a while after the conversation ends and are not a sign of a problem.", Visible = false },
                        ["procedures_invoked_1hr"] = new() { Alias = "Activations (1hr)", FormatString = "N0", Description = "Activation procedures started in the hour before the last collection" },
                        ["tasks_aborted_1hr"] = new() { Alias = "Activation Aborted (1hr)", FormatString = "N0", Description = "Activation tasks that ended with an error in the hour before the last collection", Highlighting = Status("ActivationStatus") },
                        ["task_limit_reached_1hr"] = new() { Alias = "Task Limit Reached (1hr)", FormatString = "N0", Description = "Times a queue had max readers running when another reader would have been started, in the hour before the last collection." },
                        ["ActivationStatus"] = Hidden(),
                        ["TransmissionQueueAgeWarningThreshold"] = new() { Alias = "Transmission Queue Warning (mins)", Visible = false },
                        ["TransmissionQueueAgeCriticalThreshold"] = new() { Alias = "Transmission Queue Critical (mins)", Visible = false },
                        ["ConversationCountWarningThreshold"] = new() { Alias = "Conversations Warning", FormatString = "N0", Visible = false },
                        ["ConversationCountCriticalThreshold"] = new() { Alias = "Conversations Critical", FormatString = "N0", Visible = false },
                        ["ThresholdConfigurationLevel"] = new() { Alias = "Config Level", Visible = false },
                        ["SnapshotDate"] = new() { Alias = "Snapshot Date" },
                        ["Edit"] = new() { Alias = "Edit", Link = new EditThresholdLink(), Description = "Configure thresholds for this database" },
                        ["InstanceID"] = Hidden(),
                        ["DatabaseID"] = Hidden()
                    }
                }.SetDisplayIndexBasedOnColumnOrder(),
                [QueuesTableIndex] = new CustomReportResult
                {
                    ResultName = "Queues",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["DB"] = new() { Alias = "Database" },
                        ["schema_name"] = new() { Alias = "Schema" },
                        ["queue_name"] = new() { Alias = "Queue", Highlighting = Status("QueueStatus") },
                        ["QueueStatus"] = Hidden(),
                        ["Issues"] = new() { Highlighting = Status("QueueStatus") },
                        ["message_count"] = new() { Alias = "Messages", FormatString = "N0", Description = "Messages in the queue.  For a queue with RETENTION = ON, messages waiting to be received - blank if the queue couldn't be read.", Highlighting = Status("QueueDepthStatus") },
                        ["QueueDepthStatus"] = Hidden(),
                        ["OldestMessageAgeMins"] = new() { Alias = "Oldest Message (mins)", FormatString = "N0", Description = "How long the oldest message has been waiting to be received.  Shows a queue that isn't being processed.  Not collected for queues over 100,000 messages, once 1,000,000 messages have been read across the instance (smallest queues first), or without permission to read the queue.", Highlighting = Status("QueueAgeStatus") },
                        ["QueueAgeStatus"] = Hidden(),
                        ["is_enabled"] = new() { Alias = "Enabled", Description = "False if the queue is disabled (STATUS = OFF) - messages can't be added or received.  Usually caused by a poison message - 5 consecutive rollbacks of a RECEIVE - where poison message handling is on.  Re-enable with ALTER QUEUE ... WITH STATUS = ON after dealing with the message.", Highlighting = Status("EnabledStatus") },
                        ["EnabledStatus"] = Hidden(),
                        ["status_changed_date"] = new() { Alias = "Enabled Changed", Description = "When the queue was seen to be enabled or disabled, accurate to the collection schedule.  Blank if it hasn't changed since it was first collected." },
                        ["is_activation_enabled"] = new() { Alias = "Activation Enabled" },
                        ["activation_procedure"] = new() { Alias = "Activation Procedure" },
                        ["max_readers"] = new() { Alias = "Max Readers", FormatString = "N0" },
                        ["activated_tasks"] = new() { Alias = "Running Readers", FormatString = "N0", Description = "Activated tasks running for the queue.  Highlighted where all readers are busy with messages still waiting.", Highlighting = Status("ReadersStatus") },
                        ["ReadersStatus"] = Hidden(),
                        ["monitor_state"] = new() { Alias = "Monitor State", Description = "State of the queue monitor (activated queues).  INACTIVE, NOTIFIED or RECEIVES_OCCURRING.  NOTIFIED for a long time with messages waiting means activation fired but nothing is receiving from the queue." },
                        ["monitor_tasks_waiting"] = new() { Alias = "Tasks Waiting", FormatString = "N0", Visible = false },
                        ["monitor_last_activated_time"] = new() { Alias = "Last Activated", Visible = false },
                        ["monitor_last_empty_rowset_time"] = new() { Alias = "Last Empty RECEIVE", Description = "Last time a RECEIVE on the queue returned no messages.  Long ago with messages waiting suggests a backlog." },
                        ["is_retention_enabled"] = new() { Alias = "Retention", Visible = false },
                        ["is_poison_message_handling_enabled"] = new() { Alias = "Poison Message Handling", Visible = false },
                        ["is_broker_enabled"] = new() { Alias = "Broker Enabled", Visible = false },
                        ["QueueDepthWarningThreshold"] = new() { Alias = "Warning Threshold", FormatString = "N0", Visible = false },
                        ["QueueDepthCriticalThreshold"] = new() { Alias = "Critical Threshold", FormatString = "N0", Visible = false },
                        ["QueueAgeWarningThreshold"] = new() { Alias = "Age Warning (mins)", FormatString = "N0", Visible = false },
                        ["QueueAgeCriticalThreshold"] = new() { Alias = "Age Critical (mins)", FormatString = "N0", Visible = false },
                        ["ThresholdConfigurationLevel"] = new() { Alias = "Config Level", Visible = false },
                        ["SnapshotDate"] = new() { Alias = "Snapshot Date" },
                        ["Edit"] = new() { Alias = "Edit", Link = new EditThresholdLink(), Description = "Configure thresholds for this queue" },
                        ["QueueID"] = Hidden(),
                        ["InstanceID"] = Hidden(),
                        ["DatabaseID"] = Hidden()
                    }
                }.SetDisplayIndexBasedOnColumnOrder(),
                [TransmissionQueueTableIndex] = new CustomReportResult
                {
                    ResultName = "Transmission Queue",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["DB"] = new() { Alias = "Database" },
                        ["to_service_name"] = new() { Alias = "To Service" },
                        ["transmission_status"] = new() { Alias = "Transmission Status", Description = "Reason the message hasn't been sent.  Usually the error from the last attempt.  Blank if the message hasn't been tried yet." },
                        ["message_count"] = new() { Alias = "Messages", FormatString = "N0" },
                        ["oldest_enqueue_time"] = new() { Alias = "Oldest Message" },
                        ["AgeMins"] = new() { Alias = "Age (mins)", FormatString = "N0" },
                        ["SnapshotDate"] = new() { Alias = "Snapshot Date" },
                        ["InstanceID"] = Hidden(),
                        ["DatabaseID"] = Hidden()
                    }
                }.SetDisplayIndexBasedOnColumnOrder(),
                [QueueDepthTableIndex] = new CustomReportResult
                {
                    ResultName = "Queue Depth Over Time",
                    ShowWithAllResults = false, // Chart data
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["SnapshotDate"] = new() { Alias = "Snapshot Date" },
                        ["Queue"] = new(),
                        ["MessageCount"] = new() { Alias = "Messages (max)", FormatString = "N0" }
                    }
                },
                [ActivityTableIndex] = new CustomReportResult
                {
                    ResultName = "Activity Over Time",
                    ShowWithAllResults = false, // Chart data
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["SnapshotDate"] = new() { Alias = "Snapshot Date" },
                        ["Measure"] = new(),
                        ["ValuePerSec"] = new() { Alias = "Per Sec", FormatString = "N2" },
                        ["Value"] = new() { Alias = "Total", FormatString = "N0" }
                    }
                },
                [ConversationsTableIndex] = new CustomReportResult
                {
                    ResultName = "Open Conversations Over Time",
                    ShowWithAllResults = false, // Chart data
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["SnapshotDate"] = new() { Alias = "Snapshot Date" },
                        ["DB"] = new() { Alias = "Database" },
                        ["ConversationCount"] = new() { Alias = "Open Conversations (max)", FormatString = "N0" }
                    }
                }
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@DatabaseID", ParamType = "INT" },
                    new() { ParamName = "@FromDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@ToDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@DateGroupingMin", ParamType = "INT" },
                    new() { ParamName = "@TopN", ParamType = "INT" },
                    new() { ParamName = "@IncludeCritical", ParamType = "BIT" },
                    new() { ParamName = "@IncludeWarning", ParamType = "BIT" },
                    new() { ParamName = "@IncludeNA", ParamType = "BIT" },
                    new() { ParamName = "@IncludeOK", ParamType = "BIT" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@TopN",
                    Name = "Top N", // Lines on the queue depth (queues and transmission queues) and open conversations charts
                    PickerItems = new Dictionary<object, string>
                    {
                        [5] = "Top 5",
                        [10] = "Top 10",
                        [20] = "Top 20",
                        [50] = "Top 50"
                    },
                    DefaultValue = 5, // More and the legend crowds out the chart
                    MenuBar = true,
                    DataType = typeof(int)
                }
            }
        };

        #endregion Report definition
    }
}
