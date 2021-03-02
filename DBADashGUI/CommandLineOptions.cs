using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
namespace DBADashGUI
{
    public class CommandLineOptions
    {
        [Option('t', "Tags", Required = false, HelpText = "Tag filtering")]
        public string TagFilters { get; set; }

        [Option('x', "NoTagMenu", Required = false, HelpText = "Remove Tag Menu")]
        public bool NoTagMenu{ get; set; }
    }

}
