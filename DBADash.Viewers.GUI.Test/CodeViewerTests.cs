using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>The code viewer's XML mode, as the grid viewer's xml columns open in it.</summary>
    [TestClass]
    public class CodeViewerTests
    {
        /// <summary>
        /// Code set before the syntax is switched to XML - the order an object initializer naturally puts them in -
        /// used to leave the viewer empty, as XML mode formatted the editor's own copy of the text, which Code
        /// never set.
        /// </summary>
        [STATestMethod]
        public void CodeBeforeXmlSyntax_KeepsTheCode()
        {
            using var viewer = new DBADashGUI.CodeViewer
            {
                Code = "<a><b>1</b></a>",
                Language = DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes.XML
            };

            StringAssert.Contains(viewer.Code, "<b>1</b>");
        }

        /// <summary>sp_BlitzLock returns a deadlock's queries as processing instructions, not elements.</summary>
        [STATestMethod]
        public void XmlProcessingInstruction_IsShown()
        {
            using var viewer = new DBADashGUI.CodeViewer
            {
                Code = "<?query Proc1   ?>",
                Language = DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes.XML
            };

            StringAssert.Contains(viewer.Code, "Proc1");
        }

        private const DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes Xml = DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes.XML;
        private const DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes Json = DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes.Json;
        private const DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes Sql = DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes.SQL;

        /// <summary>
        /// XML is formatted unless asked not to be - View File Text shows a file as written - and Format switches
        /// between the two, back to exactly the text given.
        /// </summary>
        [STATestMethod]
        public void Xml_FormatsByDefault_AndSwitchesBack()
        {
            const string xml = "<a><b>1</b></a>";
            using var formatted = new DBADashGUI.CodeViewer { Language = Xml, Code = xml };
            StringAssert.Contains(formatted.Code, "\n", "formatted by default");

            using var viewer = new DBADashGUI.CodeViewer { Language = Xml, Format = false, Code = xml };
            Assert.AreEqual(xml, viewer.Code);
            Assert.IsTrue(FormatButton(viewer).Available);
            Assert.IsFalse(FormatButton(viewer).Checked);

            FormatButton(viewer).PerformClick();
            StringAssert.Contains(viewer.Code, "\n");
            Assert.IsTrue(FormatButton(viewer).Checked);

            FormatButton(viewer).PerformClick();
            Assert.AreEqual(xml, viewer.Code, "back to the text as given");
        }

        /// <summary>
        /// The Format button says what the unformatted text is - the file as written, or a cell's copy - so a nested JSON
        /// value's one-line copy isn't taken for the file's text.
        /// </summary>
        [STATestMethod]
        public void FormatButton_SaysWhatTheUnformattedTextIs()
        {
            using var file = new DBADashGUI.CodeViewer { Language = Json, Format = false, UnformattedLabel = "as written in the file", Code = "{}" };
            StringAssert.Contains(FormatButton(file).Text, "showing the text as written in the file");

            FormatButton(file).PerformClick();
            StringAssert.Contains(FormatButton(file).Text, "click to show the text as written in the file");

            using var unlabelled = new DBADashGUI.CodeViewer { Language = Json, Format = true, Code = "{}" };
            StringAssert.Contains(FormatButton(unlabelled).Text,
                "click to show the text " + DBADashGUI.SchemaCompare.CodeEditorForm.DefaultUnformattedLabel);
        }

        /// <summary>JSON is shown as given unless formatting is asked for - as other callers have always had it.</summary>
        [STATestMethod]
        public void Json_AsGivenByDefault_FormattedWhenAsked()
        {
            const string json = "{\"a\":1,\"b\":[1,2]}";
            using var asGiven = new DBADashGUI.CodeViewer { Language = Json, Code = json };
            Assert.AreEqual(json, asGiven.Code);
            Assert.IsFalse(FormatButton(asGiven).Checked);

            using var formatted = new DBADashGUI.CodeViewer { Language = Json, Format = true, Code = json };
            StringAssert.Contains(formatted.Code, "\"a\": 1");
            Assert.IsTrue(FormatButton(formatted).Checked);
        }

        /// <summary>JSON lines - a log - are each formatted, rather than the whole failing to parse as one value.</summary>
        [TestMethod]
        public void FormatJson_FormatsEachOfSeveralValues()
        {
            var formatted = DBADashGUI.SchemaCompare.CodeEditor.FormatJson("{\"n\":1}\n{\"n\":2,\"s\":\"é<\"}\n");

            StringAssert.Contains(formatted, "\"n\": 1");
            StringAssert.Contains(formatted, "\"n\": 2");
            StringAssert.Contains(formatted, "\"s\": \"é<\"", "not escaped beyond what JSON needs");
        }

        /// <summary>Text that isn't JSON is shown as given, with Format off and saying why.</summary>
        [STATestMethod]
        public void Json_ThatDoesntParse_IsShownAsGiven()
        {
            const string json = "{\"a\":1,";
            using var viewer = new DBADashGUI.CodeViewer { Language = Json, Format = true, Code = json };

            Assert.AreEqual(json, viewer.Code);
            Assert.IsFalse(FormatButton(viewer).Enabled);
            StringAssert.Contains(FormatButton(viewer).Text, "can't be formatted");
        }

        /// <summary>Format is only for XML and JSON, and only in a viewer: in an editor it would replace what's been typed.</summary>
        [STATestMethod]
        public void FormatButton_OnlyForXmlAndJsonInAViewer()
        {
            using var sql = new DBADashGUI.CodeViewer { Language = Sql, Code = "SELECT 1" };
            Assert.IsFalse(FormatButton(sql).Available);

            using var editor = new DBADashGUI.SchemaCompare.CodeEditorForm { EditEnabled = true, Syntax = Json, Code = "{}" };
            Assert.IsFalse(FormatButton(editor).Available);
        }

        [TestMethod]
        public void HasLongLine_CountsEachLine()
        {
            var limit = DBADashGUI.SchemaCompare.CodeEditor.MaxHighlightedLineLength;
            Assert.IsFalse(DBADashGUI.SchemaCompare.CodeEditor.HasLongLine(new string('x', limit)));
            Assert.IsTrue(DBADashGUI.SchemaCompare.CodeEditor.HasLongLine(new string('x', limit + 1)));
            Assert.IsFalse(DBADashGUI.SchemaCompare.CodeEditor.HasLongLine(new string('x', limit) + "\n" + new string('x', limit)));
            Assert.IsFalse(DBADashGUI.SchemaCompare.CodeEditor.HasLongLine(null));
        }

        /// <summary>
        /// Minified JSON longer than the highlighting limit on one line is still formatted - the limit is checked on
        /// the text shown, which formatted is no longer one line.
        /// </summary>
        [STATestMethod]
        public void LongMinifiedJson_CanStillBeFormatted()
        {
            var json = "[" + string.Join(",", Enumerable.Range(0, 10000).Select(i => $"{{\"n\":{i}}}")) + "]";
            Assert.IsTrue(DBADashGUI.SchemaCompare.CodeEditor.HasLongLine(json));

            using var viewer = new DBADashGUI.CodeViewer { Language = Json, Format = true, Code = json };
            Assert.IsTrue(FormatButton(viewer).Checked);
            Assert.IsFalse(DBADashGUI.SchemaCompare.CodeEditor.HasLongLine(viewer.Code));
        }

        private static System.Windows.Forms.ToolStripButton FormatButton(System.Windows.Forms.Form form) =>
            FindAll<System.Windows.Forms.ToolStrip>(form).SelectMany(t => t.Items.OfType<System.Windows.Forms.ToolStripButton>())
                .Single(i => i.Name == "tsFormat");

        private static IEnumerable<T> FindAll<T>(System.Windows.Forms.Control parent) where T : System.Windows.Forms.Control =>
            parent.Controls.Cast<System.Windows.Forms.Control>().SelectMany(c => (c is T t ? [t] : Enumerable.Empty<T>()).Concat(FindAll<T>(c)));
    }
}
