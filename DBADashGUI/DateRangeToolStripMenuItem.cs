using DBADashGUI.Performance;
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;
using Octokit;
using DateRange = DBADashGUI.Performance.DateRange;

namespace DBADashGUI
{
    [ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.All)]
    public class DateRangeToolStripMenuItem : ToolStripDropDownButton
    {
        public TimeSpan DefaultTimeSpan { get; set; } = TimeSpan.FromMinutes(60);

        private TimeSpan? _selectedTimeSpan;
        public TimeSpan? SelectedTimeSpan { get => _selectedTimeSpan; set => SetTimeSpan(value); }
        public DateTime? SelectedDateFrom { get; private set; }
        public DateTime? SelectedDateTo { get; private set; }

        public DateTime DateFrom => SelectedTimeSpan.HasValue ? DateHelper.AppNow.Subtract(SelectedTimeSpan.Value) : SelectedDateFrom ?? DateHelper.AppNow.Subtract(DefaultTimeSpan);
        public DateTime DateTo => SelectedTimeSpan.HasValue ? DateHelper.AppNow : SelectedDateTo ?? DateHelper.AppNow;

        public DateTime DateFromUtc => DateFrom.AppTimeZoneToUtc();
        public DateTime DateToUtc => DateTo.AppTimeZoneToUtc();

        public TimeSpan ActualTimeSpan => SelectedTimeSpan ?? DateTo.Subtract(DateFrom);

        public TimeSpan MinimumTimeSpan { get; set; } = TimeSpan.MinValue;

        public TimeSpan MaximumTimeSpan { get; set; } = TimeSpan.MaxValue;

        private string selectedText;

        public override string Text => selectedText;

        public void SetDateRangeUtc(DateTime from, DateTime to)
        {
            SetDateRange(from.ToAppTimeZone(), to.ToAppTimeZone());
        }

        public void SetDateRange(DateTime from, DateTime to)
        {
            SelectedDateFrom = from;
            SelectedDateTo = to;
            SelectedTimeSpan = null;
            SetTextAndCheck();
        }

        public void SetTimeSpan(TimeSpan? ts)
        {
            _selectedTimeSpan = ts;
            if (SelectedTimeSpan.HasValue)
            {
                SelectedDateTo = null;
                SelectedDateFrom = null;
            }
            SetTextAndCheck();
        }

        public override ToolStripItemDisplayStyle DisplayStyle => ToolStripItemDisplayStyle.ImageAndText;

        public override Image Image => Properties.Resources.Time_16x;

        public event EventHandler DateRangeChanged;

        private void SetTimeSpanUser(TimeSpan value)
        {
            SetTimeSpan(value);
            DateRangeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetDateRangeUser(DateTime from, DateTime to)
        {
            SetDateRange(from, to);
            DateRangeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetTextAndCheck()
        {
            selectedText = string.Empty;
            var selectedRange = SelectedDateFrom.HasValue && SelectedDateTo.HasValue ? new ValueTuple<DateTime, DateTime>(SelectedDateFrom.Value, SelectedDateTo.Value) : new ValueTuple<DateTime, DateTime>(DateTime.MinValue, DateTime.MaxValue);
            foreach (var itm in this.DropDownItems.OfType<ToolStripMenuItem>())
            {
                itm.Checked = ((itm.Tag as TimeSpan?) == SelectedTimeSpan && SelectedTimeSpan != null) ||
                              (itm.Tag is (DateTime, DateTime) && SelectedDateFrom.HasValue && SelectedDateTo.HasValue && ((DateTime, DateTime))itm.Tag == selectedRange);
                itm.Font = itm.Checked ? new Font(itm.Font, FontStyle.Bold) : new Font(itm.Font, FontStyle.Regular);

                if (itm.Checked)
                {
                    selectedText = itm.Text;
                }

                if (itm.DropDownItems.Count == 0 || SelectedDateTo == null || SelectedDateFrom == null) continue;
                foreach (var dtItem in itm.DropDownItems.OfType<ToolStripMenuItem>())
                {
                    dtItem.Checked = ((DateTime, DateTime))dtItem.Tag! == selectedRange;
                    dtItem.Font = itm.Checked ? new Font(itm.Font, FontStyle.Bold) : new Font(itm.Font, FontStyle.Regular);
                    if (!dtItem.Checked) continue;
                    itm.Checked = true;
                    itm.Font = new Font(itm.Font, FontStyle.Bold);
                    selectedText = dtItem.Text;
                }
            }

            if (selectedText == string.Empty)
            {
                selectedText = "Custom";
                customMenuItem.Checked = true;
                customMenuItem.Font = new Font(this.Font, FontStyle.Bold);
            }

            Font = SelectedTimeSpan == DefaultTimeSpan
                ? new Font(this.Font, FontStyle.Regular)
                : new Font(this.Font, FontStyle.Bold);
        }

        public DateRangeToolStripMenuItem()
        {
            _selectedTimeSpan = DefaultTimeSpan;
            this.DropDownItems.AddRange(new ToolStripItem[]
             {
                CreateTimeSpanMenuItem("5 Mins",TimeSpan.FromMinutes(5)),
                CreateTimeSpanMenuItem("10 Mins", TimeSpan.FromMinutes(10)),
                CreateTimeSpanMenuItem("15 Mins", TimeSpan.FromMinutes(15)),
                CreateTimeSpanMenuItem("30 Mins", TimeSpan.FromMinutes(30)),
                CreateTimeSpanMenuItem("1 Hr", TimeSpan.FromHours(1)),
                CreateTimeSpanMenuItem("2 Hr", TimeSpan.FromHours(2)),
                CreateTimeSpanMenuItem("3 Hr", TimeSpan.FromHours(3)),
                CreateTimeSpanMenuItem("6 Hr", TimeSpan.FromHours(6)),
                CreateTimeSpanMenuItem("12 Hr", TimeSpan.FromHours(12)),
                CreateTimeSpanMenuItem("1 Day", TimeSpan.FromDays(1)),
                CreateTimeSpanMenuItem("2 Days", TimeSpan.FromDays(2)),
                CreateTimeSpanMenuItem("3 Days", TimeSpan.FromDays(3)),
                CreateTimeSpanMenuItem("7 Days", TimeSpan.FromDays(7)),
                CreateTimeSpanMenuItem("14 Days", TimeSpan.FromDays(14)),
                CreateTimeSpanMenuItem("28 Days", TimeSpan.FromDays(28)),
                CreateTimeSpanMenuItem("30 Days", TimeSpan.FromDays(30)),
                CreateTimeSpanMenuItem("60 Days", TimeSpan.FromDays(60)),
                CreateTimeSpanMenuItem("90 Days", TimeSpan.FromDays(90)),
                CreateTimeSpanMenuItem("180 Days", TimeSpan.FromDays(180)),
                CreateTimeSpanMenuItem("1 year", TimeSpan.FromDays(365)),
                new ToolStripSeparator(),
                CreateDateSelectorMenuItem(),
                CreateMonthSelectorMenuItem(),
                CreateCustomMenuItem()
         });
            SetTextAndCheck();
            this.DropDownOpening += DateRangeToolStripMenuItem_DropDownOpening;
        }

        private void DateRangeToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            foreach (var itm in this.DropDownItems.OfType<ToolStripMenuItem>())
            {
                SetVisibility(itm);
                if (itm.DropDownItems.Count <= 0) continue;
                foreach (var dtItem in itm.DropDownItems.OfType<ToolStripMenuItem>())
                {
                    SetVisibility(dtItem);
                }
                itm.Visible = itm.DropDownItems.OfType<ToolStripMenuItem>().Any(mnu => mnu.Enabled);
            }
        }

        private void SetVisibility(ToolStripMenuItem itm)
        {
            TimeSpan ts;
            switch (itm.Tag)
            {
                case TimeSpan ts1:
                    ts = ts1;
                    break;

                case ValueTuple<DateTime, DateTime> range:
                    ts = range.TimeSpan();
                    break;

                default:
                    return;
            }
            itm.Enabled = ts >= MinimumTimeSpan && ts <= MaximumTimeSpan;
            itm.Visible = ts >= MinimumTimeSpan && ts <= MaximumTimeSpan;
        }

        private ToolStripMenuItem CreateDateRangeMenuItem(ValueTuple<DateTime, DateTime> range, string label)
        {
            var isChecked = SelectedDateFrom == range.Item1 &&
                            SelectedDateTo == range.Item2;
            var menuItem = new ToolStripMenuItem(label)
            {
                Tag = range,
                Checked = isChecked,
                Font = isChecked ? new Font(this.Font, FontStyle.Bold) : new Font(this.Font, FontStyle.Regular)
            };

            menuItem.Click += (sender, e) =>
            {
                SetDateRangeUser(range.Item1, range.Item2);
            };
            return menuItem;
        }

        private ToolStripMenuItem CreateTimeSpanMenuItem(string text, TimeSpan timeSpan)
        {
            var menuItem = new ToolStripMenuItem(text)
            {
                Tag = timeSpan
            };
            menuItem.Click += (sender, e) =>
            {
                SetTimeSpanUser(timeSpan);
            };

            return menuItem;
        }

        private ToolStripMenuItem customMenuItem;
        private ToolStripMenuItem dateMenuItem;
        private ToolStripMenuItem monthMenuItem;

        private ToolStripMenuItem CreateCustomMenuItem()
        {
            var menuItem = new ToolStripMenuItem("Custom");
            menuItem.Click += (s, e) =>
            {
                var frm = new CustomTimePicker()
                { FromDate = DateFrom, ToDate = DateTo };
                frm.ShowDialog();
                if (frm.DialogResult != DialogResult.OK) return;
                SetDateRangeUser(frm.FromDate, frm.ToDate);
            };
            customMenuItem = menuItem;
            return menuItem;
        }

        private ToolStripMenuItem CreateDateSelectorMenuItem()
        {
            dateMenuItem = new ToolStripMenuItem("Date");
            UpdateDateMenuItems();
            dateMenuItem.DropDownOpening += (sender, e) => UpdateDateMenuItems();
            return dateMenuItem;
        }

        private ToolStripMenuItem CreateMonthSelectorMenuItem()
        {
            monthMenuItem = new ToolStripMenuItem("Month");
            UpdateMonthMenuItems();
            monthMenuItem.DropDownOpening += (sender, e) => UpdateMonthMenuItems();
            return monthMenuItem;
        }

        private void UpdateMonthMenuItems()
        {
            monthMenuItem.DropDownItems.Clear();
            for (var i = 0; i <= 12; i++)
            {
                var range = new ValueTuple<DateTime, DateTime>(DateHelper.AppNow.AddMonths(-i).StartOfMonth(), DateHelper.AppNow.AddMonths(-i + 1).StartOfMonth());
                var monthsAgo = i switch
                {
                    0 => "This Month",
                    1 => "Last Month",
                    _ => i + " months ago"
                };
                var humanDateString = range.Item1.ToString("MMM", CultureInfo.CurrentCulture) + " (" + monthsAgo + ")";

                var monthItem = CreateDateRangeMenuItem(range, humanDateString);

                monthMenuItem.DropDownItems.Add(monthItem);
            }
        }

        private void UpdateDateMenuItems()
        {
            dateMenuItem.DropDownItems.Clear();
            for (var i = 0; i <= 14; i++)
            {
                var range = new ValueTuple<DateTime, DateTime>(DateHelper.AppNow.AddDays(-i).Date, DateHelper.AppNow.AddDays(-i + 1).Date);
                var daysAgo = i switch
                {
                    0 => "Today",
                    1 => "Yesterday",
                    _ => i + " days ago"
                };
                var humanDateString = range.Item1.ToShortDateString() + " (" + range.Item1.DayOfWeek.ToString()[..3] + " - " +
                                      daysAgo + ")";

                var dateItem = CreateDateRangeMenuItem(range, humanDateString);
                dateMenuItem.DropDownItems.Add(dateItem);
            }
        }
    }
}