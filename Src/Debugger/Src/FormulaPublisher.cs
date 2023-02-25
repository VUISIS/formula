using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Formula.API;
using Microsoft.Formula.API.Plugins;
using Microsoft.Formula.Common.Terms;

namespace Debugger;

public class FormulaPublisher : ISolverPublisher
{
    private Dictionary<int, Dictionary<ConstraintKind, List<string>>> lfpConstraints  = new Dictionary<int, Dictionary<ConstraintKind, List<string>>>();
    private IEnumerable<Term> lfpTerms = new List<Term>();
    private List<string> coreRules = new List<string>();
    private SolveResult? SolverResult;
    private DateTime? startTime;
    private int resultTime;

    public string GetResultTimeString()
    {
        return resultTime + "ms.";
    }
    
    public int GetResultTime()
    {
        return resultTime;
    }

    public void SetSolverTask(SolveResult solverResult)
    {
        SolverResult = solverResult;
    }

    public void SetStartTime(DateTime time)
    {
        startTime = time;
    }

    public void SetLeastFixedPointTerms(IEnumerable<Term> terms)
    {
        lfpTerms = terms;
    }

    public void SetCoreRules(List<string> rules)
    {
        coreRules = rules;
    }

    public List<string> GetCoreRules()
    {
        return coreRules;
    }

    public IEnumerable<Term> GetLeastFixedPointTerms()
    {
        return lfpTerms;
    }
    
    public void SetLeastFixedPointConstraints(Dictionary<int, Dictionary<ConstraintKind, List<string>>> constraints)
    {
        lfpConstraints = constraints;
    }

    public Dictionary<int, Dictionary<ConstraintKind, List<string>>> GetLeastFixedPointConstraints()
    {
        return lfpConstraints;
    }

    public void SolveInit()
    {
        if (SolverResult != null)
        {
            SolverResult.Init();
        }
    }
    
    public SolveResult? SolveStart()
    {
        SetStartTime(DateTime.Now);
        if (SolverResult != null)
        {
            SolverResult.Start();
            if (startTime != null)
            {
                var endTime = SolverResult.StopTime;
                resultTime = (endTime - startTime).Value.Milliseconds;
            }
            return SolverResult;
        }
        return null;
    }
}