global using DBADashSharedGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommandLine;

namespace DBADashGUI
{
    static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Common.IsApplicationRunning = true;
            Parser.Default.ParseArguments<CommandLineOptions>(args)
       .WithParsed<CommandLineOptions>(o =>
       {
           Application.EnableVisualStyles();
           Application.SetCompatibleTextRenderingDefault(false);
           Application.Run(new Main(o));
       });
        
        }
    }
}
