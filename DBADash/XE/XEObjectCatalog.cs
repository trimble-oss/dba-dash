using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADash.XE
{
    /// <summary>
    /// The extended-events objects available on a monitored instance: the events that can be captured, each event's
    /// data columns (fields), and the global actions.  Queried per instance (it varies by SQL Server version/edition)
    /// and cached by the GUI.  Drives the event picker, the per-event filter field list, and validation in
    /// <see cref="XETraceDefinition"/>.
    /// </summary>
    public sealed class XEObjectCatalog
    {
        public List<XEEventInfo> Events { get; set; } = new();

        /// <summary>
        /// Global predicate sources (object_type = 'pred_source') - the fields usable in the WHERE predicate for any
        /// event (e.g. session_id, database_id, client_app_name).  These are what a filter may reference, not actions
        /// (actions are for output capture only and are invalid as predicate sources).
        /// </summary>
        public List<XEFieldInfo> PredSources { get; set; } = new();

        /// <summary>
        /// Global actions (object_type = 'action') - the fields that can be captured on every event (the "global
        /// fields"), referenced as <c>[package].[name]</c> in the <c>ACTION(...)</c> clause.  Drives the global-fields
        /// picker.  May be empty if collected by an older service that didn't return actions.
        /// </summary>
        public List<XEFieldInfo> Actions { get; set; } = new();

        /// <summary>
        /// The predicate comparators (object_type = 'pred_compare') available on the instance, mapping comparator name
        /// (e.g. <c>equal_unicode_string</c>) to its owning package.  The comparator set - and which package owns
        /// it - varies by SQL Server version/edition (a database-scoped/Azure session in particular can differ), so the
        /// UI checks this before offering an option that depends on a specific comparator and the DDL references the
        /// comparator by its real package rather than assuming <c>package0</c>.  May be empty when collected by an older
        /// service that didn't return the set - see <see cref="SupportsComparator"/>.
        /// </summary>
        public Dictionary<string, string> Comparators { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Whether a comparator can be used on this instance - a strict membership test against the enumerated set.
        /// It must be strict (not optimistic when the set is empty) because the package a comparator lives in isn't
        /// fixed, so without the enumerated set we can't build a correct <c>[package].[comparator]</c> reference and
        /// must not offer the dependent option.  An older service that doesn't return the set therefore won't offer it.
        /// </summary>
        public bool SupportsComparator(string name) =>
            !string.IsNullOrEmpty(name) && Comparators.ContainsKey(name);

        /// <summary>
        /// The package owning a comparator, for the <c>[package].[comparator](...)</c> DDL reference.  Falls back to
        /// <c>package0</c> only when the comparator isn't in the enumerated set; callers gate on
        /// <see cref="SupportsComparator"/> first, so in practice the real package is always returned here.
        /// </summary>
        public string ComparatorPackage(string name) =>
            name != null && Comparators.TryGetValue(name, out var pkg) && !string.IsNullOrEmpty(pkg) ? pkg : "package0";

        /// <summary>
        /// Finds an event by name.  An event name can exist in more than one package (e.g. both
        /// <c>sqlserver.error_reported</c> and <c>xesvlpkg.error_reported</c>), so the <c>sqlserver</c> package is
        /// preferred - that's the one the ad-hoc trace built-ins and pickers mean.
        /// </summary>
        public XEEventInfo FindEvent(string name) =>
            Events.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase) &&
                                       string.Equals(e.Package, "sqlserver", StringComparison.OrdinalIgnoreCase))
            ?? Events.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

        private static string EventKey(string package, string name) =>
            $"{package}|{name}";

        /// <summary>Numeric XE type names (used to decide numeric vs string predicate handling).</summary>
        public static bool IsNumericType(string typeName) => typeName switch
        {
            "uint64" or "int64" or "uint32" or "int32" or "uint16" or "int16" or "uint8" or "int8"
                or "float32" or "float64" => true,
            _ => false
        };

        /// <summary>
        /// Builds the catalog from the three result sets returned by <see cref="Messaging.XEObjectCatalogMessage"/>
        /// (Events, EventFields, Actions).
        /// </summary>
        public static XEObjectCatalog FromDataSet(DataSet ds)
        {
            var catalog = new XEObjectCatalog();
            if (ds == null) return catalog;

            // Key by package+name: an event name can appear in several packages (e.g. sqlserver.error_reported and
            // xesvlpkg.error_reported), and each has its own data columns.  Keying by name alone would attach every
            // package's columns to a single instance and leave the others empty.
            var eventsByKey = new Dictionary<string, XEEventInfo>(StringComparer.OrdinalIgnoreCase);
            if (ds.Tables.Contains("Events"))
            {
                foreach (DataRow r in ds.Tables["Events"].Rows)
                {
                    var evt = new XEEventInfo
                    {
                        Package = r["package_name"] as string,
                        Name = r["event_name"] as string,
                        Description = r["description"] as string
                    };
                    catalog.Events.Add(evt);
                    if (!string.IsNullOrEmpty(evt.Name)) eventsByKey[EventKey(evt.Package, evt.Name)] = evt;
                }
            }

            if (ds.Tables.Contains("EventFields"))
            {
                var hasPackage = ds.Tables["EventFields"].Columns.Contains("package_name");
                foreach (DataRow r in ds.Tables["EventFields"].Rows)
                {
                    var eventName = r["event_name"] as string;
                    if (eventName == null) continue;
                    // Older services didn't return the package with each field; fall back to a name-only match.
                    var evt = hasPackage
                        ? (eventsByKey.TryGetValue(EventKey(r["package_name"] as string, eventName), out var e) ? e : null)
                        : catalog.Events.FirstOrDefault(x => string.Equals(x.Name, eventName, StringComparison.OrdinalIgnoreCase));
                    if (evt == null) continue;
                    evt.Fields.Add(new XEFieldInfo
                    {
                        Name = r["field_name"] as string,
                        IsAction = false,
                        TypeName = r["type_name"] as string,
                        IsNumeric = IsNumericType(r["type_name"] as string)
                    });
                }
            }

            if (ds.Tables.Contains("PredSources"))
            {
                foreach (DataRow r in ds.Tables["PredSources"].Rows)
                {
                    catalog.PredSources.Add(new XEFieldInfo
                    {
                        Name = r["field_name"] as string,
                        Package = r["package_name"] as string,
                        IsAction = true, // referenced as [package].[name], same as an action reference
                        TypeName = r["type_name"] as string,
                        IsNumeric = IsNumericType(r["type_name"] as string)
                    });
                }
            }

            if (ds.Tables.Contains("Customizations"))
            {
                foreach (DataRow r in ds.Tables["Customizations"].Rows)
                {
                    var eventName = r["event_name"] as string;
                    if (eventName == null) continue;
                    if (!eventsByKey.TryGetValue(EventKey(r["package_name"] as string, eventName), out var evt)) continue;
                    evt.Customizations.Add(new XECustomizableFieldInfo
                    {
                        Name = r["field_name"] as string,
                        TypeName = r["type_name"] as string,
                        DefaultValue = r["default_value"] as string
                    });
                }
            }

            if (ds.Tables.Contains("Actions"))
            {
                foreach (DataRow r in ds.Tables["Actions"].Rows)
                {
                    catalog.Actions.Add(new XEFieldInfo
                    {
                        Name = r["field_name"] as string,
                        Package = r["package_name"] as string,
                        IsAction = true,
                        IsNumeric = IsNumericType(r["type_name"] as string)
                    });
                }
            }

            if (ds.Tables.Contains("Comparators"))
            {
                foreach (DataRow r in ds.Tables["Comparators"].Rows)
                {
                    var name = r["comparator_name"] as string;
                    if (!string.IsNullOrEmpty(name)) catalog.Comparators[name] = r["package_name"] as string;
                }
            }

            return catalog;
        }
    }

    public sealed class XEEventInfo
    {
        public string Package { get; set; } = "sqlserver";
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>The event's data columns (payload) - always captured, not selectable (informational).</summary>
        public List<XEFieldInfo> Fields { get; set; } = new();

        /// <summary>
        /// The event's customizable columns (<c>column_type = 'customizable'</c>) - the optional collection toggles
        /// (e.g. <c>collect_statement</c>) that can be turned on/off per event via <c>SET</c> in the DDL.
        /// </summary>
        public List<XECustomizableFieldInfo> Customizations { get; set; } = new();

        public override string ToString() => Name;
    }

    /// <summary>
    /// A customizable column of an event - an optional collection toggle set via <c>SET name=(value)</c> in the
    /// <c>ADD EVENT</c> clause.  In practice these are boolean <c>collect_*</c> switches with a default state.
    /// </summary>
    public sealed class XECustomizableFieldInfo
    {
        public string Name { get; set; }
        public string TypeName { get; set; }

        /// <summary>The column's default value as reported by <c>sys.dm_xe_object_columns.column_value</c>.</summary>
        public string DefaultValue { get; set; }

        public string Description { get; set; }

        public bool IsBoolean => string.Equals(TypeName, "boolean", StringComparison.OrdinalIgnoreCase);

        /// <summary>The default on/off state for a boolean customizable column.</summary>
        public bool DefaultOn =>
            string.Equals(DefaultValue, "true", StringComparison.OrdinalIgnoreCase) || DefaultValue == "1";

        public override string ToString() => Name;
    }

    public sealed class XEFieldInfo
    {
        public string Name { get; set; }
        public string Package { get; set; }
        /// <summary>true = global action (referenced as <c>[package].[name]</c>); false = event data column (<c>[name]</c>).</summary>
        public bool IsAction { get; set; }
        public bool IsNumeric { get; set; }

        /// <summary>The XE type name (e.g. "unicode_string", "int64").  Drives the offer of case-insensitive matching.</summary>
        public string TypeName { get; set; }

        /// <summary>true for a unicode string field - the only fields we offer XE's case-insensitive comparators for.</summary>
        public bool IsUnicodeString => string.Equals(TypeName, "unicode_string", StringComparison.OrdinalIgnoreCase);

        public override string ToString() => Name;
    }
}
