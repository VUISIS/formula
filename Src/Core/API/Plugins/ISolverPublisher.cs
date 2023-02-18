using System;
using System.Collections.Generic;
using Microsoft.Formula.Common;
using Microsoft.Formula.Common.Terms;

using System.Threading.Tasks;

namespace Microsoft.Formula.API.Plugins;

public interface ISolverPublisher
{
    public void SetSolverResult(List<Task<SolveResult>> tasks);
    public void SetStartTime(DateTime time);
    public int GetResultTime();
    public string GetResultTimeString();
    public void SetLeastFixedPointTerms(IEnumerable<Term> terms);
    public IEnumerable<Term> GetLeastFixedPointTerms();
    public Dictionary<int, Dictionary<ConstraintKind, List<string>>> GetLeastFixedPointConstraints();
    public void SetLeastFixedPointConstraints(Dictionary<int, Dictionary<ConstraintKind, List<string>>> constraints);
    public SolveResult WaitForCompletion();
}