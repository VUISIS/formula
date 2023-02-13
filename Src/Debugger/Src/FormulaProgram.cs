using Microsoft.Formula.CommandLine;
using Microsoft.Formula.API;
using Microsoft.Formula.Common;
using Microsoft.Formula.Common.Terms;

namespace Debugger;

public class FormulaProgram
{
    private CommandInterface ci;
    private DebuggerSink consoleSink;
    private FormulaPublisher formulaPublisher;
    
    public FormulaProgram()
    {
        consoleSink = new DebuggerSink();
        formulaPublisher = new FormulaPublisher();
        var consoleChooser = new CommandLineProgram.ConsoleChooser();
        var envParams = new EnvParams();
        EnvParams.SetParameter(envParams, EnvParamKind.Debug_SolverPublisher, formulaPublisher);
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
    
    public Set<Term> GetPositiveConstraints()
    {
        return formulaPublisher.PositiveConstraintTerms;
    }
    
    public Set<Term> GetNegativeConstraints()
    {
        return formulaPublisher.NegativeConstraintTerms;
    }
}