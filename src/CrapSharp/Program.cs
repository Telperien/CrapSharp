using CrapSharp;
using CrapSharp.Processes;

// Usage: crapsharp --help (see Options.Usage). Exit codes: see ExitCodes.
return new App(new SystemProcessRunner(), Console.Out, Console.Error).Run(args);
