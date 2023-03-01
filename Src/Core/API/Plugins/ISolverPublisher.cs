using System;
using System.Collections.Generic;
using Microsoft.Formula.Common;
using Microsoft.Formula.Common.Terms;

using System.Threading.Tasks;
using Microsoft.Formula.Common.Rules;

namespace Microsoft.Formula.API.Plugins;

public interface ISolverPublisher
{
    public void SetSolverResult(SolveResult solverResult);
    public SolveResult GetSolverResult();
    public void AddPosConstraint(int id, string constraint);
    public Dictionary<int, List<string>> GetPosConstraints();
    public void AddNegConstraint(int id, string constraint);
    public Dictionary<int, List<string>> GetNegConstraints();
    public void AddDirConstraint(int id, string constraint);
    public Dictionary<int, List<string>> GetDirConstraints();
    public void AddFlatConstraint(int id, string constraint);
    public Dictionary<int, List<string>> GetFlatConstraints();
    public void AddCurrentTerm(int id, string currentTerm);
    public Dictionary<int, string> GetCurrentTerms();
    public void AddVariableFact(int id, string fact);
    public Dictionary<int, string> GetVarFacts();
    public void SetCoreRules(Dictionary<int, List<string>> rules);
    public Dictionary<int, List<string>> GetCoreRules();
}