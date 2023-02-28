using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Formula.API;
using Microsoft.Formula.API.Plugins;
using Microsoft.Formula.Common;
using Microsoft.Formula.Common.Terms;

namespace Debugger;

public class FormulaPublisher : ISolverPublisher
{
    private Dictionary<int, List<string>> DirConstraintTerms = new Dictionary<int, List<string>>();
    private Dictionary<int, List<string>> PosConstraintTerms = new Dictionary<int, List<string>>();
    private Dictionary<int, List<string>> NegConstraintTerms = new Dictionary<int, List<string>>();
    private Dictionary<int, List<string>> FlatConstraintTerms = new Dictionary<int, List<string>>();
    private Dictionary<int, string> CurrentTerms = new Dictionary<int, string>();
    private Dictionary<int, string> VarFacts = new Dictionary<int, string>();
    private Dictionary<int, List<string>> CoreRules = new Dictionary<int, List<string>>();
    private Set<Term>? varFacts;
    private SolveResult? SolverResult;
    private DateTime? startTime;
    private int resultTime;

    public string GetResultTimeString()
    {
        return resultTime + "ms.";
    }

    public void AddPosConstraint(int id, string constraint)
    {
        if(PosConstraintTerms.ContainsKey(id))
        {
            PosConstraintTerms[id].Add(constraint);
            return;
        }
        PosConstraintTerms.Add(id, new List<string>{constraint});
    }

    public void AddNegConstraint(int id, string constraint)
    {
        if(NegConstraintTerms.ContainsKey(id))
        {
            NegConstraintTerms[id].Add(constraint);
            return;
        }
        NegConstraintTerms.Add(id, new List<string>{constraint});
    }

    public void AddDirConstraint(int id, string constraint)
    {
        if(DirConstraintTerms.ContainsKey(id))
        {
            DirConstraintTerms[id].Add(constraint);
            return;
        }
        DirConstraintTerms.Add(id, new List<string>{constraint});
    }

    public void AddFlatConstraint(int id, string constraint)
    {
        if(FlatConstraintTerms.ContainsKey(id))
        {
            FlatConstraintTerms[id].Add(constraint);
            return;
        }
        FlatConstraintTerms.Add(id, new List<string>{constraint});
    }

    public void AddCurrentTerm(int id, string currentTerm)
    {
        if (!CurrentTerms.ContainsKey(id))
        {
            CurrentTerms.Add(id, currentTerm);
        }
    }

    public void AddVariableFact(int id, string fact)
    {
        if(!VarFacts.ContainsKey(id))
        {
            VarFacts.Add(id, fact);
        }
    }

    public void SetCoreRules(Dictionary<int, List<string>> rules)
    {
        CoreRules = rules;
    }

    public int GetResultTime()
    {
        return resultTime;
    }
    
    public void SetSolverResult(SolveResult solverResult)
    {
        SolverResult = solverResult;
    }

    public void SetStartTime(DateTime time)
    {
        startTime = time;
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