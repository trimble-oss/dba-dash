using System;
using System.IO;
using DBADash.QueryPlan.Skia;

// Writes the query plan operator icon reference to the path given - see PlanOperatorReference.
// Run by this project's own build; see the project file.

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: DBADash.QueryPlan.Reference <output.html>");
    return 1;
}

var path = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(path)!);
File.WriteAllText(path, PlanOperatorReference.BuildHtml());

Console.WriteLine("Query plan operator reference: " + path);
return 0;
