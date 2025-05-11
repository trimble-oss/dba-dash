using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBADashGUI.SchemaCompare;

namespace DBADashGUI.AgentJobs
{
    public class SqlJobStep
    {
        public string StepName { get; set; }
        public int StepId { get; set; }
        public string Subsystem { get; set; }
        public string Command { get; set; }
        public string DatabaseName { get; set; }
        public int OnSuccessAction { get; set; }
        public int OnSuccessStepId { get; set; }
        public int OnFailAction { get; set; }
        public int OnFailStepId { get; set; }
        public int RetryAttempts { get; set; }
        public int RetryInterval { get; set; }

        public CodeEditor.CodeEditorModes CodeEditorMode => Subsystem switch
        {
            "TSQL" => CodeEditor.CodeEditorModes.SQL,
            "PowerShell" => CodeEditor.CodeEditorModes.PowerShell,
            _ => CodeEditor.CodeEditorModes.None
        };

        public string OnSuccessActionDescription => OnSuccessAction switch
        {
            1 => "Quit with success",
            2 => "Quit with failure",
            3 => "Go to next step",
            4 => $"Go to step {OnSuccessStepId}",
            _ => OnSuccessAction.ToString()
        };

        public string OnFailActionDescription => OnFailAction switch
        {
            1 => "Quit with success",
            2 => "Quit with failure",
            3 => "Go to next step",
            4 => $"Go to step {OnSuccessStepId}",
            _ => OnFailAction.ToString()
        };
    }
}