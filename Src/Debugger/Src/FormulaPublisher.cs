using Microsoft.Formula.API.Plugins;
using Microsoft.Formula.Common;
using Microsoft.Formula.Common.Terms;

namespace Debugger;

public class FormulaPublisher : ISolverPublisher
{
    public Set<Term> PositiveConstraintTerms { get; set; } = new Set<Term>(Term.Compare);
    public Set<Term> NegativeConstraintTerms { get; set; } = new Set<Term>(Term.Compare);
}