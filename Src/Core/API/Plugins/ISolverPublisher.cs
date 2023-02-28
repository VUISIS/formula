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
    public void SetStartTime(DateTime time);
    public int GetResultTime();
    public string GetResultTimeString();
    public void AddPosConstraint(int id, string constraint);
    public void AddNegConstraint(int id, string constraint);
    public void AddDirConstraint(int id, string constraint);
    public void AddFlatConstraint(int id, string constraint);
    public void AddCurrentTerm(int id, string currentTerm);
    public void AddVariableFact(int id, string fact);
    public void SetCoreRules(Dictionary<int, List<string>> rules);
    public SolveResult SolveStart();
    public void SolveInit();
}