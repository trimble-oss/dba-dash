using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADashGUI.Performance
{
    public class BaseCounter
    {
        public string ObjectName { get; set; }
        public string CounterName { get; set; }
        public string InstanceName { get; set; }

        [JsonIgnore]
        public string FullName => $"{ObjectName}\\{CounterName}\\{InstanceName}".Trim('\\');

        public override string ToString()
        {
            return ObjectName + "\\" + CounterName + "\\" + InstanceName;
        }
    }

    public class AlertCounter : BaseCounter
    {
        [JsonIgnore]
        public int CounterID => -1;

        private string _instanceName;

        public bool ApplyToAllInstances { get; set; }

        public new string InstanceName { get => ApplyToAllInstances ? "*" : _instanceName; set => _instanceName = value; }

        public override string ToString()
        {
            return ObjectName + "\\" + CounterName + "\\" + InstanceName;
        }
    }

    public class CounterEntity : BaseCounter
    {
        // Cache mapping normalized "object|counter|instance" -> CounterID to avoid repeatedly
        // scanning the DataTable returned by CommonData.GetCounters().
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _counterIdCache = new();

        private static string KeyFor(string objectName, string counterName, string instanceName)
        {
            return ($"{objectName ?? string.Empty}|{counterName ?? string.Empty}|{instanceName ?? string.Empty}").ToLowerInvariant();
        }

        public static void ClearCounterIdCache()
        {
            _counterIdCache.Clear();
        }

        [JsonIgnore]
        public int CounterID
        {
            get
            {
                if (field > 0) return field;

                // Capture the original key based on the current properties before any parsing/mutation.
                var originalKey = KeyFor(ObjectName, CounterName, InstanceName);

                // If we have a cached id, only use it when it's a valid (> 0) CounterID.
                // Previously we cached misses (0) which caused permanent negative caching
                // and prevented later resolution until ClearCounterIdCache was called.
                if (_counterIdCache.TryGetValue(originalKey, out var cachedId) && cachedId > 0)
                {
                    field = cachedId;
                    return field;
                }

                var counters = CommonData.GetCounters();

                // Backwards compatibility: older persisted counters encoded the full path
                // as "ObjectName\\CounterName\\InstanceName" in CounterName.
                // Safely parse that format only when ObjectName/InstanceName are not set
                // and CounterName contains a 3-part backslash-separated value.
                string parsedKey = originalKey;
                if (string.IsNullOrWhiteSpace(ObjectName)
                    && string.IsNullOrWhiteSpace(InstanceName)
                    && !string.IsNullOrWhiteSpace(CounterName))
                {
                    var parts = CounterName.Split('\\');
                    if (parts.Length == 3)
                    {
                        ObjectName = parts[0].Trim();
                        CounterName = parts[1].Trim();
                        InstanceName = parts[2].Trim();
                        parsedKey = KeyFor(ObjectName, CounterName, InstanceName);
                    }
                }

                var match = counters.AsEnumerable().FirstOrDefault(r =>
                    string.Equals(r.Field<string>("object_name"), ObjectName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(r.Field<string>("counter_name"), CounterName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(r.Field<string>("instance_name"), InstanceName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    field = match.Field<int>("CounterID");
                }

                // Only cache successful resolutions (CounterID > 0). Do not cache misses (0)
                // which would prevent later successful resolution if counters are refreshed.
                if (field > 0)
                {
                    _counterIdCache.GetOrAdd(originalKey, field);
                    if (parsedKey != originalKey)
                    {
                        _counterIdCache.GetOrAdd(parsedKey, field);
                    }
                }

                return field;
            }
            set
            {
                field = value;
                if (field > 0)
                {
                    var key = KeyFor(ObjectName, CounterName, InstanceName);
                    _counterIdCache.AddOrUpdate(key, value, (_, __) => value);
                }
            }
        }
    }

    public class Counter : CounterEntity
    {
        public bool Max { get; set; }
        public bool Avg { get; set; }
        public bool Min { get; set; }
        public bool Total { get; set; }
        public bool SampleCount { get; set; }

        public bool Current { get; set; }

        public List<string> GetAggColumns(bool includeCurrent = true)
        {
            var agg = new List<string>();
            if (Avg) { agg.Add("Avg"); }
            if (Max) { agg.Add("Max"); }
            if (Min) { agg.Add("Min"); }
            if (Total) { agg.Add("Total"); }
            if (Current && includeCurrent) { agg.Add("Current"); }
            if (SampleCount) { agg.Add("SampleCount"); }
            return agg;
        }
    }
}