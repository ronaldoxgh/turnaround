using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PasCode
{
    public interface ISolvable
    {
        void Solve();
        PasNamed Find(string name);
    }

    public abstract class PasNamed : ISolvable
    {
        public string Name;
        public virtual void Solve() { }
        public virtual PasNamed Find(string name) { return null; }

        internal static void SolveObj(ISolvable obj)
        {
            if (obj != null)
                obj.Solve();
        }

        internal static void SolveList(System.Collections.IList list)
        {
            if (list != null)
                foreach (var named in list)
                    (named as ISolvable).Solve();
        }

        internal static List<PasNamed> Context = new List<PasNamed>();
        internal static PasNamed Current(Type type)
        {
            for (var i = Context.Count - 1; i >= 0; i--)
                if (Context[i].GetType() == type)
                    return Context[i];
            return null;
        }

        internal static PasNamed FindInList(System.Collections.IList list, string name)
        {
            foreach (var item in list)
            {
                var n = item as PasNamed;
                if (n.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return n;
            }
            return null;
        }

        internal static PasNamed SolveName(string name)
        {
            var last = Context.Last();
            if (last != null)
                return last.Find(name);
            return null;
        }
    }

    public abstract class PasType : PasNamed { }

    public class PasEnumConst : PasNamed { }

    public class PasEnumType : PasType
    {
        public List<PasEnumConst> Consts = new List<PasEnumConst>();
        public override void Solve()
        {
            base.Solve();
            SolveList(Consts);
        }
    }

    public class PasTypeRef : PasNamed
    {
        public PasType TypeDecl;
        public override void Solve()
        {
            base.Solve();
            TypeDecl = SolveName(this.Name) as PasType;
        }
    }

    public class PasArrayType : PasType
    {
        public PasTypeRef ItemType;
        public PasValue Interval;
        public override void Solve()
        {
            base.Solve();
            SolveObj(ItemType);
            SolveObj(Interval);
        }
    }

    public class PasSetType : PasType
    {
        public PasTypeRef ItemType;
        public override void Solve()
        {
            base.Solve();
            SolveObj(ItemType);
        }
    }

    public class PasRecordType : PasType
    {
        public List<PasNamed> Vars = new List<PasNamed>();
        public override void Solve()
        {
            base.Solve();
            SolveList(Vars);
        }
    }

    public class PasPointerType : PasType
    {
        public PasTypeRef ReferenceType;
        public override void Solve()
        {
            base.Solve();
            SolveObj(ReferenceType);
        }
    }

    public class PasStringType : PasType
    {
        public int MaxLen;
    }

    public class PasAliasType : PasType
    {
        public string RefTypeName;
        public override void Solve()
        {
            base.Solve();
        }
    }

    public class PasProcedureType : PasType
    {
        public PasProcedureApproach Approach;
        public PasTypeRef ReturnType;
        public List<PasParam> Params = new List<PasParam>();
        public bool IsStdCall;
        public override void Solve()
        {
            base.Solve();
            SolveObj(ReturnType);
            SolveList(Params);
        }
    }

    public enum PasClassVisibility { Default, Private, Protected, Public, Published }
    public class PasClassType : PasType
    {
        public string AncestorName;
        public List<string> Interfaces = new List<string>();
        public List<PasNamed> Decls = new List<PasNamed>();

        public override void Solve()
        {
            base.Solve();
            SolveList(Decls);
        }

        public override PasNamed Find(string name)
        {
            return base.Find(name) ?? PasNamed.FindInList(Decls, name);
        }
    }

    public class PasProperty : PasNamed
    {
        public PasTypeRef DataType;
        public List<PasParam> IndexParams = new List<PasParam>();
        public bool IsDefault;
        public PasClassVisibility Visibility = PasClassVisibility.Default;
        public PasValue ReaderValue;
        public PasValue WriterValue;
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveList(IndexParams);
            PasNamed.SolveObj(ReaderValue);
            PasNamed.SolveObj(WriterValue);
        }
    }

    public class PasVarDecl : PasNamed
    {
        public PasTypeRef DataType;
        public PasValue InitialValue;
        public PasClassVisibility Visibility = PasClassVisibility.Default;
        public override void Solve()
        {
            base.Solve();
            SolveObj(DataType);
            SolveObj(InitialValue);
        }
    }

    public class PasConstDecl : PasNamed
    {
        public PasTypeRef DataType;
        public PasValue Value;
        public override void Solve()
        {
            base.Solve();
            SolveObj(DataType);
            SolveObj(Value);
        }
    }

    public enum PasParamBinding { Const, Var, Copy, Out, In }
    public class PasParam : PasNamed
    {
        public PasParamBinding Binding;
        public PasTypeRef DataType;
        public PasValue DefaultValue;
        public override void Solve()
        {
            base.Solve();
            SolveObj(DataType);
            SolveObj(DefaultValue);
        }
    }

    public enum PasProcedureApproach { Procedure, Function, Constructor, Destructor }
    public class PasProcedureDecl : PasNamed
    {
        public PasProcedureApproach Approach;
        public PasTypeRef ReturnType;
        public List<PasParam> Params = new List<PasParam>();
        public bool IsVirtual;
        public bool IsAbstract;
        public bool IsOverride;
        public bool IsDynamic;
        public bool IsStdCall;
        public bool IsOverload;
        public bool IsReintroduce;
        public bool IsForward;
        public bool IsExternal;
        public bool IsStatic;
        public string ExternalLib;
        public PasClassVisibility Visibility = PasClassVisibility.Default;
        public List<PasNamed> Decls = new List<PasNamed>();
        public List<PasStat> Codes = new List<PasStat>();
        public string InClassName;
        public override void Solve()
        {
            base.Solve();
            SolveObj(ReturnType);
            SolveList(Params);
            SolveList(Decls);
            SolveList(Codes);
        }
    }

    public class PasUnit : PasNamed
    {
        public List<string> InterfaceUses = new List<string>();
        public List<PasNamed> InterfaceDecls = new List<PasNamed>();
        public List<string> ImplementationUses = new List<string>();
        public List<PasNamed> ImplementationDecls = new List<PasNamed>();
        public List<PasStat> InitializationCodes = new List<PasStat>();
        public List<PasStat> FinalizationCodes = new List<PasStat>();
        public override void Solve()
        {
            base.Solve();
            SolveList(InterfaceDecls);
            SolveList(ImplementationDecls);
            SolveList(InitializationCodes);
            SolveList(FinalizationCodes);
        }
    }

    public abstract class PasStat : ISolvable
    {
        public virtual void Solve() { }
        public virtual PasNamed Find(string name) { return null; }
    }

    public class PasBegin : PasStat
    {
        public List<PasStat> Codes = new List<PasStat>();
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveList(Codes);
        }
    }

    public class PasIf : PasStat
    {
        public PasValue Condition;
        public List<PasStat> TrueCodes = new List<PasStat>();
        public List<PasStat> FalseCodes = new List<PasStat>();
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveObj(Condition);
            PasNamed.SolveList(TrueCodes);
            PasNamed.SolveList(FalseCodes);
        }
    }

    public class PasWhile : PasStat
    {
        public PasValue Condition;
        public List<PasStat> Codes = new List<PasStat>();
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveObj(Condition);
            PasNamed.SolveList(Codes);
        }
    }

    public enum PasForStep { UpTo, DownTo }
    public class PasFor : PasStat
    {
        public string IteratorName;
        public PasValue ValueFrom;
        public PasValue ValueTo;
        public PasForStep Step = PasForStep.UpTo;
        public List<PasStat> Codes = new List<PasStat>();
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveObj(ValueFrom);
            PasNamed.SolveObj(ValueTo);
            PasNamed.SolveList(Codes);
        }
    }

    public class PasRepeat : PasStat
    {
        public List<PasStat> Codes = new List<PasStat>();
        public PasValue ExitCondition;
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveList(Codes);
            PasNamed.SolveObj(ExitCondition);
        }
    }

    public class PasCaseItem : PasStat
    {
        public List<PasValue> Values = new List<PasValue>();
        public List<PasStat> Codes = new List<PasStat>();
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveList(Values);
            PasNamed.SolveList(Codes);
        }
    }

    public class PasCase : PasStat
    {
        public PasValue SubjectValue;
        public List<PasCaseItem> Items = new List<PasCaseItem>();
        public List<PasStat> DefaultCodes = new List<PasStat>();
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveObj(SubjectValue);
            PasNamed.SolveList(Items);
            PasNamed.SolveList(DefaultCodes);
        }
    }

    public class PasAssignment : PasStat
    {
        public PasValue LeftSide;
        public PasValue RightSide;
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveObj(LeftSide);
            PasNamed.SolveObj(RightSide);
        }
    }

    public class PasExcept : PasStat
    {
        public string Name;
        public PasTypeRef ExceptionType;
        public List<PasStat> Codes = new List<PasStat>();
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveObj(ExceptionType);
            PasNamed.SolveList(Codes);
        }
    }

    public class PasTry : PasStat
    {
        public List<PasStat> Codes = new List<PasStat>();
        public List<PasStat> FinallyCodes = new List<PasStat>();
        public bool HasExceptionHandler;
        public List<PasExcept> Exceptions = new List<PasExcept>();
        public bool HasElseExceptionCodes;
        public bool HasUntypedExceptionCodes;
        public List<PasStat> UntypedExceptionCodes = new List<PasStat>();
        public List<PasStat> ElseExceptionCodes = new List<PasStat>();
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveList(Codes);
            PasNamed.SolveList(FinallyCodes);
            PasNamed.SolveList(Exceptions);
            PasNamed.SolveList(UntypedExceptionCodes);
            PasNamed.SolveList(ElseExceptionCodes);
        }
    }

    public class PasWith : PasStat
    {
        public PasValue SubjectValue;
        public List<PasStat> Codes = new List<PasStat>();
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveObj(SubjectValue);
            PasNamed.SolveList(Codes);
        }
    }

    public class PasRaise : PasStat
    {
        public PasValue ExceptionValue;
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveObj(ExceptionValue);
        }
    }

    public class PasCall : PasStat
    {
        public PasValue CallingValue;
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveObj(CallingValue);
        }
    }

    public class PasInherited : PasStat
    {
        public PasValue InheritedValue;
        public override void Solve()
        {
            base.Solve();
            PasNamed.SolveObj(InheritedValue);
        }
    }

    public enum PasSymbol { None, Nil, True, False, Null }
    public enum PasValueKind { Parenthesis, Brackets, IntLiteral, StrLiteral, FloatLiteral, Name, Operation, Params, Symbol, DataAt, Interval }
    public enum PasValueOperator
    {
        None,
        Positive,
        Negative,
        NotMask,
        AndMask,
        OrMask,
        XorMask,
        Concat,
        Sum,
        Subtract,
        Multiply,
        Divide,
        IntDiv,
        Remainder,
        ShiftLeft,
        ShiftRight,
        Equal,
        Inequal,
        Less,
        Greater,
        NonLess,
        NonGreater,
        Not,
        And,
        Or,
        Xor,
        Interval,
        Union,
        Intersection,
        Diference,
        Belongs,
        CastAs,
        InstanceOf,
        AddressOf
    }

    public class PasValue : ISolvable
    {
        public PasValueKind Kind;
        public PasValueOperator Operator;
        public List<PasValue> Args = new List<PasValue>();
        public string StrData;
        public PasNamed NameDecl;
        public Int64 IntData;
        public float FloatData;
        public PasSymbol SymbolData;
        public PasValue Prior, Next;

        public static int Precedence(PasValueOperator op)
        {
            switch (op)
            {
                case PasValueOperator.CastAs:
                    return 0;
                case PasValueOperator.InstanceOf:
                    return 1;
                case PasValueOperator.AddressOf:
                    return 2;
                case PasValueOperator.Positive:
                    return 2;
                case PasValueOperator.Negative:
                    return 3;
                case PasValueOperator.NotMask:
                    return 4;
                case PasValueOperator.Not:
                    return 5;
                case PasValueOperator.Multiply:
                    return 6;
                case PasValueOperator.Divide:
                    return 7;
                case PasValueOperator.IntDiv:
                    return 8;
                case PasValueOperator.Remainder:
                    return 9;
                case PasValueOperator.ShiftLeft:
                    return 10;
                case PasValueOperator.ShiftRight:
                    return 11;
                case PasValueOperator.AndMask:
                    return 12;
                case PasValueOperator.OrMask:
                    return 13;
                case PasValueOperator.XorMask:
                    return 14;
                case PasValueOperator.Sum:
                    return 15;
                case PasValueOperator.Concat:
                    return 16;
                case PasValueOperator.Subtract:
                    return 17;
                case PasValueOperator.Interval:
                    return 18;
                case PasValueOperator.Union:
                    return 19;
                case PasValueOperator.Intersection:
                    return 20;
                case PasValueOperator.Diference:
                    return 21;
                case PasValueOperator.Belongs:
                    return 22;
                case PasValueOperator.Equal:
                    return 23;
                case PasValueOperator.Inequal:
                    return 24;
                case PasValueOperator.Less:
                    return 25;
                case PasValueOperator.Greater:
                    return 26;
                case PasValueOperator.NonLess:
                    return 27;
                case PasValueOperator.NonGreater:
                    return 28;
                case PasValueOperator.And:
                    return 29;
                case PasValueOperator.Or:
                    return 30;
                case PasValueOperator.Xor:
                    return 31;
                default:
                    return int.MaxValue;
            }
        }

        public void Solve()
        {
            PasNamed.SolveObj(Prior);
            if (Kind == PasValueKind.Name)
                NameDecl = PasNamed.SolveName(StrData);
            PasNamed.SolveList(Args);
        }

        public PasNamed Find(string name)
        {
            return null;///
        }
    }

}
