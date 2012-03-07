using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace PasCode
{
    public interface ISolvable
    {
        void Solve();
        PasDecl DotFind(string name);
    }

    public static class Context
    {
        public static void Solve(ISolvable solvable)
        {
            if (solvable != null)
                solvable.Solve();
        }

        public static void Solve(IList list)
        {
            if (list != null)
                foreach (ISolvable solvable in list)
                    solvable.Solve();
        }

        public static List<PasDecl> Rooms = new List<PasDecl>();

        public static PasDecl Current(Type type)
        {
            for (var i = Context.Rooms.Count - 1; i >= 0; i--)
                if (Context.Rooms[i].GetType() == type)
                    return Context.Rooms[i];
            return null;
        }

        public static void Enter(PasDecl room) { Rooms.Add(room); }

        public static void Leave() { Rooms.Remove(Rooms.Last()); }

        public static PasDecl Find(System.Collections.IList list, string name)
        {
            foreach (PasDecl item in list)
                if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return item;
                else if (item is PasEnumTypeDecl)
                {
                    var temp = (item as PasEnumTypeDecl).DotFind(name);
                    if (temp != null)
                        return temp;
                }
            return null;
        }

        public static PasDecl Find(string name)
        {
            for (var i = Context.Rooms.Count - 1; i >= 0; i--)
            {
                var room = Context.Rooms[i];
                var found = room.DotFind(name) ?? room.FindContext(name);
                if (found != null)
                    return found;
            }
            return null;
        }

        public static PasTypeDecl NeedArrayDomain(List<PasValue> Args)
        {
            ///
            return null;
        }

        public static PasTypeDecl NeedIntDomain(long IntData)
        {
            ///TODO
            return null;
        }

        public static PasTypeDecl NeedStringDomain()
        {
            ///TODO
            return null;
        }

        public static PasTypeDecl NeedStringDomain(string stringData)
        {
            ///TODO
            return null;
        }

        public static PasTypeDecl NeedFloatDomain(float floatData)
        {
            ///TODO
            return null;
        }

        public static PasTypeDecl NeedSignedDomain(PasTypeDecl pasDomain)
        {
            ///TODO
            return null;
        }

        public static PasTypeDecl NeedCommonDomain(List<PasValue> args)
        {
            ///TODO
            return null;
        }

        public static PasTypeDecl NeedCommonDomainUp(List<PasValue> args)
        {
            var common = NeedCommonDomain(args);
            if (common != null)
                return common.CoerceUp();
            return null;
        }

        public static PasTypeDecl NeedFloatDomain()
        {
            ///TODO
            return null;
        }

        public static PasTypeDecl NeedIntDomain()
        {
            ///TODO
            return null;
        }

        public static PasRef RefOf(PasDecl decl)
        {
            return new PasRef { Decl = decl, Name = decl != null ? decl.Name : null };
        }

        public static PasRef RefOf(PasRef declRef)
        {
            return new PasRef
            {
                Decl = declRef != null ? declRef.Decl : null,
                Name = declRef != null ? declRef.Name : null
            };
        }

    }

    public abstract class PasDecl : ISolvable
    {
        public string Name;
        public virtual void Solve() { }
        public virtual PasDecl DotFind(string name) { return null; }
        public virtual PasDecl FindContext(string name) { return null; }
        public virtual PasTypeDecl GetDomain() { return null; }
    }

    public abstract class PasTypeDecl : PasDecl
    {
        public override PasTypeDecl GetDomain() { return this; }

        internal PasTypeDecl CoerceUp()
        {
            ///TODO
            return null;
        }
    }

    public class PasEnumConst : PasDecl
    {
        public PasEnumTypeDecl EnumDomain;

        public override PasTypeDecl GetDomain()
        {
            return EnumDomain;
        }
    }

    public class PasEnumTypeDecl : PasTypeDecl
    {
        public List<PasEnumConst> Consts = new List<PasEnumConst>();

        public override PasDecl DotFind(string name)
        {
            return Context.Find(Consts, name);
        }

        public override void Solve()
        {
            base.Solve();
            Context.Solve(Consts);
            foreach (var enumConst in Consts)
                enumConst.EnumDomain = this;
        }
    }

    public class PasRef : ISolvable
    {
        public string Name;
        public PasDecl Decl;

        public void Solve()
        {
            if (string.IsNullOrEmpty(Name) && Decl != null)
                Name = Decl.Name;
            else if (Decl == null && !string.IsNullOrEmpty(Name))
                Decl = Context.Find(Name);
        }

        public PasDecl DotFind(string name)
        {
            return Decl != null ? Decl.DotFind(name) : null;
        }
    }

    public class PasArrayTypeDecl : PasTypeDecl
    {
        public PasRef ItemType;
        public PasValue Interval;
        public bool Packed;
        public override void Solve()
        {
            base.Solve();
            Context.Solve(ItemType);
            Context.Solve(Interval);
        }
    }

    public class PasSetTypeDecl : PasTypeDecl
    {
        public PasRef ItemTypeRef;
        public override void Solve()
        {
            base.Solve();
            Context.Solve(ItemTypeRef);
        }
    }

    public class PasRecordTypeDecl : PasTypeDecl
    {
        public List<PasDecl> Vars = new List<PasDecl>();
        public bool Packed;
        public override void Solve()
        {
            base.Solve();
            Context.Solve(Vars);
        }
    }

    public class PasPointerTypeDecl : PasTypeDecl
    {
        public PasRef DataDomain;
        public override void Solve()
        {
            base.Solve();
            Context.Solve(DataDomain);
        }
    }

    public class PasStringTypeDecl : PasTypeDecl
    {
        public int MaxLen;
    }

    public class PasAliasTypeDecl : PasTypeDecl
    {
        public PasRef TargetType;
        public override void Solve()
        {
            base.Solve();
            Context.Solve(TargetType);
        }
    }

    public class PasProcedureTypeDecl : PasTypeDecl
    {
        public PasProcedureApproach Approach;
        public PasRef ReturnDomain;
        public List<PasParamDecl> Params = new List<PasParamDecl>();
        public bool IsStdCall;
        public bool IsOverload;
        public override void Solve()
        {
            base.Solve();
            Context.Solve(Params);
            Context.Solve(ReturnDomain);
        }
    }

    public enum PasVisibility { Default, Private, Protected, Public, Published }
    public class PasClassTypeDecl : PasTypeDecl
    {
        public PasRef Ancestor;
        public List<PasRef> Interfaces = new List<PasRef>();
        public List<PasDecl> Decls = new List<PasDecl>();
        public bool IsForwarded;

        public override void Solve()
        {
            base.Solve();
            Context.Solve(Ancestor);
            Context.Solve(Interfaces);

            Context.Enter(this);
            try
            {
                Context.Solve(Decls);
            }
            finally
            {
                Context.Leave();
            }
        }

        public override PasDecl DotFind(string name)
        {
            return base.DotFind(name) ?? Context.Find(Decls, name);
        }
    }

    public class PasMetaclassTypeDecl : PasTypeDecl
    {
        public PasRef ClassDomain;
        public override void Solve()
        {
            base.Solve();
            Context.Solve(ClassDomain);
        }
    }

    public class PasProperty : PasDecl
    {
        public PasRef TypeRef;
        public List<PasParamDecl> IndexParams = new List<PasParamDecl>();
        public bool IsDefault;
        public PasVisibility Visibility = PasVisibility.Default;
        public PasValue ReaderValue;
        public PasValue WriterValue;
        public PasValue DefaultValue;
        public PasValue StoredCond;
        public override void Solve()
        {
            base.Solve();
            Context.Solve(TypeRef);
            Context.Solve(IndexParams);
            Context.Solve(ReaderValue);
            Context.Solve(WriterValue);
            Context.Solve(DefaultValue);
            Context.Solve(StoredCond);
        }
    }

    public class PasVarDecl : PasDecl
    {
        public PasRef TypeRef;
        public PasValue InitialValue;
        public PasVisibility Visibility = PasVisibility.Default;
        public override void Solve()
        {
            base.Solve();
            Context.Solve(TypeRef);
            Context.Solve(InitialValue);
        }
    }

    public class PasConstDecl : PasDecl
    {
        public PasRef TypeRef;
        public PasValue Value;
        public PasVisibility Visibility = PasVisibility.Default;
        public override void Solve()
        {
            base.Solve();
            Context.Solve(TypeRef);
            Context.Solve(Value);
        }
    }

    public enum PasParamBinding { Const, Var, Copy, Out, In }
    public class PasParamDecl : PasDecl
    {
        public PasParamBinding Binding;
        public PasRef TypeRef;
        public PasValue DefaultValue;
        public override void Solve()
        {
            base.Solve();
            Context.Solve(TypeRef);
            Context.Solve(DefaultValue);
        }
    }

    public enum PasProcedureApproach { Procedure, Function, Constructor, Destructor }
    public class PasProcedureDecl : PasDecl
    {
        public PasProcedureApproach Approach;
        public PasRef ReturnType;
        public List<PasParamDecl> Params = new List<PasParamDecl>();
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
        public PasVisibility Visibility = PasVisibility.Default;
        public List<PasDecl> Decls = new List<PasDecl>();
        public List<PasStat> Codes = new List<PasStat>();
        public string InClassName;///
        public override void Solve()
        {
            base.Solve();
            Context.Solve(Params);
            Context.Solve(ReturnType);

            Context.Enter(this);
            try
            {
                Context.Solve(Decls);
                Context.Solve(Codes);
            }
            finally
            {
                Context.Leave();
            }
        }
    }

    public class PasUse : ISolvable
    {
        public string UnitName;
        public PasUnit UnitRef;

        public void Solve()
        {
            UnitRef = Context.Find(UnitName) as PasUnit;
        }

        public PasDecl DotFind(string name)
        {
            return null;
        }
    }

    public class PasUnit : PasDecl
    {
        public List<PasUse> InterfaceUses = new List<PasUse>();
        public List<PasDecl> InterfaceDecls = new List<PasDecl>();
        public List<PasUse> ImplementationUses = new List<PasUse>();
        public List<PasDecl> ImplementationDecls = new List<PasDecl>();
        public List<PasStat> InitializationCodes = new List<PasStat>();
        public List<PasStat> FinalizationCodes = new List<PasStat>();

        private PasDecl FindUses(List<PasUse> uses, string name)
        {
            foreach (var use in uses)
                if (use.UnitRef != null)
                {
                    var found = use.UnitRef.DotFind(name);
                    if (found != null)
                        return found;
                }
            return null;
        }

        public override PasDecl DotFind(string name)
        {
            return Context.Find(InterfaceDecls, name) ?? Context.Find(ImplementationDecls, name);
        }

        public override PasDecl FindContext(string name)
        {
            return base.FindContext(name) ?? FindUses(InterfaceUses, name) ?? FindUses(ImplementationUses, name);
        }

        public override void Solve()
        {
            base.Solve();
            Context.Enter(this);
            try
            {
                Context.Solve(InterfaceUses);
                Context.Solve(InterfaceDecls);
                Context.Solve(ImplementationUses);
                Context.Solve(ImplementationDecls);
                Context.Solve(InitializationCodes);
                Context.Solve(FinalizationCodes);
            }
            finally
            {
                Context.Leave();
            }
        }
    }

    public abstract class PasStat : ISolvable
    {
        public virtual void Solve() { }
        public virtual PasDecl DotFind(string name) { return null; }
    }

    public class PasBegin : PasStat
    {
        public List<PasStat> Codes = new List<PasStat>();
        public override void Solve()
        {
            base.Solve();
            Context.Solve(Codes);
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
            Context.Solve(Condition);
            Context.Solve(TrueCodes);
            Context.Solve(FalseCodes);
        }
    }

    public class PasWhile : PasStat
    {
        public PasValue Condition;
        public List<PasStat> Codes = new List<PasStat>();

        public override void Solve()
        {
            base.Solve();
            Context.Solve(Condition);
            Context.Solve(Codes);
        }
    }

    public enum PasForStep { UpTo, DownTo }
    public class PasFor : PasStat
    {
        public PasValue Iterator;
        public PasValue ValueFrom;
        public PasValue ValueTo;
        public PasForStep Step = PasForStep.UpTo;
        public List<PasStat> Codes = new List<PasStat>();

        public override void Solve()
        {
            base.Solve();
            Context.Solve(Iterator);
            Context.Solve(ValueFrom);
            Context.Solve(ValueTo);
            Context.Solve(Codes);
        }
    }

    public class PasRepeat : PasStat
    {
        public List<PasStat> Codes = new List<PasStat>();
        public PasValue ExitCondition;

        public override void Solve()
        {
            base.Solve();
            Context.Solve(Codes);
            Context.Solve(ExitCondition);
        }
    }

    public class PasCaseItem : PasStat
    {
        public List<PasValue> Values = new List<PasValue>();
        public List<PasStat> Codes = new List<PasStat>();

        public override void Solve()
        {
            base.Solve();
            Context.Solve(Values);
            Context.Solve(Codes);
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
            Context.Solve(SubjectValue);
            Context.Solve(Items);
            Context.Solve(DefaultCodes);
        }
    }

    public class PasAssignment : PasStat
    {
        public PasValue LeftSide;
        public PasValue RightSide;

        public override void Solve()
        {
            base.Solve();
            Context.Solve(LeftSide);
            Context.Solve(RightSide);
        }
    }

    public class PasExcept : PasStat
    {
        public string VarName;
        public PasRef ExceptionDomain;
        public List<PasStat> Codes = new List<PasStat>();

        public override void Solve()
        {
            base.Solve();
            Context.Solve(ExceptionDomain);
            Context.Solve(Codes);
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
            Context.Solve(Codes);
            Context.Solve(FinallyCodes);
            Context.Solve(Exceptions);
            Context.Solve(UntypedExceptionCodes);
            Context.Solve(ElseExceptionCodes);
        }
    }

    public class PasWith : PasStat
    {
        public PasValue SubjectValue;
        public List<PasStat> Codes = new List<PasStat>();

        public override void Solve()
        {
            base.Solve();
            Context.Solve(SubjectValue);

            ///Context.Enter(this);
            try
            {
                Context.Solve(Codes);
            }
            finally
            {
                ///Context.Leave();
            }
        }
    }

    public class PasRaise : PasStat
    {
        public PasValue ExceptionValue;

        public override void Solve()
        {
            base.Solve();
            Context.Solve(ExceptionValue);
        }
    }

    public class PasCall : PasStat
    {
        public PasValue Value;

        public override void Solve()
        {
            base.Solve();
            Context.Solve(Value);
        }
    }

    public class PasInherited : PasStat
    {
        public PasValue Value;

        public override void Solve()
        {
            base.Solve();
            Context.Solve(Value);
        }
    }

    public enum PasSymbol { None, Nil, True, False, Null }
    public enum PasValueKind
    {
        Parenthesis, Brackets, IntLiteral, StrLiteral, FloatLiteral, Name,
        Operation, Params, Symbol, DataAt, Interval, HexLiteral
    }
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
        public PasRef DeclRef;
        public PasRef TypeRef;
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
            Context.Solve(Args);
            if (Prior == null)
                switch (Kind)
                {
                    case PasValueKind.Parenthesis:
                        SolveParenthesis();
                        break;
                    case PasValueKind.Brackets:
                        SolveBrackets();
                        break;
                    case PasValueKind.IntLiteral:
                    case PasValueKind.HexLiteral:
                        SolveIntLiteral();
                        break;
                    case PasValueKind.StrLiteral:
                        SolveStrLiteral();
                        break;
                    case PasValueKind.FloatLiteral:
                        SolveFloatLiteral();
                        break;
                    case PasValueKind.Name:
                        SolveName();
                        break;
                    case PasValueKind.Operation:
                        SolveOperation();
                        break;
                    case PasValueKind.Params:
                        SolveParams();
                        break;
                    case PasValueKind.Symbol:
                        SolveSymbol();
                        break;
                    case PasValueKind.DataAt:
                        SolveDataAt();
                        break;
                    case PasValueKind.Interval:
                        SolveInterval();
                        break;
                    default:
                        throw new Exception("unsolvable value kind " + Kind.ToString());
                }
            else
            {
                Context.Solve(Prior);
                switch (Kind)
                {
                    case PasValueKind.Name:
                        {
                            if (Prior.TypeRef != null)
                            {
                                var found = Prior.TypeRef.DotFind(StrData);
                                if (found != null)
                                    TypeRef = Context.RefOf(found.GetDomain());
                            }
                            break;
                        }
                    ///default: throw new Exception("invalid member kind " + Kind.ToString());
                }
            }
        }

        public PasDecl DotFind(string name)
        {
            return TypeRef.DotFind(name);
        }

        private void SolveParenthesis()
        {
            if (Args.Count == 1)
                TypeRef = Context.RefOf(Args[0].TypeRef);
            else
                throw new Exception("invalid parenthesis expression");
        }

        private void SolveBrackets()
        {
            TypeRef = Context.RefOf(Context.NeedArrayDomain(Args));
        }

        private void SolveIntLiteral()
        {
            TypeRef = Context.RefOf(Context.NeedIntDomain(IntData));
        }

        private void SolveStrLiteral()
        {
            TypeRef = Context.RefOf(Context.NeedStringDomain(StrData));
        }

        private void SolveFloatLiteral()
        {
            TypeRef = Context.RefOf(Context.NeedFloatDomain(FloatData));
        }

        private void SolveName()
        {
            DeclRef = Context.RefOf(Context.Find(StrData));
            if (DeclRef != null && DeclRef.Decl != null)
                TypeRef = Context.RefOf(DeclRef.Decl.GetDomain());
        }

        private void SolveOperation()
        {
            switch (Operator)
            {
                case PasValueOperator.None:
                    break;
                case PasValueOperator.Positive:
                    TypeRef = Context.RefOf(Context.NeedSignedDomain(Args[0].TypeRef.Decl as PasTypeDecl));
                    break;
                case PasValueOperator.Negative:
                    TypeRef = Args[0].TypeRef != null ? Context.RefOf(Context.NeedSignedDomain(Args[0].TypeRef.Decl as PasTypeDecl)) : null;
                    break;
                case PasValueOperator.NotMask:
                    TypeRef = Context.RefOf(Args[0].TypeRef);
                    break;
                case PasValueOperator.AndMask:
                    TypeRef = Context.RefOf(Context.NeedCommonDomain(Args));
                    break;
                case PasValueOperator.OrMask:
                    TypeRef = Context.RefOf(Context.NeedCommonDomain(Args));
                    break;
                case PasValueOperator.XorMask:
                    TypeRef = Context.RefOf(Context.NeedCommonDomain(Args));
                    break;
                case PasValueOperator.Concat:
                    TypeRef = Context.RefOf(Context.NeedStringDomain());
                    break;
                case PasValueOperator.Sum:
                    TypeRef = Context.RefOf(Context.NeedCommonDomainUp(Args));
                    break;
                case PasValueOperator.Subtract:
                    TypeRef = Context.RefOf(Context.NeedCommonDomainUp(Args));
                    break;
                case PasValueOperator.Multiply:
                    TypeRef = Context.RefOf(Context.NeedCommonDomainUp(Args));
                    break;
                case PasValueOperator.Divide:
                    TypeRef = Context.RefOf(Context.NeedFloatDomain());
                    break;
                case PasValueOperator.IntDiv:
                    TypeRef = Context.RefOf(Context.NeedIntDomain());
                    break;
                case PasValueOperator.Remainder:
                    //DomainRef = NeedCommonDomain(Args);
                    break;
                case PasValueOperator.ShiftLeft:
                    break;
                case PasValueOperator.ShiftRight:
                    break;
                case PasValueOperator.Equal:
                    break;
                case PasValueOperator.Inequal:
                    break;
                case PasValueOperator.Less:
                    break;
                case PasValueOperator.Greater:
                    break;
                case PasValueOperator.NonLess:
                    break;
                case PasValueOperator.NonGreater:
                    break;
                case PasValueOperator.Not:
                    break;
                case PasValueOperator.And:
                    break;
                case PasValueOperator.Or:
                    break;
                case PasValueOperator.Xor:
                    break;
                case PasValueOperator.Interval:
                    break;
                case PasValueOperator.Union:
                    break;
                case PasValueOperator.Intersection:
                    break;
                case PasValueOperator.Diference:
                    break;
                case PasValueOperator.Belongs:
                    break;
                case PasValueOperator.CastAs:
                    break;
                case PasValueOperator.InstanceOf:
                    break;
                case PasValueOperator.AddressOf:
                    break;
                default:
                    throw new Exception("invalid operator " + Operator.ToString());
            }
        }

        private void SolveParams() { }

        private void SolveSymbol() { }

        private void SolveDataAt() { }

        private void SolveInterval() { }
    }

}
