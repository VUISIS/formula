using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Formula.API;
using Microsoft.Formula.API.Plugins;
using Microsoft.Formula.Common;
using Microsoft.Formula.Common.Terms;
using ReactiveUI;

namespace Debugger;

public class FormulaPublisher : ISolverPublisher
{
    private Set<Term> PositiveConstraintTerms { get; set;  } = new Set<Term>(Term.Compare);
    private Set<Term> NegativeConstraintTerms { get; set; } = new Set<Term>(Term.Compare);
    private List<Task<SolveResult>> SolveResults { get; set; } = new List<Task<SolveResult>>();
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
    
    public void SetPosConstraintTerms(Set<Term> terms)
    {
        PositiveConstraintTerms = terms;
    }

    public void SetNegConstraintTerms(Set<Term> terms)
    {
        NegativeConstraintTerms = terms;
    }

    public void SetSolverResult(List<Task<SolveResult>> tasks)
    {
        SolveResults = tasks;
    }

    public void SetStartTime(DateTime time)
    {
        startTime = time;
    }

    public Set<Term> GetConstraintTerms()
    {
        return PositiveConstraintTerms.UnionWith(NegativeConstraintTerms);
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