using Microsoft.Formula.Common;
using Microsoft.Formula.Common.Terms;

namespace Microsoft.Formula.API.Plugins;

public interface ISolverPublisher
{
    public Set<Term> PositiveConstraintTerms { get; set; }
    public Set<Term> NegativeConstraintTerms { get; set; }
}