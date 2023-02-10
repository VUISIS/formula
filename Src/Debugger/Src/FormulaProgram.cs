using Microsoft.Formula.CommandLine;
using Microsoft.Formula.API;

namespace Debugger;

public class FormulaProgram
{
    private CommandInterface ci;
    private DebuggerSink consoleSink;
    
    public FormulaProgram()
    {
        consoleSink = new DebuggerSink();
        var consoleChooser = new CommandLineProgram.ConsoleChooser();
        var envParams = new EnvParams();
        ci = new CommandInterface(consoleSink, consoleChooser, envParams);
    }

    public bool ExecuteCommand(string command)
    {
        return ci.DoCommand(command);
    }

    public string GetConsoleOutput()
    {
        var output = consoleSink.Output;
        ClearConsoleOutput();
        return output;
    }
    
    public void ClearConsoleOutput()
    {
        consoleSink.ClearOutput();
    }
}