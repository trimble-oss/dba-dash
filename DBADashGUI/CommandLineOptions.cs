using CommandLine;
using System.Collections.Generic;

namespace DBADashGUI
{
    public class CommandLineOptions
    {
        [Option('t', "Tags", Required = false, HelpText = "Tag filtering")]
        public string TagFilters { get; set; }

        [Option('x', "NoTagMenu", Required = false, HelpText = "Remove Tag Menu")]
        public bool NoTagMenu { get; set; }

        [Value(0, MetaName = "files", Required = false, HelpText = "Deadlock graph (.xdl) file(s) to open in the deadlock viewer, without starting the full GUI")]
        public IEnumerable<string> Files { get; set; }

        [Option("RegisterFileAssociation", Required = false, HelpText = "Offer DBA Dash in Open with for .xdl files (current user), then exit")]
        public bool RegisterFileAssociation { get; set; }

        [Option("UnregisterFileAssociation", Required = false, HelpText = "Remove the .xdl file association added by RegisterFileAssociation, then exit")]
        public bool UnregisterFileAssociation { get; set; }
    }

}
