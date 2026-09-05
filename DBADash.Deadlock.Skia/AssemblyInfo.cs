using System.Runtime.CompilerServices;

// Text elision is an implementation detail worth testing directly rather than making public API.
// Declared here rather than as an MSBuild InternalsVisibleTo item because this project sets
// GenerateAssemblyInfo=false to share GlobalAssemblyInfo.cs, which suppresses generated attributes.
[assembly: InternalsVisibleTo("DBADash.Deadlock.Skia.Test")]
