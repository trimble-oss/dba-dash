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
using static DBADashGUI.DiffControl;

namespace DBADashGUI
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
                addDiffControl();
                diff1.OldText = oldText;
                diff1.NewText = newText;
                diff1.Mode = mode;
            

       }

        private void addDiffControl()
        {
               if (!this.Controls.Contains(diff1)){
                    this.Controls.Add(diff1);
                    diff1.Dock = DockStyle.Fill;
                }
        }
     

        private void Diff_Load(object sender, EventArgs e)
        {
            addDiffControl();
        }
    }
}
