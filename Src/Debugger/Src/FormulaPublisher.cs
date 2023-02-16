using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;
using Microsoft.Formula.API.Plugins;
using Microsoft.Formula.Common;
using Microsoft.Formula.Common.Terms;
using Microsoft.Formula.Common.Rules;

namespace Debugger;

public class FormulaPublisher : ISolverPublisher
{
    private Map<Term, Term> LFP { get; set; } = new Map<Term, Term>(Term.Compare);
    private Set<Term> PositiveConstraintTerms { get; set;  } = new Set<Term>(Term.Compare);
    private Set<Term> NegativeConstraintTerms { get; set; } = new Set<Term>(Term.Compare);
    private List<Task<SolveResult>> SolveResults { get; set; } = new List<Task<SolveResult>>();
    private Set<Term> facts { get; set;  } = new Set<Term>(Term.Compare);
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

    public void SetVarFacts(Set<Term> factSet)
    {
        facts = factSet;
    }
    
    public Set<Term> GetVarFacts()
    {
        return facts;
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

    public void SetLeastFixedPointMap(Map<Term, Term> lfp)
    {
        LFP = lfp;
    }
    
    public Map<Term, Term> GetLeastFixedPointMap()
    {
        return LFP;
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