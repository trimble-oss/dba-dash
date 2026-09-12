using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBADash.QueryStats
{
    /// <summary>
    /// Identifies a plan cache row for diffing purposes: the plan handle and the statement offsets,
    /// reduced to 128 bits.
    ///
    /// <para>The handle itself is never stored.  The baseline is only ever looked up, never enumerated
    /// back into handles, so hashing the key rather than keeping it cuts the entry from the best part of
    /// a hundred bytes plus an array allocation down to sixteen bytes of struct.  That is what makes a
    /// baseline of tens of thousands of statements per instance affordable in a service collecting from
    /// hundreds of instances.</para>
    ///
    /// <para>The plan handle is in the key rather than the sql_handle because the two are not
    /// interchangeable: the same ad hoc text executed in two databases produces one sql_handle and two
    /// plan handles.  Keying on the sql_handle would merge two databases' counters into one nonsense
    /// delta.</para>
    /// </summary>
    public readonly struct BaselineKey : IEquatable<BaselineKey>
    {
        private const ulong FnvPrime = 1099511628211;
        private const ulong FnvOffsetA = 14695981039346656037;
        private const ulong FnvOffsetB = 1469598103934665603;

        private readonly ulong a;
        private readonly ulong b;

        private BaselineKey(ulong a, ulong b)
        {
            this.a = a;
            this.b = b;
        }

        public static BaselineKey Create(ReadOnlySpan<byte> planHandle, int startOffset, int endOffset)
        {
            // Two independently seeded FNV-1a passes over the same bytes, walked in opposite directions so
            // the two halves do not degenerate into the same value.
            var ha = FnvOffsetA;
            var hb = FnvOffsetB;

            for (var i = 0; i < planHandle.Length; i++)
            {
                ha = (ha ^ planHandle[i]) * FnvPrime;
                hb = (hb ^ planHandle[planHandle.Length - 1 - i]) * FnvPrime;
            }

            unchecked
            {
                var offsets = ((ulong)(uint)startOffset << 32) | (uint)endOffset;
                for (var shift = 0; shift < 64; shift += 8)
                {
                    var octet = (byte)(offsets >> shift);
                    ha = (ha ^ octet) * FnvPrime;
                    hb = (hb ^ octet) * FnvPrime;
                }
            }

            return new BaselineKey(ha, hb);
        }

        public bool Equals(BaselineKey other) => a == other.a && b == other.b;

        public override bool Equals(object obj) => obj is BaselineKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(a, b);

        internal void Write(BinaryWriter writer)
        {
            writer.Write(a);
            writer.Write(b);
        }

        internal static BaselineKey Read(BinaryReader reader) => new(reader.ReadUInt64(), reader.ReadUInt64());
    }

    /// <summary>
    /// The previous snapshot of one plan cache row.  <see cref="NotSupported"/> stands in for a counter the
    /// instance's version does not have, so the whole entry stays a flat struct of longs.
    /// </summary>
    public struct BaselineEntry
    {
        /// <summary>Sentinel for a measure this instance does not expose.  Every real counter is
        /// non-negative, so a negative value cannot be mistaken for one.</summary>
        public const long NotSupported = -1;

        public long ExecutionCount;
        public long WorkerTime;
        public long ElapsedTime;
        public long LogicalReads;
        public long LogicalWrites;
        public long PhysicalReads;
        public long ClrTime;
        public long Rows;
        public long Dop;
        public long GrantKb;
        public long UsedGrantKb;
        public long Spills;

        /// <summary>The slowest execution in the plan's life.  Only says anything about an interval by having
        /// risen since the previous collection - see the processor's slowest execution.</summary>
        public long MaxElapsedTime;

        public DateTime CreationTimeUtc;
        public long PlanGenerationNum;

        /// <summary>When this row was last seen in the plan cache, used to decide what to evict first.</summary>
        public long LastSeenTicks;
    }

    /// <summary>
    /// One instance's baseline: what every plan cache row it executed looked like at the previous
    /// collection, plus the two facts needed to bound the next one - when that collection happened, and
    /// how long its read took.
    ///
    /// <para>Bounded by <see cref="MaxEntries"/> and trimmed least-recently-seen first.  Eviction is safe
    /// rather than silently wrong: an evicted row that returns is treated as a first sighting, which is
    /// either attributed in full (if the plan compiled inside the interval) or counted as unattributed
    /// work.  Either way the eviction count travels to the repository, so a cap set too low shows up as a
    /// data quality number rather than as quietly missing load.</para>
    ///
    /// <para>Not safe for concurrent use.  A collection holds the connection's lock from the read until it
    /// has applied its result - see <see cref="QueryStatsBaselineStore.LockAsync"/>.</para>
    /// </summary>
    public class Baseline
    {
        private const int FileFormatVersion = 3;

        private readonly Dictionary<BaselineKey, BaselineEntry> entries = new();

        public int MaxEntries { get; set; } = 20000;

        /// <summary>
        /// When the previous collection read the plan cache.  Drives both the source filter (only rows
        /// executed since then can have a delta) and PeriodTime on the fact rows, so a missed collection
        /// describes itself: the next delta covers the whole gap and says how long that gap was.
        /// </summary>
        public DateTime? LastCollectionUtc { get; set; }

        /// <summary>
        /// The snapshot of the first collection applied to this baseline since it was last empty.  From then on
        /// every collection has read every row that finished an execution since the one before it, so the
        /// entries are a complete record of what ran - apart from what <see cref="Trim"/> has dropped, which
        /// <see cref="EvictedThroughUtc"/> accounts for.  Null while there is nothing to trust: a new baseline,
        /// or one just discarded.
        /// </summary>
        public DateTime? ContinuousSinceUtc { get; private set; }

        /// <summary>
        /// The most recent last sighting of any entry <see cref="Trim"/> has dropped since
        /// <see cref="ContinuousSinceUtc"/>.  A row is always compiled before it is first seen, so a row compiled
        /// after this cannot have been one of them.
        /// </summary>
        public DateTime? EvictedThroughUtc { get; private set; }

        /// <summary>How long the previous read of the DMV took.  A plan cache large enough to make the
        /// read expensive is a property of the instance, not a transient, so the previous duration is the
        /// best available predictor for whether the next one should be attempted.</summary>
        public int LastReadDurationMs { get; set; }

        /// <summary>Rows dropped by the most recent trim, reported and then reset.</summary>
        public int Evictions { get; private set; }

        public int Count => entries.Count;

        /// <summary>How many entries the storage holds before it has to grow.  For tests.</summary>
        internal int Capacity => entries.EnsureCapacity(0);

        /// <summary>True when nothing was loaded from disk, so every row this run sees is a first sighting.</summary>
        public bool IsFirstCollection => LastCollectionUtc == null;

        /// <summary>
        /// Throw the baseline away when the gap since the previous collection is longer than
        /// <paramref name="maxLookback"/>, and report whether it did.
        ///
        /// <para>A service that has been stopped for days leaves a baseline whose entries are days old.
        /// Diffing against them is worse than not diffing at all, for three reasons that compound: the
        /// source filter is driven by the last collection time, so the read returns everything executed
        /// since - effectively the whole plan cache; every plan compiled during the gap counts as compiled
        /// inside the interval, so its whole lifetime is taken as this interval's work; and the result is a
        /// single row stamped with a period of days, which any report window containing that one snapshot
        /// then attributes in full.  A gap should read as a gap, not as a spike.</para>
        ///
        /// <para>What is given up is the genuine accumulated delta for plans that stayed cached throughout.
        /// That is the right trade: there is no way to say when during those days the work happened, and
        /// this collection exists precisely so that a period is not misreported.  The interval's estimated share
        /// of the work is still counted, as unattributed, on the collection's own row.</para>
        /// </summary>
        public bool DiscardIfStale(DateTime nowUtc, TimeSpan maxLookback)
        {
            if (LastCollectionUtc == null) return false;
            if (nowUtc.Subtract(LastCollectionUtc.Value) <= maxLookback) return false;

            entries.Clear();
            LastCollectionUtc = null;
            ContinuousSinceUtc = null;
            EvictedThroughUtc = null;
            Evictions = 0;
            return true;
        }

        /// <summary>
        /// How long a gap <see cref="DiscardIfStale"/> tolerates: the configured lookback, but never less than three
        /// of the schedule's longest normal gaps.  A collection that runs hourly has an hour between collections by
        /// design, and a lookback shorter than that would discard the baseline on every run, so that nearly all of
        /// its work came out unattributed.  Three, so a couple of missed collections still produce a real delta.
        /// </summary>
        public static TimeSpan GetMaxLookback(TimeSpan configured, TimeSpan? scheduleInterval) =>
            scheduleInterval * 3 is { } scheduled && scheduled > configured ? scheduled : configured;

        public bool TryGet(BaselineKey key, out BaselineEntry entry) => entries.TryGetValue(key, out entry);

        public void Upsert(BaselineKey key, BaselineEntry entry) => entries[key] = entry;

        /// <summary>
        /// Whether a row compiled at <paramref name="creationTimeUtc"/> would be in the baseline already if it
        /// had finished any execution before the previous collection.
        ///
        /// <para>True when the baseline has been kept without a break since before the row was compiled, and
        /// nothing compiled that late has been trimmed out of it.  Every collection reads every row that has
        /// finished an execution since the one before, so a row missing from a baseline like that has not
        /// finished one - whatever its counters hold was done inside the current interval.  The case this
        /// exists for is a long execution that started before the previous read and finished after it.</para>
        /// </summary>
        public bool WouldHaveSeen(DateTime creationTimeUtc) =>
            ContinuousSinceUtc != null
            && creationTimeUtc >= ContinuousSinceUtc.Value
            && (EvictedThroughUtc == null || creationTimeUtc > EvictedThroughUtc.Value);

        /// <summary>
        /// Move the baseline on to the collection in <paramref name="result"/>: its rows become what the next
        /// collection diffs against, its snapshot the start of the next interval.
        /// </summary>
        public void Apply(QueryStatsResult result)
        {
            Evictions = 0;

            // Refresh what is already held first.  An update never grows the dictionary, and a refreshed entry
            // is the most recently seen, so the room made below never comes out of this collection's own rows.
            var adding = new List<KeyValuePair<BaselineKey, BaselineEntry>>();
            foreach (var update in result.PendingBaseline)
            {
                if (entries.ContainsKey(update.Key))
                {
                    entries[update.Key] = update.Value;
                }
                else
                {
                    adding.Add(update);
                }
            }

            // Room is made before adding rather than by trimming after.  Storage a dictionary grows into is never
            // given back, so going over the cap for a moment would cost the difference - double, as it grows -
            // for the life of the service.  For the same reason it grows no further than the cap needs.
            Evict(entries.Count + adding.Count - MaxEntries);
            if (adding.Count > MaxEntries)
            {
                // More new rows than the cap holds, from one collection: the surplus is evicted on arrival
                var surplus = adding.Count - MaxEntries;
                adding.RemoveRange(MaxEntries, surplus);
                Evictions += surplus;
                EvictedThroughUtc = new DateTime(result.SnapshotDateUtc.Ticks, DateTimeKind.Utc);
            }
            var capacity = entries.EnsureCapacity(0);
            var needed = entries.Count + adding.Count;
            if (needed > capacity)
            {
                entries.EnsureCapacity(Math.Max(needed, Math.Min(capacity * 2, MaxEntries)));
            }
            foreach (var add in adding)
            {
                entries[add.Key] = add.Value;
            }

            // An empty baseline starts keeping a complete record from this collection on
            ContinuousSinceUtc ??= result.SnapshotDateUtc;
            LastCollectionUtc = result.SnapshotDateUtc;
            LastReadDurationMs = result.ReadDurationMs;
        }

        /// <summary>
        /// Drop the least recently seen rows until the cap is respected.
        /// </summary>
        public void Trim()
        {
            Evictions = 0;
            Evict(entries.Count - MaxEntries);
        }

        /// <summary>
        /// Drop the <paramref name="count"/> least recently seen rows.  Finds where the cut falls by sorting the
        /// last sightings alone rather than the entries, which are large enough that copying all of them to sort
        /// would cost more than the rows being dropped.
        /// </summary>
        private void Evict(int count)
        {
            if (count <= 0 || entries.Count == 0) return;

            var seen = new long[entries.Count];
            var i = 0;
            foreach (var kv in entries)
            {
                seen[i++] = kv.Value.LastSeenTicks;
            }
            Array.Sort(seen);

            // Everything seen before the cut goes, and as many of those seen exactly at it as make up the count -
            // a whole collection's rows share one sighting, so ties are the usual case rather than the exception.
            var cut = seen[Math.Min(count, seen.Length) - 1];
            var before = Array.BinarySearch(seen, cut);
            while (before > 0 && seen[before - 1] == cut) before--;
            var atCut = count - before;
            var doomed = new List<BaselineKey>(Math.Min(count, seen.Length));
            foreach (var kv in entries)
            {
                if (kv.Value.LastSeenTicks < cut)
                {
                    doomed.Add(kv.Key);
                }
                else if (kv.Value.LastSeenTicks == cut && atCut > 0)
                {
                    doomed.Add(kv.Key);
                    atCut--;
                }
            }
            foreach (var key in doomed)
            {
                entries.Remove(key);
            }
            Evictions += doomed.Count;

            var evictedThrough = new DateTime(cut, DateTimeKind.Utc);
            if (EvictedThroughUtc == null || evictedThrough > EvictedThroughUtc.Value)
            {
                EvictedThroughUtc = evictedThrough;
            }
        }

        public void Save(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            // Written to a temporary file and moved into place: a baseline half-written by a process kill
            // would be read back as a corrupt file and discarded, costing the instance a whole interval of
            // attribution rather than nothing.
            var temp = path + ".tmp";
            using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write(FileFormatVersion);
                writer.Write(LastCollectionUtc?.Ticks ?? 0L);
                writer.Write(LastReadDurationMs);
                writer.Write(ContinuousSinceUtc?.Ticks ?? 0L);
                writer.Write(EvictedThroughUtc?.Ticks ?? 0L);
                writer.Write(entries.Count);
                foreach (var kv in entries)
                {
                    kv.Key.Write(writer);
                    var e = kv.Value;
                    writer.Write(e.ExecutionCount);
                    writer.Write(e.WorkerTime);
                    writer.Write(e.ElapsedTime);
                    writer.Write(e.LogicalReads);
                    writer.Write(e.LogicalWrites);
                    writer.Write(e.PhysicalReads);
                    writer.Write(e.ClrTime);
                    writer.Write(e.Rows);
                    writer.Write(e.Dop);
                    writer.Write(e.GrantKb);
                    writer.Write(e.UsedGrantKb);
                    writer.Write(e.Spills);
                    writer.Write(e.MaxElapsedTime);
                    writer.Write(e.CreationTimeUtc.Ticks);
                    writer.Write(e.PlanGenerationNum);
                    writer.Write(e.LastSeenTicks);
                }
            }

            File.Move(temp, path, true);
        }

        public static Baseline Load(string path, int maxEntries)
        {
            var baseline = new Baseline { MaxEntries = maxEntries };
            if (!File.Exists(path)) return baseline;

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fs);
            var version = reader.ReadInt32();
            if (version != FileFormatVersion)
            {
                // A format change costs one interval of attribution, which is cheaper than carrying a
                // reader for every past shape of the file.
                return baseline;
            }

            baseline.LastCollectionUtc = ReadDate(reader);
            baseline.LastReadDurationMs = reader.ReadInt32();
            baseline.ContinuousSinceUtc = ReadDate(reader);
            baseline.EvictedThroughUtc = ReadDate(reader);

            var count = reader.ReadInt32();
            // Sized once rather than doubled into, which would leave up to twice the storage behind
            baseline.entries.EnsureCapacity(count);
            for (var i = 0; i < count; i++)
            {
                var key = BaselineKey.Read(reader);
                var entry = new BaselineEntry
                {
                    ExecutionCount = reader.ReadInt64(),
                    WorkerTime = reader.ReadInt64(),
                    ElapsedTime = reader.ReadInt64(),
                    LogicalReads = reader.ReadInt64(),
                    LogicalWrites = reader.ReadInt64(),
                    PhysicalReads = reader.ReadInt64(),
                    ClrTime = reader.ReadInt64(),
                    Rows = reader.ReadInt64(),
                    Dop = reader.ReadInt64(),
                    GrantKb = reader.ReadInt64(),
                    UsedGrantKb = reader.ReadInt64(),
                    Spills = reader.ReadInt64(),
                    MaxElapsedTime = reader.ReadInt64(),
                    CreationTimeUtc = new DateTime(reader.ReadInt64(), DateTimeKind.Utc),
                    PlanGenerationNum = reader.ReadInt64(),
                    LastSeenTicks = reader.ReadInt64()
                };
                baseline.entries[key] = entry;
            }

            return baseline;
        }

        /// <summary>A date written as ticks, with zero standing for none.</summary>
        private static DateTime? ReadDate(BinaryReader reader)
        {
            var ticks = reader.ReadInt64();
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }
    }
}
