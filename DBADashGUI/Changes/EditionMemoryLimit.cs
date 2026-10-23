using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace DBADashGUI
{
    /// <summary>
    /// Buffer-pool memory cap for a Database Engine edition and version.
    /// Mirrors dbo.EditionMemoryLimit so the Hardware grid can color rows before that function is deployed.
    /// </summary>
    internal static class EditionMemoryLimit
    {
        public const long MaxServerMemoryNotConfiguredMb = 2147483647;

        internal readonly record struct Result(DBADashStatus.DBADashStatusEnum Status, string Message);

        private static readonly (string Prefix, int Major, bool Is2008R2)[] Versions =
        {
            ("SQL 2008 R2 ", 10, true),
            ("SQL 2005 ", 9, false),
            ("SQL 2008 ", 10, false),
            ("SQL 2012 ", 11, false),
            ("SQL 2014 ", 12, false),
            ("SQL 2016 ", 13, false),
            ("SQL 2017 ", 14, false),
            ("SQL 2019 ", 15, false),
            ("SQL 2022 ", 16, false),
            ("SQL 2025 ", 17, false),
        };

        public static Result? EvaluateMaxServerMemoryRow(DataRowView row)
        {
            if (!TryGetInt64(row, "BufferPoolMB", out var maxMb)) return null;
            decimal? physicalGb = TryGetDecimal(row, "PhysicalMemoryGB", out var physical) ? physical : null;
            int? limitMb = null;
            if (row.Row.Table.Columns.Contains("SQLVersion"))
            {
                limitMb = BufferPoolLimitFromVersion(Convert.ToString(row["SQLVersion"]));
            }

            return EvaluateMaxServerMemory(physicalGb, maxMb, limitMb);
        }

        /// <summary>
        /// Warning when max server memory is not configured or is above physical memory.
        /// Information when it is half of physical memory or less, unless it is already near the edition buffer pool cap.
        /// A low value is only informational: instance stacking and other applications are valid reasons.
        /// </summary>
        public static Result? EvaluateMaxServerMemory(decimal? physicalGb, long maxMb, int? editionLimitMb)
        {
            if (maxMb >= MaxServerMemoryNotConfiguredMb)
            {
                return new Result(DBADashStatus.DBADashStatusEnum.Warning,
                    "Max server memory has not been configured. The default of 2,147,483,647 MB lets SQL Server consume memory up to the edition limit. Leave RAM for the operating system.");
            }

            if (physicalGb is > 0)
            {
                var physicalMb = physicalGb.Value * 1024m;
                var maxText = FormatMb((int)Math.Min(maxMb, int.MaxValue));
                var physicalText = physicalGb.Value.ToString("N1", CultureInfo.CurrentCulture) + " GB";
                if (maxMb > physicalMb + 512m)
                {
                    return new Result(DBADashStatus.DBADashStatusEnum.Warning,
                        "Max server memory is set to " + maxText + ", which is above the " + physicalText + " of physical memory. SQL Server cannot use more memory than the server has, so this setting does not reserve RAM for the operating system.");
                }

                if (physicalGb >= 16 && maxMb <= physicalMb / 2m && !IsNearEditionCap(maxMb, editionLimitMb))
                {
                    var pct = (maxMb / physicalMb).ToString("P0", CultureInfo.CurrentCulture);
                    return new Result(DBADashStatus.DBADashStatusEnum.Information,
                        "Max server memory is set to " + maxText + " of " + physicalText + " physical memory (" + pct + "). That is half of the installed RAM or less. A lower setting can be appropriate when several instances share the server or other applications need memory.");
                }
            }

            return null;
        }

        public static Result? EvaluateRow(DataRowView row)
        {
            if (TryGetInt32(row, "PhysicalMemoryLimitStatus", out var status)
                && Enum.IsDefined(typeof(DBADashStatus.DBADashStatusEnum), status))
            {
                var message = row.Row.Table.Columns.Contains("PhysicalMemoryLimitMessage")
                    ? Convert.ToString(row["PhysicalMemoryLimitMessage"])
                    : null;
                return new Result((DBADashStatus.DBADashStatusEnum)status, message ?? string.Empty);
            }

            if (!TryGetDecimal(row, "PhysicalMemoryGB", out var physicalGb)) return null;
            var sqlVersion = row.Row.Table.Columns.Contains("SQLVersion") ? Convert.ToString(row["SQLVersion"]) : null;
            TryGetInt64(row, "BufferPoolMB", out var maxMb);
            var hasMax = row.Row.Table.Columns.Contains("BufferPoolMB") && row["BufferPoolMB"].DBNullToNull() != null;
            return Evaluate(sqlVersion, physicalGb, hasMax ? maxMb : null);
        }

        public static Result Evaluate(string sqlVersion, decimal physicalGb, long? maxServerMemoryMb)
        {
            if (string.IsNullOrWhiteSpace(sqlVersion) || sqlVersion.StartsWith("Azure ", StringComparison.OrdinalIgnoreCase))
            {
                return new Result(DBADashStatus.DBADashStatusEnum.NA,
                    "Memory limits are not evaluated for Azure SQL, or the edition and version are unknown. Azure SQL capacity is determined by the service tier.");
            }

            int major = 0;
            var is2008R2 = false;
            string rest = null;
            foreach (var version in Versions)
            {
                if (!sqlVersion.StartsWith(version.Prefix, StringComparison.OrdinalIgnoreCase)) continue;
                major = version.Major;
                is2008R2 = version.Is2008R2;
                rest = sqlVersion[version.Prefix.Length..];
                break;
            }

            if (rest == null)
            {
                return new Result(DBADashStatus.DBADashStatusEnum.NA,
                    "Edition or version is unknown, so the buffer pool memory limit was not evaluated.");
            }

            var tokens = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            var hasServicePack = tokens.Any(t => t.StartsWith("SP", StringComparison.OrdinalIgnoreCase) && t.Length > 2 && t[2..].All(char.IsDigit));
            while (tokens.Count > 0 && IsVersionToken(tokens[^1]))
            {
                tokens.RemoveAt(tokens.Count - 1);
            }

            var edition = string.Join(' ', tokens);
            var limitMb = BufferPoolLimitMb(edition, major, is2008R2);
            var extended = major > 13 || (major == 13 && hasServicePack);
            var label = EditionLabel(edition);
            var physicalText = physicalGb.ToString("N1", CultureInfo.CurrentCulture) + " GB";
            var maxText = MaxServerMemoryText(maxServerMemoryMb, limitMb, physicalGb);

            if (limitMb == null && !IsKnownEdition(edition))
            {
                return new Result(DBADashStatus.DBADashStatusEnum.NA,
                    "Edition or version is unknown, so the buffer pool memory limit was not evaluated.");
            }

            if (limitMb == null)
            {
                return new Result(DBADashStatus.DBADashStatusEnum.OK,
                    label + " does not impose a buffer pool memory limit. Physical memory is " + physicalText + "." + Environment.NewLine + Environment.NewLine + maxText);
            }

            var physicalMb = physicalGb * 1024m;
            var headroomMb = Math.Max(limitMb.Value * 0.1m, 4096m);
            var limitText = FormatMb(limitMb.Value);
            if (physicalMb <= limitMb.Value)
            {
                return new Result(DBADashStatus.DBADashStatusEnum.OK,
                    "Physical memory is " + physicalText + ", within the " + label + " buffer pool limit of " + limitText + "." + Environment.NewLine + Environment.NewLine + maxText);
            }

            var slight = physicalMb <= limitMb.Value + headroomMb;
            var status = slight ? DBADashStatus.DBADashStatusEnum.Information : DBADashStatus.DBADashStatusEnum.Warning;
            var extra = extended ? ExtendedMemoryText(edition) : string.Empty;
            var message = "Physical memory is " + physicalText + ", " + (slight ? "slightly above" : "above")
                + " the " + label + " buffer pool limit of " + limitText + " for this version of SQL Server."
                + (slight ? " The difference is in the range often left for the operating system." : string.Empty)
                + Environment.NewLine + Environment.NewLine
                + "The Database Engine will not use the memory above that limit for the buffer pool."
                + extra
                + Environment.NewLine + Environment.NewLine
                + maxText
                + Environment.NewLine + Environment.NewLine
                + "The limit applies to this instance. Other SQL Server instances on the same server have their own limits.";
            return new Result(status, message);
        }

        private static int? BufferPoolLimitMb(string edition, int major, bool is2008R2)
        {
            if (edition.Contains("Express", StringComparison.OrdinalIgnoreCase))
                return major >= 13 ? 1410 : 1024;
            if (edition.StartsWith("Web Edition", StringComparison.OrdinalIgnoreCase))
                return is2008R2 || major >= 11 ? 65536 : null;
            if (edition.Contains("Business Intelligence", StringComparison.OrdinalIgnoreCase))
                return major <= 11 ? 65536 : 131072;
            if (edition.Contains("Workgroup", StringComparison.OrdinalIgnoreCase))
                return major <= 9 ? 3072 : 4096;
            if (edition.Contains("Standard", StringComparison.OrdinalIgnoreCase))
            {
                if (major >= 17) return 262144;
                if (major >= 12) return 131072;
                if (major == 11 || is2008R2) return 65536;
                return null;
            }

            if (is2008R2 && edition.StartsWith("Enterprise Edition", StringComparison.OrdinalIgnoreCase))
                return 2097152;
            return null;
        }

        private static bool IsKnownEdition(string edition)
        {
            return edition.Contains("Express", StringComparison.OrdinalIgnoreCase)
                || edition.StartsWith("Web Edition", StringComparison.OrdinalIgnoreCase)
                || edition.Contains("Business Intelligence", StringComparison.OrdinalIgnoreCase)
                || edition.Contains("Workgroup", StringComparison.OrdinalIgnoreCase)
                || edition.Contains("Standard", StringComparison.OrdinalIgnoreCase)
                || edition.Contains("Enterprise", StringComparison.OrdinalIgnoreCase)
                || edition.Contains("Developer", StringComparison.OrdinalIgnoreCase)
                || edition.Contains("Evaluation", StringComparison.OrdinalIgnoreCase)
                || edition.Contains("Datacenter", StringComparison.OrdinalIgnoreCase);
        }

        private static string EditionLabel(string edition)
        {
            if (edition.Contains("Express", StringComparison.OrdinalIgnoreCase)) return "Express edition";
            if (edition.StartsWith("Web Edition", StringComparison.OrdinalIgnoreCase)) return "Web edition";
            if (edition.Contains("Business Intelligence", StringComparison.OrdinalIgnoreCase)) return "Business Intelligence edition";
            if (edition.Contains("Workgroup", StringComparison.OrdinalIgnoreCase)) return "Workgroup edition";
            if (edition.Contains("Standard", StringComparison.OrdinalIgnoreCase) && edition.Contains("Developer", StringComparison.OrdinalIgnoreCase))
                return "Standard Developer edition";
            if (edition.Contains("Standard", StringComparison.OrdinalIgnoreCase)) return "Standard edition";
            if (edition.Contains("Datacenter", StringComparison.OrdinalIgnoreCase)) return "Datacenter edition";
            if (edition.Contains("Developer", StringComparison.OrdinalIgnoreCase)) return "Developer edition";
            if (edition.Contains("Evaluation", StringComparison.OrdinalIgnoreCase)) return "Evaluation edition";
            if (edition.Contains("Enterprise", StringComparison.OrdinalIgnoreCase)) return "Enterprise edition";
            return "This edition";
        }

        private static string ExtendedMemoryText(string edition)
        {
            int? columnstore = null;
            int? inMemory = null;
            if (edition.Contains("Standard", StringComparison.OrdinalIgnoreCase))
            {
                columnstore = 32768;
                inMemory = 32768;
            }
            else if (edition.StartsWith("Web Edition", StringComparison.OrdinalIgnoreCase))
            {
                columnstore = 16384;
            }
            else if (edition.Contains("Express", StringComparison.OrdinalIgnoreCase))
            {
                columnstore = 352;
                inMemory = 352;
            }

            if (columnstore == null) return string.Empty;
            var text = " Columnstore segment cache (up to " + FormatMb(columnstore.Value) + ")";
            if (inMemory != null)
            {
                text += " and memory-optimized data (up to " + FormatMb(inMemory.Value) + ")";
            }

            return text + " can use additional memory.";
        }

        private static string MaxServerMemoryText(long? maxServerMemoryMb, int? limitMb, decimal physicalGb)
        {
            var setting = EvaluateMaxServerMemory(physicalGb > 0 ? physicalGb : null, maxServerMemoryMb ?? 0, limitMb);
            if (maxServerMemoryMb == null) return "Max server memory is not available.";
            if (setting != null && (maxServerMemoryMb >= MaxServerMemoryNotConfiguredMb || maxServerMemoryMb > physicalGb * 1024m + 512m || (physicalGb >= 16 && maxServerMemoryMb <= physicalGb * 1024m / 2m)))
                return setting.Value.Message;

            var text = "Max server memory is set to " + FormatMb((int)Math.Min(maxServerMemoryMb.Value, int.MaxValue));
            if (limitMb == null) return text + ".";
            if (maxServerMemoryMb <= limitMb)
                return text + ", which is within the edition buffer pool limit. Max server memory does not raise that cap.";
            return text + ". That is above the buffer pool limit, which is valid: from SQL Server 2012, max server memory also covers clerks outside the buffer pool.";
        }

        private static int? BufferPoolLimitFromVersion(string sqlVersion)
        {
            if (!TryReadEdition(sqlVersion, out var major, out var is2008R2, out var edition)) return null;
            return BufferPoolLimitMb(edition, major, is2008R2);
        }

        private static bool TryReadEdition(string sqlVersion, out int major, out bool is2008R2, out string edition)
        {
            major = 0;
            is2008R2 = false;
            edition = string.Empty;
            if (string.IsNullOrWhiteSpace(sqlVersion)) return false;
            string rest = null;
            foreach (var version in Versions)
            {
                if (!sqlVersion.StartsWith(version.Prefix, StringComparison.OrdinalIgnoreCase)) continue;
                major = version.Major;
                is2008R2 = version.Is2008R2;
                rest = sqlVersion[version.Prefix.Length..];
                break;
            }

            if (rest == null) return false;
            var tokens = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            while (tokens.Count > 0 && IsVersionToken(tokens[^1]))
            {
                tokens.RemoveAt(tokens.Count - 1);
            }

            edition = string.Join(' ', tokens);
            return IsKnownEdition(edition);
        }

        private static bool IsNearEditionCap(long maxMb, int? limitMb)
        {
            if (limitMb == null) return false;
            return maxMb >= limitMb.Value * 0.8m;
        }

        private static string FormatMb(int mb)
        {
            if (mb >= 1048576) return (mb / 1048576m).ToString("N0", CultureInfo.CurrentCulture) + " TB";
            if (mb >= 10240) return (mb / 1024m).ToString("N0", CultureInfo.CurrentCulture) + " GB";
            if (mb >= 1024) return (mb / 1024m).ToString("N1", CultureInfo.CurrentCulture) + " GB";
            return mb.ToString("N0", CultureInfo.CurrentCulture) + " MB";
        }

        private static bool IsVersionToken(string token)
        {
            if (token.Equals("RTM", StringComparison.OrdinalIgnoreCase) || token.Equals("GDR", StringComparison.OrdinalIgnoreCase))
                return true;
            if (token.Length < 3) return false;
            var prefix = token[..2];
            if (prefix.Equals("CU", StringComparison.OrdinalIgnoreCase) || prefix.Equals("SP", StringComparison.OrdinalIgnoreCase))
                return token[2..].All(char.IsDigit);
            return token.StartsWith("CTP", StringComparison.OrdinalIgnoreCase) && (token.Length == 3 || token[3..].All(char.IsDigit));
        }

        private static bool TryGetDecimal(DataRowView row, string column, out decimal value)
        {
            value = 0;
            if (!row.Row.Table.Columns.Contains(column)) return false;
            var raw = row[column].DBNullToNull();
            switch (raw)
            {
                case decimal d: value = d; return true;
                case double d: value = (decimal)d; return true;
                case float f: value = (decimal)f; return true;
                case int i: value = i; return true;
                case long l: value = l; return true;
                case null: return false;
                default:
                    return decimal.TryParse(Convert.ToString(raw, CultureInfo.CurrentCulture), NumberStyles.Any, CultureInfo.CurrentCulture, out value)
                        || decimal.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            }
        }

        private static bool TryGetInt64(DataRowView row, string column, out long value)
        {
            value = 0;
            if (!TryGetDecimal(row, column, out var parsed)) return false;
            value = (long)parsed;
            return true;
        }

        private static bool TryGetInt32(DataRowView row, string column, out int value)
        {
            value = 0;
            if (!TryGetDecimal(row, column, out var parsed)) return false;
            value = (int)parsed;
            return true;
        }
    }
}
