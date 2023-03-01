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
    private SolveResult? SolverResult;
    private DateTime? startTime;

    public void AddPosConstraint(int id, string constraint)
    {
        if(PosConstraintTerms.ContainsKey(id))
        {
            PosConstraintTerms[id].Add(constraint);
            return;
        }
        PosConstraintTerms.Add(id, new List<string>{constraint});
    }

    public Dictionary<int, List<string>> GetPosConstraints()
    {
        return PosConstraintTerms;
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
    
    public Dictionary<int, List<string>> GetNegConstraints()
    {
        return NegConstraintTerms;
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
    
    public Dictionary<int, List<string>> GetDirConstraints()
    {
        return DirConstraintTerms;
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
    
    public Dictionary<int, List<string>> GetFlatConstraints()
    {
        return FlatConstraintTerms;
    }

    public void AddCurrentTerm(int id, string currentTerm)
    {
        if (!CurrentTerms.ContainsKey(id))
        {
            CurrentTerms.Add(id, currentTerm);
        }
    }

    public Dictionary<int, string> GetCurrentTerms()
    {
        return CurrentTerms;
    }

    public void AddVariableFact(int id, string fact)
    {
        Console.WriteLine("ADD FACT");
        if (!VarFacts.ContainsKey(id))
        {
            Console.WriteLine(id.ToString() + " " + fact);
            VarFacts.Add(id, fact);
        }
    }

    public Dictionary<int, string> GetVarFacts()
    {
        Console.WriteLine(VarFacts.Count.ToString());
        return VarFacts;
    }

    public void SetCoreRules(Dictionary<int, List<string>> rules)
    {
        CoreRules = rules;
    }

    public Dictionary<int, List<string>>? GetCoreRules()
    {
        return CoreRules;
    }

    public void SetSolverResult(SolveResult solverResult)
    {
        SolverResult = solverResult;
    }

    public SolveResult? GetSolverResult()
    {
        return SolverResult;
    }

    public void SetStartTime(DateTime time)
    {
        startTime = time;
    }
}