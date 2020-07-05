using ICSharpCode.TextEditor.Document;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DBAChecksGUI
{

    public class AppSyntaxModeProvider : ISyntaxModeFileProvider
    {
        private List<SyntaxMode> m_syntaxModes = null;

        public ICollection<SyntaxMode> SyntaxModes
        {
            get
            {
                return m_syntaxModes;
            }
        }

        public AppSyntaxModeProvider()
        {
        
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources\\SyntaxModes.xml");
            if (File.Exists(path)) {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    m_syntaxModes = SyntaxMode.GetSyntaxModes(fs);
                }
            }
            else
            {
                m_syntaxModes = new List<SyntaxMode>();
            }                  
        }

        public XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources\\" +  syntaxMode.FileName);

            return new XmlTextReader(path);
        }

        public void UpdateSyntaxModeList()
        {
        }
    }

}
