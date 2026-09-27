using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// Reads the deadlock graph XML back out of SSMS's own deadlock viewer control.
    ///
    /// Unlike the plan viewer, SSMS's deadlock control (Microsoft.SqlServer.Management.SqlMgmt.
    /// Deadlock.DeadlockControl, confirmed directly against the SSMS 22 install by decoding what
    /// EditorFactoryDeadlock.CreateEditorInstance actually constructs) doesn't keep a "get me the XML"
    /// method at all - it parses the .xdl once into a private field of a plain, XmlSerializer-shaped
    /// object model (Microsoft.SqlServer.Management.SqlMgmt.Deadlock.deadlock, [XmlRoot(Namespace = "",
    /// IsNullable = false)], confirmed by reserializing a live instance and checking it round-trips to
    /// a well-formed &lt;deadlock&gt; document) and keeps only that.  So there's nothing to call - the
    /// XML is reconstructed the same way anything would reserialize that object model, once reflection
    /// gets past the field being private.
    /// </summary>
    internal static class DeadlockXmlReader
    {
        public const string DeadlockControlTypeName =
            "Microsoft.SqlServer.Management.SqlMgmt.Deadlock.DeadlockControl";

        private const string DeadlockModelFieldName = "deadlock";

        /// <summary>
        /// Returns the deadlock graph XML from the currently focused deadlock tab, or null when
        /// nothing focused is a deadlock tab, or a future SSMS build has changed the control's shape.
        /// </summary>
        public static string TryRead() => TryRead(FocusedControlFinder.Find(DeadlockControlTypeName));

        /// <summary>
        /// Returns the deadlock graph XML from a specific DeadlockControl instance - used when the
        /// caller already holds the exact control a right-click landed on, rather than needing to
        /// re-derive it from whatever currently has focus.
        /// </summary>
        public static string TryRead(System.Windows.Forms.Control control)
        {
            if (control == null) return null;

            var field = control.GetType().GetField(
                DeadlockModelFieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var model = field?.GetValue(control);
            if (model == null) return null;

            // No XML declaration: serializing to a string would otherwise declare encoding="utf-16", which
            // is then wrong once VisualizerLauncher writes it to disk as UTF-8.
            var serializer = new XmlSerializer(model.GetType());
            using var stringWriter = new StringWriter();
            using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { OmitXmlDeclaration = true }))
            {
                serializer.Serialize(xmlWriter, model);
            }

            return stringWriter.ToString();
        }
    }
}
