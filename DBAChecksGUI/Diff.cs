using ICSharpCode.TextEditor.Document;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBAChecksGUI.DiffControl;

namespace DBAChecksGUI
{
    public partial class Diff : Form
    {

        DiffControl diff1 = new DiffControl();

        public Diff()
        {
            InitializeComponent();
        }


        public ViewMode Mode
        {
            get
            {
                return diff1.Mode;
            }
            set
            {
                diff1.Mode = value;
                
            }
        }

        public void setText(string oldText,string newText, ViewMode mode= ViewMode.Diff)
        {
            diff1.OldText = oldText;
            diff1.NewText = newText;

       }

     

        private void Diff_Load(object sender, EventArgs e)
        {
            this.Controls.Add(diff1);
            diff1.Dock = DockStyle.Fill;
        }
    }
}
