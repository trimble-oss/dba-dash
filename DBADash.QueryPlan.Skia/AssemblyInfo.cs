using System.Runtime.CompilerServices;

// Text elision and the parsed glyph paths are implementation details worth testing directly rather
// than making public API.  Declared here rather than as an MSBuild InternalsVisibleTo item because
// this project sets GenerateAssemblyInfo=false to share GlobalAssemblyInfo.cs, which suppresses
// generated attributes.
[assembly: InternalsVisibleTo("DBADash.QueryPlan.Skia.Test")]
