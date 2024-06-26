﻿namespace Microsoft.Formula.Common.Terms
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using System.Threading;

    using API;
    using API.Nodes;
    using Compiler;
    using Rules;
    using Solver;

    public sealed class BaseOpSymb : Symbol
    {
        private int arity;

        public override SymbolKind Kind
        {
            get { return SymbolKind.BaseOpSymb; }
        }

        public override bool IsSelect
        {
            get
            {
                return OpKind is ReservedOpKind && ((ReservedOpKind)OpKind) == ReservedOpKind.Select;
            }
        }

        public override bool IsTypeUnn
        {
            get
            {
                return OpKind is ReservedOpKind && ((ReservedOpKind)OpKind) == ReservedOpKind.TypeUnn;
            }
        }

        public override bool IsRange
        {
            get
            {
                return OpKind is ReservedOpKind && ((ReservedOpKind)OpKind) == ReservedOpKind.Range;
            }
        }

        public override bool IsRelabel
        {
            get
            {
                return OpKind is ReservedOpKind && ((ReservedOpKind)OpKind) == ReservedOpKind.Relabel;
            }
        }

        public override bool IsSymAnd
        {
            get
            {
                return OpKind is OpKind && ((OpKind)OpKind) == API.OpKind.SymAnd;
            }
        }

        public override bool IsSymAndAll
        {
            get
            {
                return OpKind is OpKind && ((OpKind)OpKind) == API.OpKind.SymAndAll;
            }
        }
        public override bool IsSymOr
        {
            get
            {
                return OpKind is OpKind && ((OpKind)OpKind) == API.OpKind.SymOr;
            }
        }
        
        public override bool IsSymOrAll
        {
            get
            {
                return OpKind is OpKind && ((OpKind)OpKind) == API.OpKind.SymOrAll;
            }
        }

        public override bool IsSymCount
        {
            get
            {
                return OpKind is OpKind && ((OpKind)OpKind) == API.OpKind.SymCount;
            }
        }

        public override bool IsSymMax
        {
            get
            {
                return OpKind is OpKind && ((OpKind)OpKind) == API.OpKind.SymMax;
            }
        }
        
        public override bool IsSymMaxAll
        {
            get
            {
                return OpKind is OpKind && ((OpKind)OpKind) == API.OpKind.SymMaxAll;
            }
        }

        
        public override bool IsSymMin
        {
            get
            {
                return OpKind is OpKind && ((OpKind)OpKind) == API.OpKind.SymMin;
            }
        }
        
        public override bool IsSymMinAll
        {
            get
            {
                return OpKind is OpKind && ((OpKind)OpKind) == API.OpKind.SymMinAll;
            }
        }

        public override bool IsReservedOperation
        {
            get { return OpKind is ReservedOpKind; }
        }

        public object OpKind
        {
            get;
            private set;
        }

        public override int Arity
        {
	        get { return arity; }
        }

        public override string PrintableName
        {
            get 
            {
                if (OpKind is OpKind)
                {
                    API.ASTQueries.OpStyleKind style;
                    return API.ASTQueries.ASTSchema.Instance.ToString((OpKind)OpKind, out style);
                }
                else if (OpKind is RelKind)
                {
                    return API.ASTQueries.ASTSchema.Instance.ToString((RelKind)OpKind);
                }
                else if (OpKind is ReservedOpKind)
                {
                    return API.ASTQueries.ASTSchema.Instance.ToString((ReservedOpKind)OpKind);
                }
                else
                {
                    throw new NotImplementedException();
                }            
            }
        }

        internal Func<Node, List<Flag>, bool> Validator
        {
            get;
            private set;
        }

        internal Func<TermIndex, Term[], Term[]> UpwardApprox
        {
            get;
            private set;
        }

        internal Func<TermIndex, Term[], Term[]> DownwardApprox
        {
            get;
            private set;
        }

        internal Func<Executer, Bindable[], Term> Evaluator
        {
            get;
            private set;
        }

        internal Func<SymExecuter, Bindable[], Term> SymEvaluator
        {
            get;
            private set;
        }

        /// <summary>
        /// Some base ops require additional implicit constraints. For instance, 
        /// x / y implies y != 0. The app constrainer can return an additional 
        /// set of constraints given the arguments to the application. The arguments
        /// to the additional constraints should contain only constants or subterms
        /// of the application arguments.
        /// </summary>
        internal Func<TermIndex, Term[], IEnumerable<Tuple<RelKind, Term, Term>>> AppConstrainer
        {
            get;
            private set;
        }
        
        internal BaseOpSymb(
            OpKind opKind, 
            int arity, 
            Func<Node, List<Flag>, bool> validator,
            Func<TermIndex, Term[], Term[]> upApprox,
            Func<TermIndex, Term[], Term[]> downApprox,
            Func<Executer, Bindable[], Term> evaluator,
            Func<TermIndex, Term[], IEnumerable<Tuple<RelKind, Term, Term>>> appConstrainer = null,
            Func<SymExecuter, Bindable[], Term> symEvaluator = null)
        {
            Contract.Requires(validator != null && upApprox != null && downApprox != null);
            Contract.Requires(evaluator != null);

            OpKind = opKind;
            Validator = validator;
            UpwardApprox = upApprox;
            DownwardApprox = downApprox;
            Evaluator = evaluator;
            AppConstrainer = appConstrainer == null ? EmptyConstrainer : appConstrainer;
            SymEvaluator = symEvaluator;
            this.arity = arity;
        }

        internal BaseOpSymb(
            ReservedOpKind opKind, 
            int arity, Func<Node, 
            List<Flag>, bool> validator,
            Func<TermIndex, Term[], Term[]> upApprox,
            Func<TermIndex, Term[], Term[]> downApprox,
            Func<Executer, Bindable[], Term> evaluator,
            Func<SymExecuter, Bindable[], Term> symEvaluator = null)
        {
            Contract.Requires(validator != null);
            OpKind = opKind;
            Validator = validator;
            UpwardApprox = upApprox;
            DownwardApprox = downApprox;
            Evaluator = evaluator;
            SymEvaluator = symEvaluator;
            this.arity = arity;
        }

        internal BaseOpSymb(
            RelKind opKind, 
            int arity, 
            Func<Node, List<Flag>, bool> validator,
            Func<TermIndex, Term[], Term[]> upApprox,
            Func<TermIndex, Term[], Term[]> downApprox,
            Func<Executer, Bindable[], Term> evaluator,
            Func<SymExecuter, Bindable[], Term> symEvaluator = null)
        {
            Contract.Requires(validator != null && upApprox != null && downApprox != null);
            OpKind = opKind;
            Validator = validator;
            UpwardApprox = upApprox;
            DownwardApprox = downApprox;
            Evaluator = evaluator;
            SymEvaluator = symEvaluator;
            this.arity = arity;
        }

        private IEnumerable<Tuple<RelKind, Term, Term>> EmptyConstrainer(TermIndex index, Term[] args)
        {
            yield break;
        }
    }
}
