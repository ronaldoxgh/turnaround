using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace CsCode
{
    public abstract class CsDecl
    {
        public string Name;
    }

    public class CsRef
    {
        public CsDecl Decl;
        public string Name { get { return Decl != null ? Decl.Name : null; } }
    }

    public class CsNamespace: CsDecl
    {
        public List<CsUsing> Usings = new List<CsUsing>();
        public List<CsDecl> Decls = new List<CsDecl>();
    }

    public class CsUsing
    {
        public string Namespace;
    }

    public enum CsNamespaceVisibility { Default, Private, Public, Internal }

    public class CsType: CsDecl
    {
        public CsNamespaceVisibility Visibility = CsNamespaceVisibility.Public;
    }

    public class CsDelegateDomain: CsType
    {
        public CsRef ReturnType;
        public List<CsParamDecl> Params = new List<CsParamDecl>();
        public List<CsStat> Codes = new List<CsStat>();
    }

    public class CsClassTypeDecl : CsType
    {
        public bool IsStatic = false;
        public CsRef AncestorRef;
        public List<CsRef> Interfaces = new List<CsRef>();
        public List<CsDecl> Decls = new List<CsDecl>();
    }

    public class CsStructTypeDecl: CsType
    {
        public List<CsDecl> Decls = new List<CsDecl>();
    }

    public class CsAliasTypeDecl: CsType
    {
        public string TargetTypeName;
    }

    public enum CsClassVisibility { Private, Protected, Public, Default, Internal }

    public enum CsParamBind { Copy, Ref }

    public class CsParamDecl: CsDecl
    {
        public CsRef TypeRef;
        public CsParamBind Bind = CsParamBind.Copy;
    }

    public abstract class CsClassMember: CsDecl
    {
        public CsClassVisibility Visibility = CsClassVisibility.Public;
    }

    public class CsMethodDecl: CsClassMember
    {
        public bool IsStatic = false;
        public bool IsOverride = false;
        public bool IsAbstract = false;
        public bool IsVirtual = false;
        public CsRef ReturnType;
        public List<CsParamDecl> Params = new List<CsParamDecl>();
        public List<CsStat> Codes = new List<CsStat>();
        public bool IsConstructor;
        public bool IsDestructor;
        public CsBase BaseCall;
    }

    public class CsField: CsClassMember
    {
        public bool IsConst = false;
        public bool IsStatic = false;
        public CsRef TypeRef;
        public CsValue InitialValue;
    }

    public class CsLocalVarDecl: CsStat
    {
        public string Name;
        public bool IsConst = false;
        public CsRef TypeRef;
        public CsValue InitialValue;
    }

    public class CsProperty: CsDecl
    {
        public CsRef TypeRef;
        public List<CsParamDecl> IndexParams = new List<CsParamDecl>();
        public bool IsDefault;
        public bool IsStatic = false;
        public CsClassVisibility Visibility = CsClassVisibility.Default;
        public CsValue ReaderValue;
        public CsValue WriterValue;
    }

    public class CsEnumConst: CsDecl
    {
        public CsEnumTypeDecl EnumDomain;
    }

    public class CsEnumTypeDecl: CsType
    {
        public List<CsEnumConst> Consts = new List<CsEnumConst>();
    }

    public abstract class CsStat
    {
    }

    public class CsBegin: CsStat
    {
        public List<CsStat> Codes = new List<CsStat>();
    }

    public class CsIf: CsStat
    {
        public CsValue Condition;
        public List<CsStat> TrueCodes = new List<CsStat>();
        public List<CsStat> FalseCodes = new List<CsStat>();
    }

    public class CsWhile: CsStat
    {
        public CsValue Condition;
        public List<CsStat> Codes = new List<CsStat>();
    }

    public enum CsForStep { UpTo, DownTo }
    public class CsFor: CsStat
    {
        public string IteratorName;
        public CsValue ValueFrom;
        public CsValue ValueTo;
        public CsForStep Step = CsForStep.UpTo;
        public List<CsStat> Codes = new List<CsStat>();
    }

    public class CsRepeat: CsStat
    {
        public List<CsStat> Codes = new List<CsStat>();
        public CsValue ExitCondition;
    }

    public class CsSwitchCase
    {
        public List<CsValue> Values = new List<CsValue>();
        public List<CsStat> Codes = new List<CsStat>();
    }

    public class CsSwitch: CsStat
    {
        public CsValue SubjectValue;
        public List<CsSwitchCase> Cases = new List<CsSwitchCase>();
        public List<CsStat> DefaultCodes = new List<CsStat>();
    }

    public enum CsSymbol { None, Nil, True, False, Null }
    public enum CsValueKind { Parenthesis, Brackets, IntLiteral, StrLiteral, FloatLiteral, Name, Operation, CallParams, SpecialSymbol, DataAt, HexLiteral }
    public enum CsValueOperator
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

    public class CsValue
    {
        public CsValueKind Kind;
        public CsValueOperator Operator;
        public List<CsValue> Args = new List<CsValue>();
        public string StrData;
        public Int64 IntData;
        public float FloatData;
        public CsSymbol SymbolData;
        public CsValue Prior, Next;
        public CsRef TypeRef;
        public CsRef NameRef;

        public static int Precedence(CsValueOperator op)
        {
            switch (op)
            {
                case CsValueOperator.CastAs:
                    return 0;
                case CsValueOperator.InstanceOf:
                    return 1;
                case CsValueOperator.AddressOf:
                    return 2;
                case CsValueOperator.Positive:
                    return 2;
                case CsValueOperator.Negative:
                    return 3;
                case CsValueOperator.NotMask:
                    return 4;
                case CsValueOperator.Not:
                    return 5;
                case CsValueOperator.Multiply:
                    return 6;
                case CsValueOperator.Divide:
                    return 7;
                case CsValueOperator.IntDiv:
                    return 8;
                case CsValueOperator.Remainder:
                    return 9;
                case CsValueOperator.ShiftLeft:
                    return 10;
                case CsValueOperator.ShiftRight:
                    return 11;
                case CsValueOperator.AndMask:
                    return 12;
                case CsValueOperator.OrMask:
                    return 13;
                case CsValueOperator.XorMask:
                    return 14;
                case CsValueOperator.Sum:
                    return 15;
                case CsValueOperator.Concat:
                    return 16;
                case CsValueOperator.Subtract:
                    return 17;
                case CsValueOperator.Interval:
                    return 18;
                case CsValueOperator.Union:
                    return 19;
                case CsValueOperator.Intersection:
                    return 20;
                case CsValueOperator.Diference:
                    return 21;
                case CsValueOperator.Belongs:
                    return 22;
                case CsValueOperator.Equal:
                    return 23;
                case CsValueOperator.Inequal:
                    return 24;
                case CsValueOperator.Less:
                    return 25;
                case CsValueOperator.Greater:
                    return 26;
                case CsValueOperator.NonLess:
                    return 27;
                case CsValueOperator.NonGreater:
                    return 28;
                case CsValueOperator.And:
                    return 29;
                case CsValueOperator.Or:
                    return 30;
                case CsValueOperator.Xor:
                    return 31;
                default:
                    return int.MaxValue;
            }
        }
    }

    public class CsAssignment: CsStat
    {
        public CsValue LeftSide;
        public CsValue RightSide;
    }

    public class CsThrow: CsStat
    {
        public CsValue ExceptionValue;
    }

    public class CsTry: CsStat
    {
        public List<CsStat> Codes = new List<CsStat>();
        public List<CsStat> FinallyCodes = new List<CsStat>();
        public bool HasExceptionHandler;
        public List<CsCatch> Exceptions = new List<CsCatch>();
        public bool HasElseExceptionCodes;
        public bool HasUntypedExceptionCodes;
        public List<CsStat> UntypedExceptionCodes = new List<CsStat>();
        public List<CsStat> ElseExceptionCodes = new List<CsStat>();
    }

    public class CsCatch: CsStat
    {
        public string Name;
        public CsRef ExceptionDomain;
        public List<CsStat> Codes = new List<CsStat>();
    }

    public class CsCall: CsStat
    {
        public CsValue Value;
    }

    public class CsBase: CsStat
    {
        public CsValue Value;
    }
}
