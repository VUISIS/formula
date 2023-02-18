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
    private List<Task<SolveResult>> SolveResults = new List<Task<SolveResult>>();
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

    public void SetSolverResult(List<Task<SolveResult>> tasks)
    {
        SolveResults = tasks;
    }

    public void SetStartTime(DateTime time)
    {
        startTime = time;
    }

    public void SetLeastFixedPointTerms(IEnumerable<Term> terms)
    {
        lfpTerms = terms;
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
    
    public SolveResult? WaitForCompletion()
    {
        if (SolveResults.Count < 1)
        {
            return null;
        }

        if (Task.WhenAll(SolveResults).Wait(30000))
        {
            var solveResult = SolveResults[0].Result;
            if (startTime != null)
            {
                var endTime = solveResult.StopTime;
                resultTime = (endTime - startTime).Value.Milliseconds;
            }
            return solveResult;
        }
        return null;
    }
}