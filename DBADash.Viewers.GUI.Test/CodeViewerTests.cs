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
    }
}
