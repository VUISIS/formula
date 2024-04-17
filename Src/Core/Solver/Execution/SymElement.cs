﻿namespace Microsoft.Formula.Solver
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using System.Threading;

    using API;
    using API.ASTQueries;
    using API.Nodes;
    using Common;
    using Common.Extras;
    using Common.Rules;
    using Common.Terms;

    //// Aliases for Z3 types to avoid clashes
    using Z3Expr = Microsoft.Z3.Expr;
    using Z3BoolExpr = Microsoft.Z3.BoolExpr;
    using Z3Context = Microsoft.Z3.Context;

    /// <summary>
    /// A symbolic element is a term, possibly with symbolic constants, that exists in the LFP of the program
    /// only for those valuations where the side constraint is satisfied. For backtracking purposes, side constraints
    /// map from a state index to a side constraint. The overall side constraint is a disjunction of the map's image.
    /// </summary>
    internal class SymElement
    {
        public Term Term
        {
            get;
            private set;
        }

        public Z3Expr Encoding
        {
            get;
            private set;
        }

        public Map<int, Z3BoolExpr> SideConstraints
        {
            get;
            private set;
        }

        public bool HasConstraints()
        {
            return !constraintData.IsEmpty();
        }

        public bool IsDirectlyProvable
        {
            get;
            private set;
        }

        public void SetDirectlyProvable()
        {
            IsDirectlyProvable = true;
        }

        public List<ConstraintData> GetConstraintData()
        {
            return constraintData;
        }

        private List<ConstraintData> constraintData = new List<ConstraintData>();

        private HashSet<Z3BoolExpr> cachedConstraints = new HashSet<Z3BoolExpr>();

        public void AddConstraintData(HashSet<Z3BoolExpr> exprs, Set<Term> posTerms, Set<Tuple<Term, Set<Tuple<Term, Term>>>> negTerms)
        {
            bool needToAdd = true;

            foreach (var item in constraintData)
            {
                if (item.IsSameConstraintData(exprs, posTerms, negTerms))
                {
                    needToAdd = false;
                    break;
                }
            }

            if (needToAdd)
            {
                var data = new ConstraintData(exprs, posTerms, negTerms);
                constraintData.Add(data);
            }
        }

        public bool ContainsConstraint(Z3BoolExpr expr)
        {
            return cachedConstraints.Contains(expr);
        }

        private Z3BoolExpr CreateAndCacheConstraint(SymExecuter executer, Z3BoolExpr currConstraint, Z3BoolExpr nextConstraint)
        {
            cachedConstraints.Add(nextConstraint);

            if (currConstraint == null)
            {
                return nextConstraint;
            }
            else
            {
                return executer.Solver.Context.MkAnd(currConstraint, nextConstraint);
            }
        }

        private Z3BoolExpr GetSideConstraints(SymExecuter executer, Set<Term> processed)
        {
            List<Z3BoolExpr> constraints = new List<Z3BoolExpr>();
            Term t = this.Term;
            processed.Add(t);
            SymElement next;
            Z3BoolExpr topConstraint = null;
            Z3BoolExpr currConstraint = null;
            Set<Term> localProcessed = null;

            if (IsDirectlyProvable)
            {
                return executer.Solver.Context.MkTrue();
            }

            foreach (var constraint in constraintData)
            {
                currConstraint = null;
                localProcessed = new Set<Term>(Term.Compare, processed);
                foreach (var posTerm in constraint.PosConstraints)
                {
                    if (!localProcessed.Contains(posTerm) &&
                        executer.GetSymbolicTerm(posTerm, out next))
                    {
                        var nextConstraint = next.GetSideConstraints(executer, localProcessed);
                        if (nextConstraint != null)
                        {
                            currConstraint = CreateAndCacheConstraint(executer, currConstraint, nextConstraint);
                        }
                    }
                }

                localProcessed = new Set<Term>(Term.Compare, processed);
                foreach (var negTerm in constraint.NegConstraints)
                {
                    if (!processed.Contains(negTerm.Item1) &&
                        executer.GetSymbolicTerm(negTerm.Item1, out next))
                    {
                        var nextConstraint = next.GetSideConstraints(executer, localProcessed);

                        Z3BoolExpr expr;
                        foreach (var item in negTerm.Item2)
                        {
                            Term normalized;
                            var expr1 = executer.Encoder.GetTerm(item.Item1, out normalized);
                            var expr2 = executer.Encoder.GetTerm(item.Item2, out normalized);
                            expr = executer.Solver.Context.MkEq(expr1, expr2);
                            nextConstraint = executer.Solver.Context.MkAnd(nextConstraint, expr);
                        }

                        nextConstraint = executer.Solver.Context.MkNot(nextConstraint);
                        currConstraint = CreateAndCacheConstraint(executer, currConstraint, nextConstraint);
                    }
                }

                foreach (var nextConstraint in constraint.DirConstraints)
                {
                    currConstraint = CreateAndCacheConstraint(executer, currConstraint, nextConstraint);
                }

                if (topConstraint == null)
                {
                    topConstraint = currConstraint;
                }
                else
                {
                    topConstraint = executer.Solver.Context.MkOr(topConstraint, currConstraint);
                }
            }

            return topConstraint;
        }

        public Z3BoolExpr GetSideConstraints(SymExecuter executer)
        {
            List<Z3BoolExpr> constraints = new List<Z3BoolExpr>();
            Set<Term> processed = new Set<Term>(Term.Compare);
            Term t = this.Term;
            SymElement next;
            Z3BoolExpr topConstraint = null;
            Z3BoolExpr currConstraint = null;
            Z3Context context = executer.Solver.Context;

            if (IsDirectlyProvable)
            {
                return executer.Solver.Context.MkTrue();
            }

            foreach (var constraint in constraintData)
            {
                currConstraint = null;
                foreach (var posTerm in constraint.PosConstraints)
                {
                    processed.Clear();
                    processed.Add(t);
                    if (executer.GetSymbolicTerm(posTerm, out next))
                    {
                        var nextConstraint = next.GetSideConstraints(executer, processed);
                        currConstraint = CreateAndCacheConstraint(executer, currConstraint, nextConstraint);
                    }
                }

                foreach (var negTerm in constraint.NegConstraints)
                {
                    processed.Clear();
                    processed.Add(t);
                    if (executer.GetSymbolicTerm(negTerm.Item1, out next))
                    {
                        var nextConstraint = next.GetSideConstraints(executer, processed);
                        Z3BoolExpr expr;
                        foreach (var item in negTerm.Item2)
                        {
                            Term normalized;
                            var expr1 = executer.Encoder.GetTerm(item.Item1, out normalized);
                            var expr2 = executer.Encoder.GetTerm(item.Item2, out normalized);
                            expr = executer.Solver.Context.MkEq(expr1, expr2);
                            nextConstraint = executer.Solver.Context.MkAnd(nextConstraint, expr);
                        }

                        nextConstraint = context.MkNot(nextConstraint);
                        currConstraint = CreateAndCacheConstraint(executer, currConstraint, nextConstraint);
                    }
                }

                foreach (var nextConstraint in constraint.DirConstraints)
                {
                    currConstraint = CreateAndCacheConstraint(executer, currConstraint, nextConstraint);
                }

                if (topConstraint == null)
                {
                    topConstraint = currConstraint;
                }
                else
                {
                    topConstraint = executer.Solver.Context.MkOr(topConstraint, currConstraint);
                }
            }

            return topConstraint;
        }

        /// <summary>
        ///  The earliest index at which the side constraint is known to be a tautology.
        /// </summary>
        public int TautologyNumber
        {
            get;
            private set;
        }

        public SymElement(Term term, Z3Expr encoding, Z3Context context)
        {
            Contract.Requires(term != null && encoding != null && context != null);
            Term = term;
            Encoding = encoding;
            SideConstraints = new Map<int, Z3BoolExpr>(Compare);
            IsDirectlyProvable = false;
        }

        /// <summary>
        /// Removes all constraints introduced at or after index.
        /// </summary>
        /// <param name="index"></param>
        public void ContractSideConstraint(int index)
        {
            var deleteList = new LinkedList<int>();
            foreach (var kv in SideConstraints.GetEnumerable(index))
            {
                deleteList.AddLast(kv.Key);
            }

            foreach (var k in deleteList)
            {
                SideConstraints.Remove(k);
            }
        }

        /// <summary>
        /// "null" is the smallest symbolic element.
        /// </summary>
        public static int Compare(SymElement e1, SymElement e2)
        {
            if (e1 == null && e2 == null)
            {
                return 0;
            }
            else if (e1 == null && e2 != null)
            {
                return -1;
            }
            else if (e1 != null && e2 == null)
            {
                return 1;
            }
            else
            {
                return Term.Compare(e1.Term, e2.Term);
            }
        }

        public void Debug_Print()
        {
            Console.WriteLine(Term.Debug_GetSmallTermString());
        }

        private static int Compare(int x, int y)
        {
            if (x < y)
            {
                return -1;
            }
            else if (x > y)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    internal class ConstraintData
    {
        public HashSet<Z3BoolExpr> DirConstraints
        {
            get;
            private set;
        }

        public Set<Term> PosConstraints
        {
            get;
            private set;
        }

        public Set<Tuple<Term, Set<Tuple<Term, Term>>>> NegConstraints
        {
            get;
            private set;
        }

        public ConstraintData(HashSet<Z3BoolExpr> exprs, Set<Term> posTerms, Set<Tuple<Term, Set<Tuple<Term, Term>>>> negTerms)
        {
            DirConstraints = exprs;
            PosConstraints = posTerms;
            NegConstraints = negTerms;
        }

        public bool IsSameConstraintData(HashSet<Z3BoolExpr> exprs, Set<Term> posTerms, Set<Tuple<Term, Set<Tuple<Term, Term>>>> negTerms)
        {
            if (DirConstraints.SetEquals(exprs) &&
                PosConstraints.IsSameSet(posTerms) &&
                NegConstraints.IsSameSet(negTerms))
            {
                return true;
            }

            return false;
        }
    }
}
