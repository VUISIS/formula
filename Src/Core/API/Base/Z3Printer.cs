using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Z3;

namespace Microsoft.Formula.API.Base;

public static class Z3Printer
{
    public static Dictionary<Z3_decl_kind, int> z3Precedence = new Dictionary<Z3_decl_kind, int>()
    {
        { Z3_decl_kind.Z3_OP_POWER, 0 },
        { Z3_decl_kind.Z3_OP_UMINUS, 1 }, { Z3_decl_kind.Z3_OP_BNEG, 1 }, { Z3_decl_kind.Z3_OP_BNOT, 1 },
        { Z3_decl_kind.Z3_OP_MUL, 2 }, { Z3_decl_kind.Z3_OP_DIV, 2 }, { Z3_decl_kind.Z3_OP_IDIV, 2 },
        { Z3_decl_kind.Z3_OP_MOD, 2 }, { Z3_decl_kind.Z3_OP_BMUL, 2 }, { Z3_decl_kind.Z3_OP_BSDIV, 2 },
        { Z3_decl_kind.Z3_OP_BSMOD, 2 },
        { Z3_decl_kind.Z3_OP_ADD, 3 }, { Z3_decl_kind.Z3_OP_SUB, 3 }, { Z3_decl_kind.Z3_OP_BADD, 3 },
        { Z3_decl_kind.Z3_OP_BSUB, 3 },
        { Z3_decl_kind.Z3_OP_BASHR, 4 }, { Z3_decl_kind.Z3_OP_BSHL, 4 },
        { Z3_decl_kind.Z3_OP_BAND, 5 },
        { Z3_decl_kind.Z3_OP_BXOR, 6 },
        { Z3_decl_kind.Z3_OP_BOR, 7 },
        { Z3_decl_kind.Z3_OP_LE, 8 }, { Z3_decl_kind.Z3_OP_LT, 8 }, { Z3_decl_kind.Z3_OP_GE, 8 },
        { Z3_decl_kind.Z3_OP_GT, 8 }, { Z3_decl_kind.Z3_OP_EQ, 8 }, { Z3_decl_kind.Z3_OP_SLEQ, 8 },
        { Z3_decl_kind.Z3_OP_SLT, 8 }, { Z3_decl_kind.Z3_OP_SGEQ, 8 }, { Z3_decl_kind.Z3_OP_SGT, 8 },
        { Z3_decl_kind.Z3_OP_IFF, 8 },

        { Z3_decl_kind.Z3_OP_FPA_NEG, 1 },
        { Z3_decl_kind.Z3_OP_FPA_MUL, 2 }, { Z3_decl_kind.Z3_OP_FPA_DIV, 2 }, { Z3_decl_kind.Z3_OP_FPA_REM, 2 },
        { Z3_decl_kind.Z3_OP_FPA_FMA, 2 },
        { Z3_decl_kind.Z3_OP_FPA_ADD, 3 }, { Z3_decl_kind.Z3_OP_FPA_SUB, 3 },
        { Z3_decl_kind.Z3_OP_FPA_LE, 8 }, { Z3_decl_kind.Z3_OP_FPA_LT, 8 }, { Z3_decl_kind.Z3_OP_FPA_GE, 8 },
        { Z3_decl_kind.Z3_OP_FPA_GT, 8 }, { Z3_decl_kind.Z3_OP_FPA_EQ, 8 }
    };

