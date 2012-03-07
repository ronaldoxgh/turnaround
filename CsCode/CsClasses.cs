using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsCode
{
    public abstract class CsNamed
    {
        public string Name;
    }

    public class CsNamespace : CsNamed
    {
        public List<string> Usings = new List<string>();
        public List<CsNamed> Decls = new List<CsNamed>();
    }

    public enum CsNamespaceVisibility { Default, Private, Public, Internal }

    public class CsType : CsNamed
    {
        public CsNamespaceVisibility Visibility = CsNamespaceVisibility.Public;
    }

    public class CsDelegateType : CsNamed
    {
        ///
    }

    public class CsClassType : CsType
    {
        public bool IsStatic = false;
        public string AncestorName;
        public List<string> Interfaces = new List<string>();
        public List<CsNamed> Decls = new List<CsNamed>();
    }

    public class CsStructType : CsType
    {
        public List<CsNamed> Decls = new List<CsNamed>();
    }

    public class CsAliasType : CsType
    {
        public string RefTypeName;
    }

    public enum CsClassVisibility { Private, Protected, Public, Default, Internal }

    public enum CsParamBind { Copy, Ref }

    public class CsParam : CsNamed
    {
        public CsType Type;
        public CsParamBind Bind = CsParamBind.Copy;
    }

    public abstract class CsClassMember : CsNamed
    {
        public CsClassVisibility Visibility = CsClassVisibility.Public;
    }

    public class CsMethod : CsClassMember
    {
        public bool IsStatic = false;
        public bool IsOverride = false;
        public bool IsAbstract = false;
        public CsType Type;
        public List<CsParam> Params = new List<CsParam>();
        public List<CsStat> Codes = new List<CsStat>();
    }

    public class CsField : CsClassMember
    {
        public bool IsConst = false;
        public bool IsStatic = false;
        public CsType Type;
        public CsValue InitialValue;
    }

    public class CsLocalVar : CsStat
    {
        public string Name;
        public bool IsConst = false;
        public CsType Type;
        public CsValue InitialValue;
    }

    public class CsProperty : CsNamed
    {
        public CsType Type;
        public List<CsParam> IndexParams = new List<CsParam>();
        public bool IsDefault;
        public bool IsStatic = false;
        public CsClassVisibility Visibility = CsClassVisibility.Default;
        public CsValue ReaderValue;
        public CsValue WriterValue;
    }

    public class CsEnumConst : CsNamed { }

    public class CsEnumType : CsType
    {
        public List<CsEnumConst> Consts = new List<CsEnumConst>();
    }

    public abstract class CsStat
    {
    }

    public class CsBegin : CsStat
    {
        public List<CsStat> Codes = new List<CsStat>();
    }

    public class CsIf : CsStat
    {
        public CsValue Condition;
        public List<CsStat> TrueCodes = new List<CsStat>();
        public List<CsStat> FalseCodes = new List<CsStat>();
    }

    public class CsWhile : CsStat
    {
        public CsValue Condition;
        public List<CsStat> Codes = new List<CsStat>();
    }

    public enum CsForStep { UpTo, DownTo }
    public class CsFor : CsStat
    {
        public string IteratorName;
        public CsValue ValueFrom;
        public CsValue ValueTo;
        public CsForStep Step = CsForStep.UpTo;
        public List<CsStat> Codes = new List<CsStat>();
    }

    public class CsRepeat : CsStat
    {
        public List<CsStat> Codes = new List<CsStat>();
        public CsValue ExitCondition;
    }

    public class CsSwitchCase
    {
        public List<CsValue> Values = new List<CsValue>();
        public List<CsStat> Codes = new List<CsStat>();
    }

    public class CsSwitch : CsStat
    {
        public CsValue SubjectValue;
        public List<CsSwitchCase> Cases = new List<CsSwitchCase>();
        public List<CsStat> DefaultCodes = new List<CsStat>();
    }

    public enum CsSymbol { None, Nil, True, False, Null }
    public enum CsValueKind { Parenthesis, Brackets, IntLiteral, StrLiteral, FloatLiteral, Name, Operation, CallParams, SpecialSymbol, DataAt }
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

    public class CsAssignment : CsStat
    {
        public CsValue LeftSide;
        public CsValue RightSide;
    }

    public class CsThrow : CsStat
    {
        public CsValue ExceptionValue;
    }

    public class CsTry : CsStat
    {
        public List<CsStat> Codes = new List<CsStat>();
        public List<CsStat> FinallyCodes = new List<CsStat>();
        public bool HasExceptionHandler;
        public List<CsTryCatch> Exceptions = new List<CsTryCatch>();
        public bool HasElseExceptionCodes;
        public bool HasUntypedExceptionCodes;
        public List<CsStat> UntypedExceptionCodes = new List<CsStat>();
        public List<CsStat> ElseExceptionCodes = new List<CsStat>();
    }

    public class CsTryCatch
    {
        public string Name;
        public CsType Type;
        public List<CsStat> Codes = new List<CsStat>();
    }

    public class CsValueCall : CsStat
    {
        public CsValue CallingValue;
    }

    public class CsBaseCall : CsStat
    {
        public CsValue InheritedValue;
    }
}
