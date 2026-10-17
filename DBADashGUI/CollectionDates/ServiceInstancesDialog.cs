using DBADash;
using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using DBADashService;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.CollectionDates
{
    /// <summary>
    /// Lists the instances a service collects, with any schedules of their own, temporary schedules and pauses - so it's
    /// clear which instances a change to the service schedule affects.  Given a collection, it shows whether each instance
    /// follows the service schedule for it (and so would be affected) or has its own.
    /// </summary>
    internal sealed class ServiceInstancesDialog : Form
    {
        private ServiceInstancesDialog(string serviceName, IReadOnlyCollection<ServiceInstanceInfo> instances, CollectionType? collectionType)
        {
            Text = collectionType.HasValue ? $"{collectionType} - Service Instances" : "Service Instances";
            Size = new Size(1000, 560);
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            MinimizeBox = false;

            var following = collectionType.HasValue ? instances.Count(i => i.FollowsServiceSchedule(collectionType.Value)) : instances.Count;
            var lblSummary = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(6),
                Text = collectionType.HasValue
                    ? $"{following} of {instances.Count} instances collected by {serviceName} follow the service schedule for {collectionType}, so a change to it applies to them.  Instances with their own schedule for {collectionType} aren't affected."
                    : $"{instances.Count} instances are collected by {serviceName}.  A change to the service schedule applies to each of them, except for collections where the instance has its own schedule."
            };

            var dgv = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                DataSource = new DataView(BuildTable(instances, collectionType))
            };

            Controls.Add(dgv);
            Controls.Add(lblSummary);
            Load += (_, _) =>
            {
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                this.ApplyTheme();
            };
        }

        /// <summary>Shows the instances modally.  <paramref name="collectionType"/> focuses on one collection.</summary>
        public static void Show(IWin32Window owner, string serviceName, IReadOnlyCollection<ServiceInstanceInfo> instances, CollectionType? collectionType = null)
        {
            using var frm = new ServiceInstancesDialog(serviceName, instances, collectionType);
            frm.ShowDialog(owner);
        }

        private static DataTable BuildTable(IReadOnlyCollection<ServiceInstanceInfo> instances, CollectionType? collectionType)
        {
            // The service only knows ConnectionIDs - show the name used in the GUI where the repository has it
            var displayNames = CommonData.Instances?.Rows.Cast<DataRow>()
                .Where(r => r["ConnectionID"] != DBNull.Value)
                .GroupBy(r => (string)r["ConnectionID"], StringComparer.InvariantCultureIgnoreCase)
                .ToDictionary(g => g.Key, g => (string)g.First()["InstanceDisplayName"], StringComparer.InvariantCultureIgnoreCase)
                ?? new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            var dt = new DataTable();
            dt.Columns.Add("Instance", typeof(string));
            dt.Columns.Add("ConnectionID", typeof(string));
            if (collectionType.HasValue)
            {
                dt.Columns.Add("Follows Service Schedule", typeof(bool));
                dt.Columns.Add("Own Schedule", typeof(string));
                dt.Columns.Add("Temporary Schedule", typeof(string));
            }
            else
            {
                dt.Columns.Add("Own Schedules", typeof(string));
                dt.Columns.Add("Temporary Schedules", typeof(string));
            }
            dt.Columns.Add("Collections Paused Until", typeof(string));

            foreach (var instance in instances.OrderBy(i => collectionType.HasValue && !i.FollowsServiceSchedule(collectionType.Value))
                         .ThenBy(i => displayNames.GetValueOrDefault(i.ConnectionID, i.ConnectionID), StringComparer.CurrentCultureIgnoreCase))
            {
                var row = dt.NewRow();
                row["Instance"] = displayNames.GetValueOrDefault(instance.ConnectionID, instance.ConnectionID);
                row["ConnectionID"] = instance.ConnectionID;
                if (collectionType.HasValue)
                {
                    var type = collectionType.Value;
                    row["Follows Service Schedule"] = instance.FollowsServiceSchedule(type);
                    row["Own Schedule"] = instance.InstanceSchedules.TryGetValue(type, out var own) ? Describe(own.Schedule, own.RunOnServiceStart) : string.Empty;
                    row["Temporary Schedule"] = instance.TemporarySchedules.TryGetValue(type, out var temporary) ? DescribeTemporary(temporary) : string.Empty;
                }
                else
                {
                    row["Own Schedules"] = string.Join(", ", instance.InstanceSchedules
                        .OrderBy(s => s.Key.ToString())
                        .Select(s => $"{s.Key} ({Describe(s.Value.Schedule, s.Value.RunOnServiceStart)})"));
                    row["Temporary Schedules"] = string.Join(", ", instance.TemporarySchedules
                        .OrderBy(s => s.Key.ToString())
                        .Select(s => $"{s.Key} ({DescribeTemporary(s.Value)})"));
                }
                row["Collections Paused Until"] = instance.PausedUntilUtc.HasValue ? PauseCollections.FormatPausedUntil(instance.PausedUntilUtc.Value) : string.Empty;
                dt.Rows.Add(row);
            }
            return dt;
        }

        private static string Describe(string schedule, bool runOnServiceStart) =>
            CronParser.GetScheduleDescriptionSafe(schedule, runOnServiceStart: runOnServiceStart);

        private static string DescribeTemporary(TemporarySchedule schedule) =>
            $"{Describe(schedule.Schedule, schedule.RunOnServiceStart)} until {PauseCollections.FormatPausedUntil(schedule.UntilUtc)}";
    }
}