     public static Dictionary<Z3_decl_kind, string> opToStrMap = new Dictionary<Z3_decl_kind, string>()
     {
        { Z3_decl_kind.Z3_OP_TRUE, "True" },
        { Z3_decl_kind.Z3_OP_FALSE, "False" },
        { Z3_decl_kind.Z3_OP_EQ, "==" },
        { Z3_decl_kind.Z3_OP_DISTINCT, "Distinct" },
        { Z3_decl_kind.Z3_OP_ITE, "If" },
        { Z3_decl_kind.Z3_OP_AND, "And" },
        { Z3_decl_kind.Z3_OP_OR, "Or" },
        { Z3_decl_kind.Z3_OP_IFF, "==" },
        { Z3_decl_kind.Z3_OP_XOR, "Xor" },
        { Z3_decl_kind.Z3_OP_NOT, "Not" },
        { Z3_decl_kind.Z3_OP_IMPLIES, "Implies" },

        { Z3_decl_kind.Z3_OP_IDIV, "/" },
        { Z3_decl_kind.Z3_OP_MOD, "%" },
        { Z3_decl_kind.Z3_OP_TO_REAL, "ToReal" },
        { Z3_decl_kind.Z3_OP_TO_INT, "ToInt" },
        { Z3_decl_kind.Z3_OP_POWER, "**" },
        { Z3_decl_kind.Z3_OP_IS_INT, "IsInt" },
        { Z3_decl_kind.Z3_OP_BADD, "+" },
        { Z3_decl_kind.Z3_OP_BSUB, "-" },
        { Z3_decl_kind.Z3_OP_BMUL, "*" },
        { Z3_decl_kind.Z3_OP_BOR, "|" },
        { Z3_decl_kind.Z3_OP_BAND, "&" },
        { Z3_decl_kind.Z3_OP_BNOT, "~" },
        { Z3_decl_kind.Z3_OP_BXOR, "^" },
        { Z3_decl_kind.Z3_OP_BNEG, "-" },
        { Z3_decl_kind.Z3_OP_BUDIV, "UDiv" },
        { Z3_decl_kind.Z3_OP_BSDIV, "/" },
        { Z3_decl_kind.Z3_OP_BSMOD, "%" },
        { Z3_decl_kind.Z3_OP_BSREM, "SRem" },
        { Z3_decl_kind.Z3_OP_BUREM, "URem" },

        { Z3_decl_kind.Z3_OP_EXT_ROTATE_LEFT, "RotateLeft" },
        { Z3_decl_kind.Z3_OP_EXT_ROTATE_RIGHT, "RotateRight" },

        { Z3_decl_kind.Z3_OP_SLEQ, "<=" },
        { Z3_decl_kind.Z3_OP_SLT, "<" },
        { Z3_decl_kind.Z3_OP_SGEQ, ">=" },
        { Z3_decl_kind.Z3_OP_SGT, ">" },

        { Z3_decl_kind.Z3_OP_ULEQ, "ULE" },
        { Z3_decl_kind.Z3_OP_ULT, "ULT" },
        { Z3_decl_kind.Z3_OP_UGEQ, "UGE" },
        { Z3_decl_kind.Z3_OP_UGT, "UGT" },
        { Z3_decl_kind.Z3_OP_SIGN_EXT, "SignExt" },
        { Z3_decl_kind.Z3_OP_ZERO_EXT, "ZeroExt" },

        { Z3_decl_kind.Z3_OP_REPEAT, "RepeatBitVec" },
        { Z3_decl_kind.Z3_OP_BASHR, ">>" },
        { Z3_decl_kind.Z3_OP_BSHL, "<<" },
        { Z3_decl_kind.Z3_OP_BLSHR, "LShR" },

        { Z3_decl_kind.Z3_OP_CONCAT, "Concat" },
        { Z3_decl_kind.Z3_OP_EXTRACT, "Extract" },
        { Z3_decl_kind.Z3_OP_BV2INT, "BV2Int" },
        { Z3_decl_kind.Z3_OP_ARRAY_MAP, "Map" },
        { Z3_decl_kind.Z3_OP_SELECT, "Select" },
        { Z3_decl_kind.Z3_OP_STORE, "Store" },
        { Z3_decl_kind.Z3_OP_CONST_ARRAY, "K" },
        { Z3_decl_kind.Z3_OP_ARRAY_EXT, "Ext" },

        { Z3_decl_kind.Z3_OP_PB_AT_MOST, "AtMost" },
        { Z3_decl_kind.Z3_OP_PB_LE, "PbLe" },
        { Z3_decl_kind.Z3_OP_PB_GE, "PbGe" },
        { Z3_decl_kind.Z3_OP_PB_EQ, "PbEq" },

        { Z3_decl_kind.Z3_OP_SEQ_CONCAT, "Concat" },
        { Z3_decl_kind.Z3_OP_SEQ_PREFIX, "PrefixOf" },
        { Z3_decl_kind.Z3_OP_SEQ_SUFFIX, "SuffixOf" },
        { Z3_decl_kind.Z3_OP_SEQ_UNIT, "Unit" },
        { Z3_decl_kind.Z3_OP_SEQ_CONTAINS, "Contains" },
        { Z3_decl_kind.Z3_OP_SEQ_REPLACE, "Replace" },
        { Z3_decl_kind.Z3_OP_SEQ_AT, "At" },
        { Z3_decl_kind.Z3_OP_SEQ_NTH, "Nth" },
        { Z3_decl_kind.Z3_OP_SEQ_INDEX, "IndexOf" },
        { Z3_decl_kind.Z3_OP_SEQ_LAST_INDEX, "LastIndexOf" },
        { Z3_decl_kind.Z3_OP_SEQ_LENGTH, "Length" },
        { Z3_decl_kind.Z3_OP_STR_TO_INT, "StrToInt" },
        { Z3_decl_kind.Z3_OP_INT_TO_STR, "IntToStr" },

        { Z3_decl_kind.Z3_OP_SEQ_IN_RE, "InRe" },
        { Z3_decl_kind.Z3_OP_SEQ_TO_RE, "Re" },
        { Z3_decl_kind.Z3_OP_RE_PLUS, "Plus" },
        { Z3_decl_kind.Z3_OP_RE_STAR, "Star" },
        { Z3_decl_kind.Z3_OP_RE_OPTION, "Option" },
        { Z3_decl_kind.Z3_OP_RE_UNION, "Union" },
        { Z3_decl_kind.Z3_OP_RE_RANGE, "Range" },
        { Z3_decl_kind.Z3_OP_RE_INTERSECT, "Intersect" },
        { Z3_decl_kind.Z3_OP_RE_COMPLEMENT, "Complement" },

        { Z3_decl_kind.Z3_OP_FPA_IS_NAN, "fpIsNaN" },
        { Z3_decl_kind.Z3_OP_FPA_IS_INF, "fpIsInf" },
        { Z3_decl_kind.Z3_OP_FPA_IS_ZERO, "fpIsZero" },
        { Z3_decl_kind.Z3_OP_FPA_IS_NORMAL, "fpIsNormal" },
        { Z3_decl_kind.Z3_OP_FPA_IS_SUBNORMAL, "fpIsSubnormal" },
        { Z3_decl_kind.Z3_OP_FPA_IS_NEGATIVE, "fpIsNegative" },
        { Z3_decl_kind.Z3_OP_FPA_IS_POSITIVE, "fpIsPositive" },
        { Z3_decl_kind.Z3_OP_LE, "<=" },
        { Z3_decl_kind.Z3_OP_LT, "<" },
        { Z3_decl_kind.Z3_OP_GE, ">=" },
        { Z3_decl_kind.Z3_OP_ADD, "+" },
        { Z3_decl_kind.Z3_OP_SUB, "-" },
        { Z3_decl_kind.Z3_OP_GT, ">" },
        { Z3_decl_kind.Z3_OP_MUL, "*" },
        { Z3_decl_kind.Z3_OP_DIV, "/" },
        { Z3_decl_kind.Z3_OP_FPA_RM_NEAREST_TIES_TO_EVEN, "RoundNearestTiesToEven()" },
        { Z3_decl_kind.Z3_OP_FPA_RM_NEAREST_TIES_TO_AWAY, "RoundNearestTiesToAway()" },
        { Z3_decl_kind.Z3_OP_FPA_RM_TOWARD_POSITIVE, "RoundTowardPositive()" },
        { Z3_decl_kind.Z3_OP_FPA_RM_TOWARD_NEGATIVE, "RoundTowardNegative()" },
        { Z3_decl_kind.Z3_OP_FPA_RM_TOWARD_ZERO, "RoundTowardZero()" },
        { Z3_decl_kind.Z3_OP_FPA_PLUS_INF, "fpPlusInfinity" },
        { Z3_decl_kind.Z3_OP_FPA_MINUS_INF, "fpMinusInfinity" },
        { Z3_decl_kind.Z3_OP_FPA_NAN, "fpNaN" },
        { Z3_decl_kind.Z3_OP_FPA_PLUS_ZERO, "fpPZero" },
        { Z3_decl_kind.Z3_OP_FPA_MINUS_ZERO, "fpNZero" },
        { Z3_decl_kind.Z3_OP_FPA_ADD, "fpAdd" },
        { Z3_decl_kind.Z3_OP_FPA_SUB, "fpSub" },
        { Z3_decl_kind.Z3_OP_FPA_NEG, "fpNeg" },
        { Z3_decl_kind.Z3_OP_FPA_MUL, "fpMul" },
        { Z3_decl_kind.Z3_OP_FPA_DIV, "fpDiv" },
        { Z3_decl_kind.Z3_OP_FPA_REM, "fpRem" },
        { Z3_decl_kind.Z3_OP_FPA_ABS, "fpAbs" },
        { Z3_decl_kind.Z3_OP_FPA_MIN, "fpMin" },
        { Z3_decl_kind.Z3_OP_FPA_MAX, "fpMax" },
        { Z3_decl_kind.Z3_OP_FPA_FMA, "fpFMA" },
        { Z3_decl_kind.Z3_OP_FPA_SQRT, "fpSqrt" },
        { Z3_decl_kind.Z3_OP_FPA_ROUND_TO_INTEGRAL, "fpRoundToIntegral" },

        { Z3_decl_kind.Z3_OP_FPA_EQ, "fpEQ" },
        { Z3_decl_kind.Z3_OP_FPA_LT, "fpLT" },
        { Z3_decl_kind.Z3_OP_FPA_GT, "fpGT" },
        { Z3_decl_kind.Z3_OP_FPA_LE, "fpLEQ" },
        { Z3_decl_kind.Z3_OP_FPA_GE, "fpGEQ" },

        { Z3_decl_kind.Z3_OP_FPA_FP, "fpFP" },
        { Z3_decl_kind.Z3_OP_FPA_TO_FP, "fpToFP" },
        { Z3_decl_kind.Z3_OP_FPA_TO_FP_UNSIGNED, "fpToFPUnsigned" },
        { Z3_decl_kind.Z3_OP_FPA_TO_UBV, "fpToUBV" },
        { Z3_decl_kind.Z3_OP_FPA_TO_SBV, "fpToSBV" },
        { Z3_decl_kind.Z3_OP_FPA_TO_REAL, "fpToReal" },
        { Z3_decl_kind.Z3_OP_FPA_TO_IEEE_BV, "fpToIEEEBV" }
     };

