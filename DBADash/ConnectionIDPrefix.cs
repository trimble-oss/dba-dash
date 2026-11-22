using Amazon.S3.Util;
using DBADash.Messaging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static DBADash.DBADashConnection;

namespace DBADash
{
    /// <summary>
    /// A prefix a service adds to the ConnectionID of everything it imports from an S3 or folder source
    /// (<see cref="DBADashSource.ConnectionIDPrefix"/>).  Remote services that write to different paths - one per
    /// customer, say - can then collect instances with the same ConnectionID without their data being merged into one
    /// instance in the repository.  The remote services, and any repository of their own, keep the ConnectionIDs they have.
    ///
    /// <para>Only the importing service knows the prefix, so it also translates the messages it relays from the GUI to a
    /// remote service: the prefix is removed from the ConnectionIDs in the message on the way out, and added to the
    /// ConnectionIDs in the reply on the way back.  The remote service is matched to its source by the S3 path it writes
    /// to (dbo.DBADashAgent.S3Path).  A remote service that writes to a folder has no S3 path, so messages for its
    /// instances can't be relayed.</para>
    /// </summary>
    public static class ConnectionIDPrefix
    {
        public const int MaxLength = 64;

        /// <summary>
        /// Added to the DBADash table of a DataSet the prefix has been applied to, holding the prefix.  A DataSet that
        /// has it isn't prefixed again - e.g. a file written to the failed message folder after it was prefixed.
        /// </summary>
        public const string AppliedColumnName = "ConnectionIDPrefix";

        /// <summary>dbo.Instances.ConnectionID is sysname</summary>
        private const int MaxConnectionIDLength = 128;

        private const string ConnectionIDName = "ConnectionID";
        private const string ConnectionIDsName = "ConnectionIDs";

        /// <summary>Only data imported from S3 or a folder can be prefixed.  A SQL source's ConnectionID can be set directly.</summary>
        public static bool IsSupported(ConnectionType type) => type is ConnectionType.AWSS3 or ConnectionType.Directory;

        /// <summary>Why the prefix can't be used, or null if it can.  No prefix is valid.</summary>
        public static string Validate(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return null;
            if (prefix.Trim() != prefix) return "The ConnectionID prefix can't start or end with a space.";
            if (prefix.Length > MaxLength) return $"The ConnectionID prefix can't be longer than {MaxLength} characters.";
            return null;
        }

        /// <summary>The ConnectionID with the prefix added.  Throws if the result is too long to be a ConnectionID.</summary>
        public static string Add(string connectionID, string prefix)
        {
            var prefixed = prefix + connectionID;
            if (prefixed.Length > MaxConnectionIDLength)
            {
                throw new InvalidOperationException($"ConnectionID {prefixed} is longer than {MaxConnectionIDLength} characters with the prefix {prefix} added.");
            }
            return prefixed;
        }

