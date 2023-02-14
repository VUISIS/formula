using Microsoft.Formula.CommandLine;
using Microsoft.Formula.API;

using System;

namespace Debugger;

public class FormulaProgram
{
    private CommandInterface ci;
    private DebuggerSink consoleSink;
    private EnvParams parameters;
    
    public FormulaPublisher FormulaPublisher { get; }

    
    public FormulaProgram()
    {
        consoleSink = new DebuggerSink();
        FormulaPublisher = new FormulaPublisher();
        var consoleChooser = new CommandLineProgram.ConsoleChooser();
        var tuple = new Tuple<EnvParamKind, object>[1];
        var value = new Tuple<EnvParamKind, object>(EnvParamKind.Debug_SolverPublisher, FormulaPublisher);
        tuple.SetValue(value,0);
        parameters = new EnvParams(tuple);
        ci = new CommandInterface(consoleSink, consoleChooser, parameters);
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

    public EnvParams GetParameters()
    {
        return parameters;
    }
}