     public static List<Z3_decl_kind> z3Infix = new List<Z3_decl_kind>()
     {
         { Z3_decl_kind.Z3_OP_DT_CONSTRUCTOR },
         { Z3_decl_kind.Z3_OP_DT_IS },
         { Z3_decl_kind.Z3_OP_DT_ACCESSOR },
         { Z3_decl_kind.Z3_OP_DT_CONSTRUCTOR },
         { Z3_decl_kind.Z3_OP_DT_CONSTRUCTOR }
     };

     public static string ConvertZ3ExprPrefixToInfix(Expr expr)
     {
         Contract.Assert(expr != null);
         string convStr = "";

         convStr += "(";
             
         for (int i = 0; i < expr.Args.Length; ++i)
         {
             if (i > 0 ||
                 expr.NumArgs == 1)
             {
                 if(opToStrMap.ContainsKey(expr.FuncDecl.DeclKind))
                 {
                     convStr += " " + opToStrMap[expr.FuncDecl.DeclKind] + " ";
                 }
                 else if (expr.FuncDecl.DeclKind == Z3_decl_kind.Z3_OP_DT_CONSTRUCTOR)
                 {
                     convStr += " " + expr.FuncDecl.Name + " ";
                 }
             }

             var arg = expr.Args[i];
             if (arg.Args.Length > 0)
             {
                 convStr += ConvertZ3ExprPrefixToInfix(arg);
             }
             else
             {
                 convStr += arg.ToString();
             }
         }

         convStr += ")";
         
         return convStr;
     }
}