        /// <summary>
        /// Adds the prefix to every ConnectionID in the DataSet: the ConnectionID columns of its tables, and the ConnectionIDs
        /// in the JSON payload of a connection change reply (<see cref="ConnectionChanges.ToDataSet"/>).  Nothing is changed
        /// if any of them would be too long, so a DataSet is never left partly prefixed.
        /// </summary>
        public static void Apply(DataSet ds, string prefix)
        {
            if (ds == null || string.IsNullOrEmpty(prefix)) return;
            var dbadash = ds.Tables["DBADash"];
            if (dbadash != null && dbadash.Columns.Contains(AppliedColumnName)) return;

            // Work out every change before making any
            var changes = new List<Action>();
            foreach (DataTable dt in ds.Tables)
            {
                if (dt.Columns[ConnectionIDName] is { } col && col.DataType == typeof(string))
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row[col] is string id && id.Length > 0)
                        {
                            var prefixed = Add(id, prefix);
                            changes.Add(() => row[col] = prefixed);
                        }
                    }
                }
                if (dt.TableName == ConnectionChanges.PayloadTableName && dt.Columns["Json"] is { } jsonCol)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row[jsonCol] is not string json || string.IsNullOrWhiteSpace(json)) continue;
                        var token = JToken.Parse(json);
                        RewriteJson(token, id => Add(id, prefix));
                        var rewritten = token.ToString(Newtonsoft.Json.Formatting.None);
                        changes.Add(() => row[jsonCol] = rewritten);
                    }
                }
            }

            changes.ForEach(change => change());
            if (dbadash != null)
            {
                dbadash.Columns.Add(AppliedColumnName, typeof(string));
                foreach (DataRow row in dbadash.Rows)
                {
                    row[AppliedColumnName] = prefix;
                }
            }
        }

        /// <summary>
        /// The message to relay to the remote service it's for, with the prefix of that service's source removed from its
        /// ConnectionIDs.  Throws if the message names an instance that isn't imported through that source, or names a
        /// prefixed instance when the remote service can't be matched to a source - the remote service would otherwise be
        /// asked about an instance it doesn't know by that ConnectionID.  The message is returned as it is if no source has
        /// a prefix.
        /// </summary>
        /// <param name="msg">The message, with the remote service's registered S3 path in <see cref="MessageBase.CollectAgent"/></param>
        /// <param name="sources">The relaying service's sources</param>
        public static MessageBase PrepareForRelay(MessageBase msg, IReadOnlyCollection<DBADashSource> sources)
        {
            var prefixes = ConfiguredPrefixes(sources);
            if (prefixes.Count == 0) return msg;

            var prefix = ForS3Path(sources, msg.CollectAgent?.S3Path);
            var json = JObject.Parse(msg.SerializeString());
            if (prefix == null)
            {
                var prefixed = GetConnectionIDs(json).FirstOrDefault(id => prefixes.Any(p => id.StartsWith(p, StringComparison.OrdinalIgnoreCase)));
                if (prefixed != null)
                {
                    throw new InvalidOperationException($"Unable to relay the message for {prefixed}.  It's imported from a source with a ConnectionID prefix, but the remote service {msg.CollectAgent?.AgentHostName} doesn't write to an S3 path that one of those sources reads from.");
                }
                return msg;
            }

            RewriteJson(json, id => id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && id.Length > prefix.Length
                ? id[prefix.Length..]
                : throw new InvalidOperationException($"Unable to relay the message for {id}.  The remote service {msg.CollectAgent.AgentHostName} is imported with the ConnectionID prefix {prefix}, which {id} doesn't have."));
            return (MessageBase)json.ToObject(msg.GetType());
        }

        /// <summary>
        /// The prefix of the S3 source that reads from the S3 path, or null if there isn't one or it has no prefix.  Throws
        /// if more than one source reads from the path and they have different prefixes, as which to use is ambiguous.
        /// </summary>
        public static string ForS3Path(IEnumerable<DBADashSource> sources, string s3Path)
        {
            if (!TryParseS3Location(s3Path, out var location)) return null;
            var prefixes = sources
                .Where(s => s.SourceConnection?.Type == ConnectionType.AWSS3
                            && TryParseS3Location(s.SourceConnection.ConnectionString, out var sourceLocation)
                            && sourceLocation == location)
                .Select(s => s.ConnectionIDPrefix)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            return prefixes.Count switch
            {
                0 => null,
                1 => prefixes[0],
                _ => throw new InvalidOperationException($"More than one source reads from {s3Path} and they have different ConnectionID prefixes.")
            };
        }

        /// <summary>The distinct prefixes of the sources.  Empty if none has one - the common case, where nothing needs translating.</summary>
        public static List<string> ConfiguredPrefixes(IEnumerable<DBADashSource> sources) =>
            sources.Select(s => s.ConnectionIDPrefix)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct(StringComparer.Ordinal)
                .ToList();

        /// <summary>
        /// Replaces every ConnectionID in a message or reply payload: the values of properties named ConnectionID, and the
        /// items of properties named ConnectionIDs, at any depth.  Names are matched ignoring case, as they are when the
        /// message is deserialized.
        /// </summary>
        internal static void RewriteJson(JToken token, Func<string, string> map)
        {
            if (token is not JContainer container) return;
            foreach (var property in container.DescendantsAndSelf().OfType<JProperty>().ToList())
            {
                if (property.Name.Equals(ConnectionIDName, StringComparison.OrdinalIgnoreCase) && property.Value.Type == JTokenType.String)
                {
                    property.Value = map((string)property.Value);
                }
                else if (property.Name.Equals(ConnectionIDsName, StringComparison.OrdinalIgnoreCase) && property.Value is JArray items)
                {
                    foreach (var item in items.Where(i => i.Type == JTokenType.String).ToList())
                    {
                        item.Replace(map((string)item));
                    }
                }
            }
        }

        private static List<string> GetConnectionIDs(JToken token)
        {
            var ids = new List<string>();
            RewriteJson(token, id =>
            {
                ids.Add(id);
                return id;
            });
            return ids;
        }

        /// <summary>
        /// The endpoint, bucket and key prefix of an S3 path, so the same location written differently compares equal: an
        /// s3:// URI and an AWS https URL, a trailing slash, or the case of the host and bucket.
        /// </summary>
        internal static bool TryParseS3Location(string path, out (string Endpoint, string Bucket, string Key) location)
        {
            location = default;
            if (string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                if (AmazonS3Uri.TryParseAmazonS3Uri(path, out var s3Uri) && !string.IsNullOrEmpty(s3Uri.Bucket))
                {
                    location = ("aws", s3Uri.Bucket.ToLowerInvariant(), (s3Uri.Key ?? string.Empty).Trim('/'));
                    return true;
                }
                // S3 compatible storage, e.g. MinIO - https://host:port/bucket[/prefix]
                var uri = new Uri(path, UriKind.Absolute);
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0) return false;
                location = (uri.Authority.ToLowerInvariant(), segments[0].ToLowerInvariant(),
                    Uri.UnescapeDataString(string.Join('/', segments.Skip(1))));
                return true;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }
    }
}
