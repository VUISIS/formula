namespace Microsoft.Formula.Solver
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;

    using API;
    using API.Nodes;
    using Common;
    using Common.Extras;
    using Common.Rules;
    using Common.Terms;
    using API.Base;

    using Z3Expr = Z3.Expr;
    using Z3BoolExpr = Z3.BoolExpr;
    using System.Numerics;
    using System.Text.RegularExpressions;

    internal class SymExecuter
    {
        private static char PatternVarBoundPrefix = '^';
        private static char PatternVarUnboundPrefix = '*';

        private static string termSymbGen = 
            "(\\(|\\s)?[\\w\\d]+@~SC2VAR~([\\w\\d]+)(\\s|\\))?";

        /// <summary>
        /// Facts where symbolic constants have been replaced by variables
        /// </summary>
        private Set<Term> varFacts;

        /// <summary>
        /// Map from an alias to variablized fact.
        /// </summary>
        private Map<UserCnstSymb, Term> aliasMap;

        /// <summary>
        /// Maps a type term to a set of patterns that cover the triggering of that type.
        /// </summary>
        private Map<Term, Set<Term>> typesToTriggersMap = new Map<Term, Set<Term>>(Term.Compare);

        /// <summary>
        /// Maps a find pattern to a subindex.
        /// </summary>
        private Map<Term, SymSubIndex> trigIndices = new Map<Term, SymSubIndex>(Term.Compare);

        /// <summary>
        /// Maps a comprehension symbol to a subindex.
        /// </summary>
        private Map<Symbol, SymSubIndex> comprIndices = new Map<Symbol, SymSubIndex>(Symbol.Compare);

        /// <summary>
        /// Maps a symbol to a set of indices with patterns beginning with this symbol. 
        /// </summary>
        private Map<Symbol, LinkedList<SymSubIndex>> symbToIndexMap =
            new Map<Symbol, LinkedList<SymSubIndex>>(Symbol.Compare);

        /// <summary>
        /// A map from strata to rules that are not triggered.
        /// </summary>
        private Map<int, LinkedList<CoreRule>> untrigRules =
            new Map<int, LinkedList<CoreRule>>((x, y) => x - y);

        /// <summary>
        /// The current least fixed point.
        /// </summary>
        private Map<Term, SymElement> lfp = new Map<Term, SymElement>(Term.Compare);

        private Map<Term, Set<Derivation>> facts = new Map<Term, Set<Derivation>>(Term.Compare);

        private List<Z3BoolExpr> pendingConstraints =
            new List<Z3BoolExpr>();

        private Dictionary<int, Z3BoolExpr> recursionConstraints =
            new Dictionary<int, Z3BoolExpr>();

        private Dictionary<int, int> ruleCycles =
            new Dictionary<int, int>();

        private bool hasCycles = false;
        
        private List<Dictionary<Z3Expr, Z3Expr>> prevSolutions =
            new List<Dictionary<Z3Expr, Z3Expr>>();

        private List<List<string>> solutionStrings = new List<List<string>>();

        public RuleTable Rules { get; private set; }

        public Solver Solver { get; private set; }

        public bool KeepDerivations { get; private set; }

        public TermIndex Index { get; private set; }

        public TermEncIndex Encoder { get; private set; }

        public Map<Term, Term> varToTypeMap =
            new Map<Term, Term>(Term.Compare);

        private Set<Term> PositiveConstraintTerms = new Set<Term>(Term.Compare);
        private Set<Term> NegativeConstraintTerms = new Set<Term>(Term.Compare);

        private Map<Term, List<Term>> symCountMap =
            new Map<Term, List<Term>>(Term.Compare);

        protected int coreCounter = 0;
        protected int exprCounter = 0;

        protected bool isSolvable = false;
        protected bool hasCore = false;
        protected Z3BoolExpr[] coreExprs;

        protected Dictionary<Z3Expr, Z3BoolExpr> cnfMap =
            new Dictionary<Z3Expr, Z3BoolExpr>();

        Dictionary<Z3BoolExpr, Z3BoolExpr> unsatCoreMap =
            new Dictionary<Z3BoolExpr, Z3BoolExpr>();

        public int GetSymbolicCountIndex(Term t)
        {
            List<Term> terms;
            if (!symCountMap.TryFindValue(t, out terms))
            {
                terms = new List<Term>();
                symCountMap.Add(t, terms);
            }

            return terms.Count;
        }

        public Term GetSymbolicCountTerm(Term t, int index)
        {
            return symCountMap[t].ElementAt(index);
        }

        public void AddSymbolicCountTerm(Term x, Term y)
        {
            List<Term> terms;
            if (!symCountMap.TryFindValue(x, out terms))
            {
                terms = new List<Term>();
                symCountMap.Add(x, terms);
            }

            terms.Add(y);
        }

        protected void AddPositiveConstraints(Term bind1, Term bind2)
        {
            SymElement e1, e2;

            if (bind1 != Index.FalseValue &&
                lfp.TryFindValue(bind1, out e1))
            {
                if (e1.HasConstraints())
                {
                    PositiveConstraintTerms.Add(bind1);
                }
            }

            if (bind2 != Index.FalseValue &&
                lfp.TryFindValue(bind2, out e2))
            {
                if (e2.HasConstraints())
                {
                    PositiveConstraintTerms.Add(bind2);
                }
            }
        }

        protected void RemovePositiveConstraints()
        {
            PositiveConstraintTerms.Clear();
        }

        private string RemoveSymGenVar(string b)
        {
            var matchEval = new MatchEvaluator((m) =>
            { 
                return m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value;
            });
            var newStr = Regex.Replace(b, termSymbGen, matchEval);
            if (newStr.Length < 1)
            {
                return b;
            }
            else
            {
                return newStr;
            }
        }

        public void SetVarFacts()
        {
            if (EnvParams.IsSolverPublisherSet(Solver.Env.Parameters))
            {
                foreach (var fact in aliasMap)
                {
                    EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters, EnvParamKind.Debug_SolverPublisher)
                             .AddVariableFact(fact.Key.Id, fact.Key.Name.Replace("%",""));
                }
            }
        }

        private void LFPChanged(Term t, SymElement s)
        {
            if (EnvParams.IsSolverPublisherSet(Solver.Env.Parameters))
            {
                if (t.Symbol.Kind == SymbolKind.ConSymb)
                {
                    if (!((ConSymb)t.Symbol).IsAutoGen)
                    {
                        var temp = PrefixToInfix(t);
                        var n = RemoveSymGenVar(temp);
                        EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters, EnvParamKind.Debug_SolverPublisher)
                            .AddCurrentTerm(t.Symbol.Id, n);
                    }
                }
                else 
                {
                    EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters, EnvParamKind.Debug_SolverPublisher)
                        .AddCurrentTerm(t.Symbol.Id, t.ToString());
                }
                
                foreach (var data in s.GetConstraintData())
                {
                    foreach (var dirConst in data.DirConstraints)
                    {
                        var cnvStr = Z3Printer.ConvertZ3ExprPrefixToInfix(dirConst);
                        EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters, EnvParamKind.Debug_SolverPublisher)
                                 .AddDirConstraint(t.Symbol.Id, RemoveSymGenVar(cnvStr));
                    }

                    foreach (var posConst in data.PosConstraints)
                    {
                        if (posConst.Symbol is UserSymbol userSym &&
                            !userSym.IsAutoGen)
                        {
                            var temp = PrefixToInfix(posConst);
                            if (temp.Length > 0)
                            {
                                EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters, EnvParamKind.Debug_SolverPublisher)
                                    .AddPosConstraint(t.Symbol.Id, RemoveSymGenVar(temp));
                            }
                        }
                    }
            
                    foreach (var negConst in data.NegConstraints)
                    {
                        if (negConst.Symbol is UserSymbol userSym &&
                            !userSym.IsAutoGen)
                        {
                            var temp = PrefixToInfix(negConst);
                            if (temp.Length > 0)
                            {
                                EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters,
                                        EnvParamKind.Debug_SolverPublisher)
                                    .AddNegConstraint(t.Symbol.Id, RemoveSymGenVar(temp));
                            }
                        }
                    }
                    
                    var sideConstraints = s.GetSideConstraints(this);
                    var cnvS = Z3Printer.ConvertZ3ExprPrefixToInfix(sideConstraints);
                    EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters, EnvParamKind.Debug_SolverPublisher)
                             .AddFlatConstraint(t.Symbol.Id, RemoveSymGenVar(cnvS));
                }
            }
        }

        public void SetPublisherCoreRules()
        {
            if (!EnvParams.IsSolverPublisherSet(Solver.Env.Parameters))
                return;
            
            var rules = new Dictionary<int, List<string>>();
            foreach (var coreRule in Rules.Rules)
            {
                var t = coreRule.Head;
                if (!rules.ContainsKey(t.Symbol.Id))
                {
                    rules.Add(t.Symbol.Id, new List<string>());
                }
                
                switch (t.Symbol.Kind)
                {
                    case SymbolKind.ConSymb:
                        if (t.Symbol is ConSymb conSymb &&
                            !conSymb.IsAutoGen)
                        {
                            rules[t.Symbol.Id].Add(t.ToString());
                        }
                        break;
                    case SymbolKind.UserCnstSymb:
                        if (t.Symbol is UserCnstSymb userCnstSymb)
                        {
                            if (!userCnstSymb.IsAutoGen)
                            {
                                if (userCnstSymb.IsDerivedConstant &&
                                    userCnstSymb.IsNonVarConstant)
                                {
                                    rules[t.Symbol.Id].Add(userCnstSymb.Name);
                                    break;
                                }
                                
                                var newTerm = Index.SymbCnstToVar(userCnstSymb, out var wasAdded);
                                if (wasAdded)
                                {
                                    rules[t.Symbol.Id].Add(newTerm.ToString());
                                }
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters, EnvParamKind.Debug_SolverPublisher)
                     .SetCoreRules(rules);
        }

        public bool Exists(Term t)
        {
            return lfp.ContainsKey(t);
        }

        protected Map<Term, Set<Term>> flattenedDervs =
            new Map<Term, Set<Term>>(Term.Compare);

        protected void AddOtherDervs(Term t, Set<Term> terms)
        {
            if (t != Index.FalseValue)
            {
                if (t.Symbol is UserSymbol &&
                    (!((UserSymbol)t.Symbol).IsAutoGen))
                {
                    terms.Add(t);
                }
                else
                {
                    Set<Term> otherDervs;
                    if (flattenedDervs.TryFindValue(t, out otherDervs))
                    {
                        foreach (var other in otherDervs)
                        {
                            terms.Add(other);
                        }
                    }
                }
            }
        }

        protected void AddFlattenedDerivations(Term t, Derivation d)
        {
            Set<Term> myFlattenedDervs;
            if (!flattenedDervs.TryFindValue(t, out myFlattenedDervs))
            {
                myFlattenedDervs = new Set<Term>(Term.Compare);
                flattenedDervs.Add(t, myFlattenedDervs);
            }

            AddOtherDervs(d.Binding1, myFlattenedDervs);
            AddOtherDervs(d.Binding2, myFlattenedDervs);
        }

        public bool IfExistsThenDerive(Term t, Derivation d)
        {
            Contract.Requires(t != null);

            if (!KeepDerivations)
            {
                return lfp.ContainsKey(t);
            }

            Set<Derivation> dervs;
            if (!facts.TryFindValue(t, out dervs))
            {
                return false;
            }

            dervs.Add(d);
            AddFlattenedDerivations(t, d);
            return true;
        }

        public void AddDerivation(Term t, Term bind1, Term bind2)
        {
            AddPositiveConstraints(bind1, bind2);
            ExtendLFP(t);
            RemovePositiveConstraints();
        }

        public void PendConstraint(Z3BoolExpr expr)
        {
            pendingConstraints.Add(expr);
        }

        public bool HasSideConstraint(Term term)
        {
            SymElement e;
            if (lfp.TryFindValue(term, out e))
            {
                if (e.HasConstraints())
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasSideConstraints(IEnumerable<Term> terms)
        {
            foreach (var term in terms)
            {
                SymElement e;
                if (lfp.TryFindValue(term, out e))
                {
                    if (e.HasConstraints())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public Z3BoolExpr GetSideConstraints(Term t)
        {
            SymElement e;
            if (lfp.TryFindValue(t, out e))
            {
                if (e.HasConstraints())
                {
                    return e.GetSideConstraints(this);
                }
            }

            return Solver.Context.MkTrue(); // if no constraints
        }

        public bool AddNegativeConstraint(Term t)
        {
            SymElement e;
            if (lfp.TryFindValue(t, out e))
            {
                if (e.HasConstraints())
                {
                    NegativeConstraintTerms.Add(t);
                    return true;
                }
            }

            return false;
        }

        public bool GetSymbolicTerm(Term t, out SymElement e)
        {
            return lfp.TryFindValue(t, out e);
        }

        public void PendEqualityConstraint(Term t1, Term t2)
        {
            Term normalized;
            var expr1 = Encoder.GetTerm(t1, out normalized);
            var expr2 = Encoder.GetTerm(t2, out normalized);
            PendEqualityConstraint(expr1, expr2);
        }

        public void PendEqualityConstraint(Z3Expr expr1, Z3Expr expr2)
        {
            pendingConstraints.Add(Solver.Context.MkEq(expr1, expr2));
        }

        private void AddRecursionConstraint(int ruleId)
        {
            if (pendingConstraints.IsEmpty())
            {
                return;
            }

            Z3BoolExpr expr = null;
            Z3BoolExpr previousExpr;

            foreach (var constraint in pendingConstraints)
            {
                if (expr == null)
                {
                    expr = constraint;
                }
                else
                {
                    expr = Solver.Context.MkAnd(expr, constraint);
                }
            }
            expr = Solver.Context.MkNot(expr);
            
            if (recursionConstraints.TryGetValue(ruleId, out previousExpr))
            {
                recursionConstraints[ruleId] = Solver.Context.MkAnd(previousExpr, expr);
            }
            else
            {
                recursionConstraints.Add(ruleId, expr);
            }
        }

        public void PrintRecursionConflict(Z3BoolExpr expr)
        {
            Console.WriteLine("Conflict detected in recursion constraint: " + expr + "\n\n");
        }

        public SymExecuter(Solver solver)
        {
            Contract.Requires(solver != null);
            Solver = solver;
            Rules = solver.PartialModel.Rules;
            Index = solver.PartialModel.Index;
            Encoder = new TermEncIndex(solver);

            KeepDerivations = true;

            solver.PartialModel.ConvertSymbCnstsToVars(out varFacts, out aliasMap);
            
            Map<ConSymb, List<Term>> cardTerms = 
                new Map<ConSymb, List<Term>>(Symbol.Compare);

            foreach (var fact in varFacts)
            {
                if (Solver.PartialModel.CheckIfCardTerm(fact))
                {
                    List<Term> termList;
                    if (!cardTerms.TryFindValue((ConSymb)fact.Symbol, out termList))
                    {
                        termList = new List<Term>();
                        cardTerms.Add((ConSymb)fact.Symbol, termList);
                    }
                    termList.Add(fact);
                }
            }
            
            //// Need to pre-register all aliases with the encoder.
            foreach (var kv in aliasMap)
            {
                Term vTerm = Index.SymbCnstToVar(kv.Key, out bool wasAdded);
                Term tTerm = Solver.PartialModel.GetSymbCnstType(kv.Key);
                if (!varToTypeMap.ContainsKey(vTerm))
                {
                    varToTypeMap.Add(vTerm, tTerm);
                }
                Encoder.GetVarEnc(vTerm, tTerm);
            }

            InitializeExecuter();

            SetPublisherCoreRules();
            
            SetVarFacts();

            // TODO: handle cardinality terms properly
            foreach (var kvp in cardTerms)
            {
                var inequalities = kvp.Value.ToArray();
                for (int i = 0; i < inequalities.Length - 1; i++)
                {
                    Term first = inequalities[i];

                    SymElement firstEnc, secondEnc;
                    if (!lfp.TryFindValue(first, out firstEnc))
                    {
                        // flag error
                        continue;
                    }

                    for (int j = i + 1; j < inequalities.Length; j++)
                    {
                        Term second = inequalities[j];
                        if (!lfp.TryFindValue(second, out secondEnc))
                        {
                            // flag error
                            continue;
                        }

                        Solver.Context.MkNot(Solver.Context.MkEq(firstEnc.Encoding, secondEnc.Encoding));
                    }
                }
            }
        }

        protected Z3BoolExpr ConvertToCNF(Z3Expr expr, int level)
        {
            Z3BoolExpr top;
            if (cnfMap.TryGetValue(expr, out top))
            {
                return top;
            }
            else
            {
                top = Solver.Context.MkBoolConst("P" + level + "_" + (exprCounter++));
                cnfMap.Add(expr, top);
            }

            Z3BoolExpr negTop = Solver.Context.MkNot(top);
            List<Z3BoolExpr> topExprs = new List<Z3BoolExpr>();
            Z3BoolExpr subExpr;

            if (expr.IsAnd)
            {
                List<Z3BoolExpr> subExprs = new List<Z3BoolExpr>();
                foreach (var arg in expr.Args)
                {
                    subExpr = ConvertToCNF(arg, level + 1);
                    topExprs.Add(Solver.Context.MkOr(negTop, subExpr));
                    subExprs.Add(Solver.Context.MkNot(subExpr));
                }

                subExprs.Add(top);
                topExprs.Add(Solver.Context.MkOr(subExprs));
            }
            else if (expr.IsOr)
            {
                List<Z3BoolExpr> posExprs = new List<Z3BoolExpr>();
                List<Z3BoolExpr> negExprs = new List<Z3BoolExpr>();
                foreach (var arg in expr.Args)
                {
                    subExpr = ConvertToCNF(arg, level + 1);
                    posExprs.Add(subExpr);
                    negExprs.Add(Solver.Context.MkNot(subExpr));
                }

                posExprs.Add(Solver.Context.MkNot(top));
                negExprs.Add(top);
                topExprs.Add(Solver.Context.MkOr(posExprs));
                topExprs.Add(Solver.Context.MkOr(negExprs));
            }
            else if (expr.IsNot)
            {
                subExpr = ConvertToCNF(expr.Args[0], level + 1);
                topExprs.Add(Solver.Context.MkOr(top, subExpr));
                topExprs.Add(Solver.Context.MkOr(Solver.Context.MkNot(top), Solver.Context.MkNot(subExpr)));
            }
            else
            {
                topExprs.Add(Solver.Context.MkOr(negTop, (Z3BoolExpr)expr));
                topExprs.Add(Solver.Context.MkOr(top, Solver.Context.MkNot((Z3BoolExpr)expr)));
            }

            foreach (var curr in topExprs)
            {
                Z3BoolExpr p = Solver.Context.MkBoolConst("UC_" + coreCounter++);
                unsatCoreMap.Add(p, top);
                Solver.Z3Solver.AssertAndTrack(curr, p);
            }

            if (level == 0)
            {
                Solver.Z3Solver.AssertAndTrack(top, Solver.Context.MkBoolConst("UC_" + coreCounter++));
            }

            return top;
        }

        protected void MapCoreToTerms(IEnumerable<Z3BoolExpr> coreExprs)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<Z3BoolExpr, List<Term>> lookupMap = new Dictionary<Z3BoolExpr, List<Term>>();
            foreach (var curr in lfp)
            {
                var mapped = ConvertToCNF(curr.Value.GetSideConstraints(this), -1);
                List<Term> terms;
                if (!lookupMap.TryGetValue(mapped, out terms))
                {
                    terms = new List<Term>();
                    lookupMap.Add(mapped, terms);
                }
                terms.Add(curr.Key);
            }

            List<Z3BoolExpr> processed = new List<Z3BoolExpr>();
            foreach (var item in coreExprs)
            {
                Z3BoolExpr subExpr;
                if (unsatCoreMap.TryGetValue(item, out subExpr) &&
                    !processed.Contains(subExpr))
                {
                    processed.Add(subExpr);
                    List<Term> conflicts;
                    if (lookupMap.TryGetValue(subExpr, out conflicts))
                    {
                        StringBuilder currConflict = new StringBuilder("Conflicts: ");
                        int conflictCount = 0;
                        foreach (var term in conflicts)
                        {
                            if (term.Symbol is UserSymbol &&
                               (!((UserSymbol)term.Symbol).IsAutoGen))
                            {
                                currConflict.Append(Index.ConvertConSymbAll(term) + " ");
                                ++conflictCount;
                            }
                        }
                        if (conflictCount > 0)
                        {
                            currConflict.Append("\n");
                            sb.Append(currConflict);
                        }
                    }
                }
            }

            if (EnvParams.IsSolverPublisherSet(Solver.Env.Parameters))
            {
                EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters, EnvParamKind.Debug_SolverPublisher)
                    .SetUnsatOutput(sb.ToString());
            }
            else
            {
                Console.WriteLine(sb.ToString());
            }
        }

        public bool Solve()
        {
            isSolvable = false;
            bool hasConforms = false;
            bool hasRequires = false;
            string requiresPattern = @"_Query_\d+.requires$";

            foreach (var elem in lfp)
            {
                if (elem.Key.Symbol.PrintableName.EndsWith("conforms"))
                {
                    hasConforms = true;
                }
                else if (Regex.IsMatch(elem.Key.Symbol.PrintableName, requiresPattern))
                {
                    hasRequires = true;
                }
            }

            if (hasConforms && hasRequires)
            {
                var assumptions = new List<Z3BoolExpr>();
                string conformsPattern = @"conforms\d+$";

                foreach (var elem in lfp)
                {
                    if (Regex.IsMatch(elem.Key.Symbol.PrintableName, conformsPattern) ||
                        Regex.IsMatch(elem.Key.Symbol.PrintableName, requiresPattern))
                    {
                        SymElement symbolicConforms = elem.Value;
                        var constraint = symbolicConforms.GetSideConstraints(this);
                        if (constraint != null)
                        {
                            assumptions.Add(constraint);
                        }
                    }
                }

                foreach (var kvp in recursionConstraints)
                {
                    assumptions.Add(kvp.Value);
                }

                assumptions = assumptions.Distinct().ToList();
                if (assumptions.IsEmpty())
                {
                    return true;
                }
                else if (assumptions.Count() == 1)
                {
                    ConvertToCNF(assumptions[0], 0);
                }
                else
                {
                    ConvertToCNF(Solver.Context.MkAnd(assumptions), 0);
                }

                var status = Solver.Z3Solver.Check();
                if (status == Z3.Status.SATISFIABLE)
                {
                    isSolvable = true;
                    var model = Solver.Z3Solver.Model;
                    Dictionary<Z3Expr, Z3Expr> solutionMap = new Dictionary<Z3Expr, Z3Expr>();

                    foreach (var kvp in aliasMap)
                    {
                        if (kvp.Value.Symbol.Kind == SymbolKind.UserCnstSymb)
                        {
                            Term t = kvp.Value;
                            UserCnstSymb symb = (UserCnstSymb)kvp.Value.Symbol;
                            var expr = Encoder.GetVarEnc(t, varToTypeMap[t]);
                            var interp = model.ConstInterp(expr);
                            solutionMap.Add(expr, interp);
                        }
                    }

                    prevSolutions.Add(solutionMap);
                    solutionStrings.Add(GetNewKindConstructors(model));
                }
                else if (status == Z3.Status.UNSATISFIABLE)
                {
                    hasCore = true;
                    coreExprs = Solver.Z3Solver.UnsatCore;

                    if (EnvParams.IsSolverPublisherSet(Solver.Env.Parameters))
                    {
                        EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters, EnvParamKind.Debug_SolverPublisher)
                            .SetUnsatOutput("Model not solvable.");
                    }
                }
            }
            return isSolvable;
        }

        public void GetSolution(int num)
        {
            if (hasCore)
            {
                Console.WriteLine("Model not solvable. Unsat core terms below.");
                MapCoreToTerms(coreExprs);
                return;
            }
            else if (!isSolvable)
            {
                Console.WriteLine("Model not solvable.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            if (num < solutionStrings.Count)
            {
                sb.AppendLine("Solution number " + num);
                foreach (var str in solutionStrings[num])
                {
                    sb.AppendLine(str);
                }
                sb.AppendLine();
                if (EnvParams.IsSolverPublisherSet(Solver.Env.Parameters))
                {
                    EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters, EnvParamKind.Debug_SolverPublisher)
                             .SetExtractOutput(sb.ToString());
                }
                else
                {
                    Console.WriteLine(sb.ToString());
                }
                return;
            }

            Z3.Status status;
            while (solutionStrings.Count <= num)
            {
                // Create next solution
                var prevSolution = prevSolutions[solutionStrings.Count - 1];
                var negated = new List<Z3BoolExpr>();
                foreach (var sol in prevSolution)
                {
                    if (sol.Value != null)
                    {
                        negated.Add(Solver.Context.MkNot(Solver.Context.MkEq(sol.Key, sol.Value)));
                    }
                }

                Solver.Z3Solver.Assert(Solver.Context.MkOr(negated));
                status = Solver.Z3Solver.Check();

                if (status == Z3.Status.SATISFIABLE)
                {
                    var model = Solver.Z3Solver.Model;
                    Dictionary<Z3Expr, Z3Expr> solutionMap = new Dictionary<Z3Expr, Z3Expr>();

                    foreach (var kvp in aliasMap)
                    {
                        if (kvp.Value.Symbol.Kind == SymbolKind.UserCnstSymb)
                        {
                            Term t = kvp.Value;
                            UserCnstSymb symb = (UserCnstSymb)kvp.Value.Symbol;
                            var expr = Encoder.GetVarEnc(t, varToTypeMap[t]);
                            var interp = model.ConstInterp(expr);
                            solutionMap.Add(expr, interp);
                        }
                    }

                    prevSolutions.Add(solutionMap);
                    solutionStrings.Add(GetNewKindConstructors(model));
                }
                else
                {
                    break;
                }
            }

            if (num < solutionStrings.Count)
            {
                sb.AppendLine("Solution number " + num);
                foreach (var str in solutionStrings[num])
                {
                    sb.AppendLine(str);
                }

                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("Could not find solution " + num);
            }

            if (EnvParams.IsSolverPublisherSet(Solver.Env.Parameters))
            {
                EnvParams.GetSolverPublisherParameter(Solver.Env.Parameters, EnvParamKind.Debug_SolverPublisher)
                         .SetExtractOutput(sb.ToString());
            }
            else
            {
                Console.WriteLine(sb.ToString());
            }
        }

        private void PrintSymbolicConstants(Z3.Model model)
        {
            Console.WriteLine("Symbolic constants:");
            foreach (var kvp in aliasMap)
            {
                Console.WriteLine("  " + kvp.Key.Name.Substring(1) + " = " + GetModelInterpretation(kvp.Value, model));
            }
            Console.WriteLine("");
        }

        private List<string> GetNewKindConstructors(Z3.Model model)
        {
            List<string> strs = new List<string>();
            foreach (var kvp in lfp)
            {
                Term t = kvp.Key;
                if (t.Symbol.IsDataConstructor &&
                    !((ConSymb)t.Symbol).IsAutoGen &&
                    Encoder.CanGetEncoding(kvp.Key))
                {
                    if (!kvp.Value.HasConstraints() ||
                        model.Evaluate(kvp.Value.GetSideConstraints(this)).BoolValue == Z3.Z3_lbool.Z3_L_TRUE)
                    {
                        var eval = model.Evaluate(kvp.Value.GetSideConstraints(this));
                        if (eval.BoolValue == Z3.Z3_lbool.Z3_L_TRUE)
                        {
                            strs.Add(GetModelInterpretation(kvp.Key, model));
                        }
                    }
                }
            }

            return strs;
        }

        protected IEnumerable<Term> GetDerivationElements(Term t)
        {
            Set<Term> terms;
            if (!flattenedDervs.TryFindValue(t, out terms))
            {
                terms = new Set<Term>(Term.Compare);
            }

            foreach (var term in terms)
            {
                yield return term;
            }
        }

        public void Execute()
        {
            Activation act;
            var pendingAct = new Set<Activation>(Activation.Compare);
            var pendingFacts = new Map<Term, Set<Derivation>>(Term.Compare);
            LinkedList<CoreRule> untrigList;
            uint maxDepth = Solver.RecursionBound;

            for (int i = 0; i < Rules.StratificationDepth; ++i)
            {
                if (untrigRules.TryFindValue(i, out untrigList))
                {
                    foreach (var r in untrigList)
                    {
                        Term normalized;
                        Z3Expr enc = Encoder.GetTerm(Index.FalseValue, out normalized);
                        SymElement symFalse = new SymElement(normalized, enc, Solver.Context);
                        pendingAct.Add(new Activation(r, -1, symFalse));
                    }
                }

                foreach (var kv in trigIndices)
                {
                    kv.Value.PendAll(pendingAct, i);
                }

                while (pendingAct.Count > 0)
                {
                    bool copyConstraints = true;
                    act = pendingAct.GetSomeElement();
                    pendingAct.Remove(act);
                    int ruleId = act.Rule.RuleId;
                    RemovePositiveConstraints();
                    NegativeConstraintTerms.Clear();

                    act.Rule.Execute(act.Binding1.Term, act.FindNumber, this, KeepDerivations, pendingFacts);

                    // Check for cycles and cut if execution count exceeds max depth
                    if (ruleCycles.ContainsKey(ruleId))
                    {
                        ruleCycles[ruleId]++;
                        if (ruleCycles[ruleId] > maxDepth)
                        {
                            copyConstraints = false;
                        }
                    }

                    foreach (var kv in pendingFacts)
                    {
                        foreach (var derv in kv.Value)
                        {
                            AddPositiveConstraints(derv.Binding1, derv.Binding2);
                            if (copyConstraints)
                            {
                                if (IsConstraintSatisfiable(kv.Key))
                                {
                                    IndexFact(ExtendLFP(kv.Key), kv.Value, pendingAct, i);
                                }
                            }
                            else
                            {
                                AddRecursionConstraint(ruleId);
                            }
                            RemovePositiveConstraints();
                        }
                    }

                    pendingConstraints.Clear();
                    pendingFacts.Clear();
                }
            }
        }

        private Z3BoolExpr CreateConstraint(Z3BoolExpr currConstraint, Z3BoolExpr nextConstraint)
        {
            if (currConstraint == null)
            {
                return nextConstraint;
            }
            else
            {
                return Solver.Context.MkAnd(currConstraint, nextConstraint);
            }
        }

        private bool ShouldCheckConstraints(Term t)
        {
            if (!hasCycles)
            {
                return false;
            }

            bool shouldCheckConstraints = true;
            string pattern = @"conforms\d+$";

            if (t.Symbol.PrintableName.EndsWith("conforms") ||
                Regex.IsMatch(t.Symbol.PrintableName, pattern))
            {
                shouldCheckConstraints = false;
            }

            if (pendingConstraints.IsEmpty() &&
                PositiveConstraintTerms.IsEmpty() &&
                NegativeConstraintTerms.IsEmpty())
            {
                shouldCheckConstraints = false;
            }

            return shouldCheckConstraints;
        }

        public bool IsConstraintSatisfiable(Term term)
        {
            if (!ShouldCheckConstraints(term))
            {
                return true;
            }

            Z3BoolExpr currConstraint = null;

            foreach (Term t in PositiveConstraintTerms)
            {
                var e = lfp[t];
                var nextConstraint = e.GetSideConstraints(this);
                currConstraint = CreateConstraint(currConstraint, nextConstraint);
            }

            foreach (Term t in NegativeConstraintTerms)
            {
                var e = lfp[t];
                var nextConstraint = e.GetSideConstraints(this);
                nextConstraint = Solver.Context.MkNot(nextConstraint);
                currConstraint = CreateConstraint(currConstraint, nextConstraint);
            }

            foreach (var nextConstraint in pendingConstraints)
            {
                currConstraint = CreateConstraint(currConstraint, nextConstraint);
            }

            var status = Solver.Z3Solver.Check(currConstraint);
            if (status == Z3.Status.UNSATISFIABLE)
            {
                return false;
            }

            return true;
        }

        private int GetIntRange(string s)
        {
            int fromBase = 2;
            int index = s.IndexOf("#") + 1;
            string rawNum = s.Substring(index + 1, s.Length - index - 2);
            if (s[index] == 'x')
            {
                fromBase = 16;
            }

            return Convert.ToInt32(rawNum, fromBase);
        }

        private Rational MakeRational(string s)
        {
            if (s.Contains('/'))
            {
                var pieces = s.Split('/');
                var num = new BigInteger(Int32.Parse(pieces[0]));
                var den = new BigInteger(Int32.Parse(pieces[1]));
                return new Rational(num, den);
            }
            else if (s.Contains('.'))
            {
                return new Rational(Double.Parse(s));
            }
            else
            {
                return new Rational(int.Parse(s));
            }
        }

        public string PrefixToInfix(Term t)
        {
            return t.Compute<string>(
                (x, s) => x.Args,
                (x, ch, s) =>
                {
                    if (x.Symbol.Arity == 0)
                    {
                        string str = "";
                        if (x.Symbol.Kind == SymbolKind.UserCnstSymb && x.Symbol.IsVariable)
                        {
                            return x.ToString().Substring(str.LastIndexOf('~') + 1);
                        }
                        else if (x.Symbol.Kind == SymbolKind.BaseCnstSymb)
                        {
                            str = x.Symbol.PrintableName;
                        }

                        return str;
                    }
                    else if (x.Symbol.Kind == SymbolKind.BaseOpSymb)
                    {
                        switch (((BaseOpSymb)x.Symbol).OpKind)
                        {
                            case OpKind.Add:
                                return ch.ElementAt(0) + " + " + ch.ElementAt(1);
                            case OpKind.Sub:
                                return ch.ElementAt(0) + " - " + ch.ElementAt(1);
                            case OpKind.Mul:
                                return ch.ElementAt(0) + " * " + ch.ElementAt(1);
                            case OpKind.Div:
                                return ch.ElementAt(0) + " / " + ch.ElementAt(1);
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else if (x.Symbol.IsDataConstructor)
                    {
                        string str = x.Symbol.PrintableName;
                        str += "(";
                        for (int i = 0; i < ch.Count(); i++)
                        {
                            str += ch.ElementAt(i);
                            str += i == ch.Count() - 1 ? "" : ", ";
                        }
                        str += ")";
                        return str;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                });
        }

        public string GetModelInterpretation(Term t, Z3.Model model)
        {
            if (t.Groundness == Groundness.Ground)
            {
                return t.ToString();
            }
            return t.Compute<string>(
                (x, s) => x.Args,
                (x, ch, s) =>
                {
                    if (x.Symbol.Arity == 0)
                    {
                        string str = "";
                        if (x.Symbol.Kind == SymbolKind.UserCnstSymb && x.Symbol.IsVariable)
                        {
                            var expr = Encoder.GetVarEnc(x, varToTypeMap[x]);
                            var interp = model.ConstInterp(expr);
                            var embedding = Solver.TypeEmbedder.GetEmbedding(expr.Sort);
                            if (embedding is IntRangeEmbedding)
                            {
                                IntRangeEmbedding intRangeEmbedding = (IntRangeEmbedding)embedding;
                                int val = GetIntRange(interp.ToString());
                                str = "" + (intRangeEmbedding.Lower + val);
                            }
                            else if (embedding is EnumEmbedding)
                            {
                                var enumEmbedding = (EnumEmbedding)embedding;
                                int index = (interp == null) ? 0 : ((Z3.BitVecNum)interp.Args[0]).Int;
                                str = enumEmbedding.GetSymbolAtIndex(index);
                            }
                            else if (interp == null)
                            {
                                // If there were no constraints on the term, use the default
                                str = embedding.DefaultMember.Item2.ToString();
                            }
                            else
                            {
                                str = interp.ToString();
                            }

                            return str;
                        }
                        else if (x.Symbol.Kind == SymbolKind.BaseCnstSymb)
                        {
                            str = x.Symbol.PrintableName;
                        }

                        return str;
                    }
                    else if (x.Symbol.Kind == SymbolKind.BaseOpSymb)
                    {
                        Rational r1, r2;
                        string str;
                        switch (((BaseOpSymb)x.Symbol).OpKind)
                        {
                            case OpKind.Add:
                                r1 = MakeRational(ch.ElementAt(0));
                                r2 = MakeRational(ch.ElementAt(1));
                                return (r1 + r2).ToString();
                            case OpKind.Sub:
                                r1 = MakeRational(ch.ElementAt(0));
                                r2 = MakeRational(ch.ElementAt(1));
                                return (r1 - r2).ToString();
                            case OpKind.Mul:
                                r1 = MakeRational(ch.ElementAt(0));
                                r2 = MakeRational(ch.ElementAt(1));
                                return (r1 * r2).ToString();
                            case OpKind.Div:
                                r1 = MakeRational(ch.ElementAt(0));
                                r2 = MakeRational(ch.ElementAt(1));
                                return (r1 / r2).ToString();
                            case OpKind.SymAnd:
                                if (ch.ElementAt(0) == "TRUE" && ch.ElementAt(1) == "TRUE")
                                {
                                    str = "TRUE";
                                }
                                else
                                {
                                    str = "FALSE";
                                }
                                return str;
                            case OpKind.SymAndAll:
                                bool hasFalse = ch.Any(s => s.Equals("FALSE"));
                                str = hasFalse ? "FALSE" : "TRUE";
                                return str;
                            case OpKind.SymOr:
                                bool hasTrue = ch.ElementAt(0) == "TRUE" || ch.ElementAt(1) == "TRUE";
                                str = hasTrue ? "TRUE" : "FALSE";
                                return str;
                            case OpKind.SymOrAll:
                                bool containsTrue = ch.Any(s => s.Equals("TRUE"));
                                str = containsTrue ? "TRUE" : "FALSE";
                                return str;
                            case OpKind.SymMax:
                                r1 = MakeRational(ch.ElementAt(0));
                                r2 = MakeRational(ch.ElementAt(1));
                                return r1 > r2 ? r1.ToString() : r2.ToString();
                            case OpKind.SymMin:
                                r1 = MakeRational(ch.ElementAt(0));
                                r2 = MakeRational(ch.ElementAt(1));
                                return r1 < r2 ? r1.ToString() : r2.ToString();
                            case OpKind.SymMaxAll:
                                r1 = MakeRational(ch.ElementAt(0));
                                for (int i = 1; i < ch.Count(); i++)
                                {
                                    r2 = MakeRational(ch.ElementAt(i));
                                    if (r1 <= r2)
                                    {
                                        r1 = r2;
                                    }
                                }
                                return r1.ToString();
                            case OpKind.SymMinAll:
                                r1 = MakeRational(ch.ElementAt(0));
                                for (int i = 1; i < ch.Count(); i++)
                                {
                                    r2 = MakeRational(ch.ElementAt(i));
                                    if (r1 >= r2)
                                    {
                                        r1 = r2;
                                    }
                                }
                                return r1.ToString();
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else if (x.Symbol.IsDataConstructor)
                    {
                        string str = x.Symbol.PrintableName;
                        str += "(";
                        for (int i = 0; i < ch.Count(); i++)
                        {
                            str += ch.ElementAt(i);
                            str += i == ch.Count() - 1 ? "" : ", ";
                        }
                        str += ")";
                        return str;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                });
        }

        public void InitializeExecuter()
        {
            var optRules = Rules.Optimize();
            ruleCycles = Rules.GetCycles(optRules);
            hasCycles = !ruleCycles.IsEmpty();

            foreach (var r in optRules)
            {
                foreach (var s in r.ComprehensionSymbols)
                {
                    Register(s);
                }

                if (r.Trigger1 == null && r.Trigger2 == null)
                {
                    Register(r);
                    continue;
                }

                if (r.Trigger1 != null)
                {
                    Register(r, 0);
                }

                if (r.Trigger2 != null)
                {
                    Register(r, 1);
                }
            }

            var factDerv = KeepDerivations ? new Derivation[] { new Derivation(Index) } : null;
            foreach (var f in varFacts)
            {
                IndexFact(ExtendLFP(f), factDerv, null, -1);
            }

            Term scTerm;
            bool wasAdded;
            foreach (var sc in Rules.SymbolicConstants)
            {
                scTerm = Index.MkApply(
                    Index.SCValueSymbol,
                    new Term[] { sc, aliasMap[(UserCnstSymb)sc.Symbol]  },
                    out wasAdded);

                IndexFact(ExtendLFP(scTerm), factDerv, null, -1);
            }
        }

        /// <summary>
        /// Extends the lfp with a symbolic element equivalent to t. 
        /// </summary>
        private SymElement ExtendLFP(Term t)
        {
            SymElement e;
            if (!lfp.TryFindValue(t, out e))
            {
                Term normalized = t;
                Z3Expr enc = null;
                if (Encoder.CanGetEncoding(t))
                {
                    enc = Encoder.GetTerm(t, out normalized, this);
                }

                e = new SymElement(normalized, enc, Solver.Context);
                lfp.Add(normalized, e);
            }

            if (!pendingConstraints.IsEmpty() ||
                !PositiveConstraintTerms.IsEmpty() ||
                !NegativeConstraintTerms.IsEmpty())
            {
                HashSet<Z3BoolExpr> a = new HashSet<Z3BoolExpr>(pendingConstraints);
                Set<Term> b = new Set<Term>(Term.Compare, PositiveConstraintTerms);
                Set<Term> c = new Set<Term>(Term.Compare, NegativeConstraintTerms);

                e.AddConstraintData(a, b, c);
            }
            else
            {
                e.SetDirectlyProvable();
            }
            
            LFPChanged(t, e);

            return e;
        }

        private void IndexFact(SymElement t, IEnumerable<Derivation> drs, Set<Activation> pending, int stratum)
        {
            Set<Derivation> dervs;
            if (KeepDerivations)
            {
                if (!facts.TryFindValue(t.Term, out dervs))
                {
                    dervs = new Set<Derivation>(Derivation.Compare);
                    facts.Add(t.Term, dervs);
                }

                foreach (var d in drs)
                {
                    dervs.Add(d);
                    AddFlattenedDerivations(t.Term, d);
                }
            }
            else if (!facts.TryFindValue(t.Term, out dervs))
            {
                facts.Add(t.Term, null);
            }

            LinkedList<SymSubIndex> subindices;
            if (!symbToIndexMap.TryFindValue(t.Term.Symbol, out subindices))
            {
                return;
            }

            foreach (var index in subindices)
            {
                index.TryAdd(t, pending, stratum);
            }
        }
        
        /// <summary>
        /// Register a rule without any finds.
        /// </summary>
        private void Register(CoreRule rule)
        {
            Contract.Requires(rule != null && rule.Trigger1 == null && rule.Trigger2 == null);
            LinkedList<CoreRule> untriggered;
            if (!untrigRules.TryFindValue(rule.Stratum, out untriggered))
            {
                untriggered = new LinkedList<CoreRule>();
                untrigRules.Add(rule.Stratum, untriggered);
            }

            untriggered.AddLast(rule);
        }

        /// <summary>
        /// Register a rule with a findnumber. The trigger may be constrained only by type constraints.
        /// </summary>
        private void Register(CoreRule rule, int findNumber)
        {
            Term trigger;
            Term type;
            switch (findNumber)
            {
                case 0:
                    trigger = rule.Trigger1;
                    type = rule.Find1.Type;
                    break;
                case 1:
                    trigger = rule.Trigger2;
                    type = rule.Find2.Type;
                    break;
                default:
                    throw new Impossible();
            }

            if (!trigger.Symbol.IsVariable)
            {
                Register(rule, trigger, findNumber);
                return;
            }

            Set<Term> patternSet;
            if (typesToTriggersMap.TryFindValue(type, out patternSet))
            {
                foreach (var p in patternSet)
                {
                    trigIndices[p].AddTrigger(rule, findNumber);
                }

                return;
            }

            Set<Symbol> triggerSymbols = new Set<Symbol>(Symbol.Compare);
            type.Visit(
                x => x.Symbol == Index.TypeUnionSymbol ? x.Args : null,
                x =>
                {
                    if (x.Symbol != Index.TypeUnionSymbol)
                    {
                        triggerSymbols.Add(x.Symbol);
                    }
                });

            Term pattern;
            patternSet = new Set<Term>(Term.Compare);
            foreach (var s in triggerSymbols)
            {
                if (s.Kind == SymbolKind.UserSortSymb)
                {
                    pattern = MkPattern(((UserSortSymb)s).DataSymbol, false);
                    patternSet.Add(pattern);
                    Register(rule, pattern, findNumber);
                }
                else
                {
                    Contract.Assert(s.IsDataConstructor || s.IsNonVarConstant);
                    pattern = MkPattern(s, false);
                    patternSet.Add(pattern);
                    Register(rule, pattern, findNumber);
                }
            }

            typesToTriggersMap.Add(type, patternSet);
        }

        /// <summary>
        /// Register a rule triggered by a find in position findnumber
        /// </summary>
        private void Register(CoreRule rule, Term trigger, int findNumber)
        {
            Contract.Requires(rule != null && trigger != null);

            SymSubIndex index;
            if (!trigIndices.TryFindValue(trigger, out index))
            {
                index = new SymSubIndex(this, trigger);
                trigIndices.Add(trigger, index);

                LinkedList<SymSubIndex> subindices;
                if (!symbToIndexMap.TryFindValue(index.Pattern.Symbol, out subindices))
                {
                    subindices = new LinkedList<SymSubIndex>();
                    symbToIndexMap.Add(index.Pattern.Symbol, subindices);
                }

                subindices.AddLast(index);
            }

            index.AddTrigger(rule, findNumber);
        }

        /// <summary>
        /// Register a comprehension symbol
        /// </summary>
        private void Register(Symbol comprSymbol)
        {
            Contract.Requires(comprSymbol != null);
            SymSubIndex index;
            if (!comprIndices.TryFindValue(comprSymbol, out index))
            {
                index = new SymSubIndex(this, MkPattern(comprSymbol, true));
                comprIndices.Add(comprSymbol, index);

                LinkedList<SymSubIndex> subindices;
                if (!symbToIndexMap.TryFindValue(comprSymbol, out subindices))
                {
                    subindices = new LinkedList<SymSubIndex>();
                    symbToIndexMap.Add(comprSymbol, subindices);
                }

                subindices.AddLast(index);
            }
        }

        /// <summary>
        /// Makes a subindex pattern for a symbol s. 
        /// If the symb is a comprehension symbol then the pattern is s(^0,...,^n-1, *0).
        /// Otherwise it is s(*0,...,*n).
        /// </summary>
        private Term MkPattern(Symbol s, bool isCompr)
        {
            Contract.Requires(s != null && (!isCompr || s.Arity > 0));
            bool wasAdded;
            var args = new Term[s.Arity];

            if (isCompr)
            {
                for (int i = 0; i < s.Arity - 1; ++i)
                {
                    args[i] = Index.MkVar(PatternVarBoundPrefix + i.ToString(), true, out wasAdded);
                }

                args[s.Arity - 1] = Index.MkVar(PatternVarUnboundPrefix + "0", true, out wasAdded);
            }
            else
            {
                for (int i = 0; i < s.Arity; ++i)
                {
                    args[i] = Index.MkVar(PatternVarUnboundPrefix + i.ToString(), true, out wasAdded);
                }
            }

            return Index.MkApply(s, args, out wasAdded);
        }

        private uint symbCnstId = 0;

        private Term MkSymbolicTerm(Symbol s)
        {
            Contract.Requires(s != null);
            bool wasAdded;
            var args = new Term[s.Arity];
            
            for (int i = 0; i < s.Arity; ++i)
            {
                UserCnstSymb symbCnst;
                AST<Id> id;
                args[i] = Index.MkSymbolicConstant("sc" + symbCnstId++, out symbCnst, out id);
                Solver.PartialModel.AddAlias(symbCnst, id.Node);
            }

            return Index.MkApply(s, args, out wasAdded);
        }

        public IEnumerable<Term> Query(Term comprTerm, out int nResults)
        {
            Contract.Requires(comprTerm != null);
            var subIndex = comprIndices[comprTerm.Symbol];
            var projection = new Term[comprTerm.Symbol.Arity - 1];

            //// Console.Write("Query {0}: [", subIndex.Pattern.Debug_GetSmallTermString());
            for (int i = 0; i < comprTerm.Symbol.Arity - 1; ++i)
            {
                //// Console.Write(" " + comprTerm.Args[i].Debug_GetSmallTermString());
                projection[i] = comprTerm.Args[i];
            }

            //// Console.WriteLine(" ]");

            return subIndex.Query(projection, out nResults);
        }

        public IEnumerable<Term> Query(Term pattern, Term[] projection)
        {
            /*
            Console.Write("Query {0}: [", pattern.Debug_GetSmallTermString());
            foreach (var t in projection)
            {
                Console.Write(" " + t.Debug_GetSmallTermString());
            }

            Console.WriteLine(" ]");
            */

            return trigIndices[pattern].Query(projection);
        }

        public IEnumerable<Term> Query(Term type, Term binding)
        {
            /*
            if (binding == null)
            {
                Console.WriteLine("Query type {0}", type.Debug_GetSmallTermString());
            }
            else
            {
                Console.WriteLine("Query type {0} = {1}", type.Debug_GetSmallTermString(), binding.Debug_GetSmallTermString());
            }
            */

            var patterns = typesToTriggersMap[type];
            if (binding != null)
            {
                foreach (var p in patterns)
                {
                    if (p.Symbol == binding.Symbol && Exists(binding))
                    {
                        yield return binding;
                        yield break;
                    }
                }

                yield break;
            }

            foreach (var p in patterns)
            {
                var results = trigIndices[p].Query(new Term[0]);
                foreach (var t in results)
                {
                    yield return t;
                }
            }

            yield break;
        }

        public Map<Term, Term> GetBindings(Term tA, Term tB, Map<Term, Set<Term>> partitions)
        {
            Map<Term, Term> bindings = new Map<Term, Term>(Term.Compare);
            Set<Term> lhsVars = new Set<Term>(Term.Compare);
            Set<Term> rhsVars = new Set<Term>(Term.Compare);

            // Collect all variables in the LHS term
            tA.Compute<Term>(
                (x, s) => x.Groundness == Groundness.Variable ? x.Args : null,
                (x, ch, s) =>
                {
                    if (x.Groundness != Groundness.Variable)
                    {
                        return null;
                    }
                    else if (x.Symbol.IsVariable)
                    {
                        lhsVars.Add(x);
                    }
                    else
                    {
                        foreach (var t in x.Args)
                        {
                            if (t.Symbol.IsVariable)
                            {
                                lhsVars.Add(t);
                            }
                        }
                    }

                    return null;
                });

            // Collect all variables in the RHS term
            tB.Compute<Term>(
                (x, s) =>
                {
                    if (x.Symbol.Kind == SymbolKind.BaseOpSymb)
                    {
                        return null; // don't descend into base op symbols
                    }
                    else
                    {
                        return x.Groundness == Groundness.Variable ? x.Args : null;
                    }
                },
                (x, ch, s) =>
                {
                    if (x.Groundness != Groundness.Variable)
                    {
                        return null;
                    }
                    else if (x.Symbol.IsVariable || x.Symbol.Kind == SymbolKind.BaseOpSymb || x.Symbol.Kind == SymbolKind.ConSymb)
                    {
                        rhsVars.Add(x);
                    }
                    else
                    {
                        foreach (var t in x.Args)
                        {
                            if (t.Symbol.IsVariable)
                            {
                                rhsVars.Add(t);
                            }
                        }
                    }

                    return null;
                });

            foreach (var part in partitions)
            {
                Set<Term> constants = new Set<Term>(Term.Compare);
                Set<Term> lhsPartVars = new Set<Term>(Term.Compare);
                Set<Term> rhsPartVars = new Set<Term>(Term.Compare);
                Set<Term> rhsPartGround = new Set<Term>(Term.Compare);

                foreach (var term in part.Value)
                {
                    if (term.Symbol.IsNonVarConstant)
                    {
                        constants.Add(term);
                    }
                    else if (lhsVars.Contains(term))
                    {
                        lhsPartVars.Add(term);
                    }
                    else if (rhsVars.Contains(term))
                    {
                        rhsPartVars.Add(term);
                    }
                    else if (term.Symbol.IsDataConstructor && term.Groundness == Groundness.Ground)
                    {
                        rhsPartGround.Add(term);
                    }
                }

                if (!constants.IsEmpty())
                {
                    Term constant = constants.First();
                    foreach (var rhs in rhsPartVars)
                    {
                        this.PendEqualityConstraint(rhs, constant);
                    }
                }

                if (!rhsPartVars.IsEmpty())
                {
                    foreach (var lhsVar in lhsPartVars)
                    {
                        bindings.Add(lhsVar, rhsPartVars.First());
                    }
                }
                else if (!constants.IsEmpty())
                {
                    foreach (var lhsVar in lhsPartVars)
                    {
                        bindings.Add(lhsVar, constants.First());
                    }
                }
                else if (!rhsPartGround.IsEmpty())
                {
                    foreach (var lhsVar in lhsPartVars)
                    {
                        bindings.Add(lhsVar, rhsPartGround.First());
                    }
                }
            }

            return bindings;
        }

        internal class SymSubIndex
        {
            public static readonly Term[] EmptyProjection = new Term[0];

            private SymExecuter Executer;

            private int nBoundVars;
            private Matcher patternMatcher;

            /// <summary>
            /// Map from strata to rules triggered by this pattern.
            /// </summary>
            private Map<int, LinkedList<Tuple<CoreRule, int>>> triggers =
                new Map<int, LinkedList<Tuple<CoreRule, int>>>((x, y) => x - y);

            /// <summary>
            /// Simple collection of facts. No pre-unification optimization.
            /// </summary>
            //private Set<SymElement> facts = new Set<SymElement>(SymElement.Compare);
            private Map<Term[], Set<SymElement>> facts = new Map<Term[], Set<SymElement>>(Compare);

            /// <summary>
            /// The pattern of this subindex.
            /// </summary>
            public Term Pattern
            {
                get;
                private set;
            }

            public SymSubIndex(SymExecuter executer, Term pattern)
            {
                Executer = executer;
                Pattern = pattern;

                patternMatcher = new Matcher(pattern);
                nBoundVars = 0;
                foreach (var kv in patternMatcher.CurrentBindings)
                {
                    if (((UserSymbol)kv.Key.Symbol).Name[0] == SymExecuter.PatternVarBoundPrefix)
                    {
                        ++nBoundVars;
                    }
                }
            }

            public IEnumerable<Term> Query(Term[] projection)
            {
                if (projection.IsEmpty())
                {
                    Set<SymElement> subindex;
                    if (!facts.TryFindValue(projection, out subindex))
                    {
                        yield break;
                    }

                    foreach (var t in subindex)
                    {
                        yield return t.Term;
                    }
                }
                else
                {
                    Set<SymElement> allFacts = new Set<SymElement>(SymElement.Compare);
                    foreach (var kvp in facts)
                    {
                        bool isUnifiable = true;
                        for (int i = 0; i < kvp.Key.Length; i++)
                        {
                            if (!Unifier.IsUnifiable(projection[i], kvp.Key[i]))
                            {
                                isUnifiable = false;
                                break;
                            }
                        }

                        if (isUnifiable)
                        {
                            foreach (SymElement e in kvp.Value)
                            {
                                allFacts.Add(e);
                            }
                        }
                    }

                    foreach (var t in allFacts)
                    {
                        yield return t.Term;
                    }
                }
            }

            public IEnumerable<Term> Query(Term[] projection, out int nResults)
            {
                Set<SymElement> subindex;
                if (!facts.TryFindValue(projection, out subindex))
                {
                    nResults = 0;
                }
                else
                {
                    nResults = subindex.Count;
                }

                return Query(projection);
            }

            public void AddTrigger(CoreRule rule, int findNumber)
            {
                LinkedList<Tuple<CoreRule, int>> rules;
                if (!triggers.TryFindValue(rule.Stratum, out rules))
                {
                    rules = new LinkedList<Tuple<CoreRule, int>>();
                    triggers.Add(rule.Stratum, rules);
                }

                rules.AddLast(new Tuple<CoreRule, int>(rule, findNumber));
            }

            /// <summary>
            /// Tries to add this term to the subindex. Returns true if t unifies with pattern.
            /// If pending is non-null, then pends rules that are triggered by this term.
            /// </summary>
            public bool TryAdd(SymElement t, Set<Activation> pending, int stratum)
            {
                Contract.Requires(t != null && t.Term.Groundness != Groundness.Type);
                Contract.Requires(t.Term.Owner == Pattern.Owner);

                Map<Term, Set<Term>> partitions = new Map<Term, Set<Term>>(Term.Compare);

                if (!Unifier.IsUnifiable(Pattern, t.Term, true, partitions))
                {
                    //// Terms t must unify with the pattern for insertion to succeed.
                    //// Pattern is already standardized apart from t.
                    return false;
                }

                Term[] projection;

                if (nBoundVars == 0)
                {
                    projection = EmptyProjection;
                }
                else
                {
                    var bindings = Executer.GetBindings(Pattern, t.Term, partitions);
                    int i = 0;
                    projection = new Term[nBoundVars];
                    foreach (var kv in patternMatcher.CurrentBindings)
                    {
                        if (((UserSymbol)kv.Key.Symbol).Name[0] == SymExecuter.PatternVarBoundPrefix)
                        {
                            projection[i++] = bindings[kv.Key];
                        }
                    }
                }

                Set<SymElement> subset;
                if (!facts.TryFindValue(projection, out subset))
                {
                    subset = new Set<SymElement>(SymElement.Compare);
                    facts.Add(projection, subset);
                }

                subset.Add(t);

                if (pending != null)
                {
                    LinkedList<Tuple<CoreRule, int>> triggered;
                    if (triggers.TryFindValue(stratum, out triggered))
                    {
                        foreach (var trig in triggered)
                        {
                            pending.Add(new Activation(trig.Item1, trig.Item2, t));
                        }
                    }
                }

                return true;
            }

            /// <summary>
            /// Generate pending activations for all rules triggered in this stratum. 
            /// </summary>
            public void PendAll(Set<Activation> pending, int stratum)
            {
                LinkedList<Tuple<CoreRule, int>> triggered;
                if (!triggers.TryFindValue(stratum, out triggered))
                {
                    return;
                }

                foreach (var kv in facts)
                {
                    foreach (var t in kv.Value)
                    {
                        foreach (var trig in triggered)
                        {
                            pending.Add(new Activation(trig.Item1, trig.Item2, t));
                        }
                    }
                }
            }

            public static int Compare(Term[] v1, Term[] v2)
            {
                return EnumerableMethods.LexCompare<Term>(v1, v2, Term.Compare);
            }
        }
    }
}
