using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Utils;

namespace PasCode
{
    class PasStringParser : StringParser
    {
        public PasStringParser(string sourceCode)
            : base(sourceCode)
        {
        }

        bool SkipOneLineComment()
        {
            var priorPos = Pos;
            if (ThisChar("/"))
                if (ThisChar("/"))
                {
                    char foo = '\x00';
                    while (!EndOfLine())
                        AnyChar(ref foo);
                    return true;
                }
            Pos = priorPos;
            return false;
        }

        bool SkipSimpleBlockComment()
        {
            var priorPos = Pos;
            if (ThisChar("{"))
            {
                char charRead = '\x00';
                while (AnyChar(ref charRead))
                    if (charRead == '}')
                        return true;
                throw new Exception("Missing '}'");
            }
            return false;
        }

        bool SkipSuperBlockComment()
        {
            var priorPos = Pos;
            if (ThisChar("("))
                if (ThisChar("*"))
                {
                    char charRead = '\x00';
                    while (AnyChar(ref charRead))
                        if (charRead == '*')
                            if (ThisChar(")"))
                                break;
                    return true;
                }
            Pos = priorPos;
            return false;
        }

        public override bool Comments()
        {
            return SkipOneLineComment() || SkipSimpleBlockComment() || SkipSuperBlockComment();
        }
    }

    public partial class PasReader
    {
        public PasUnit ReadUnitFile(string fileName, string defines)
        {
            return ReadUnitCode(File.ReadAllText(fileName), defines);
        }

        public PasUnit ReadUnitCode(string unitText, string defines)
        {
            return HasUnit(SolvePreCompiler(unitText, defines));
        }

        void Require(bool truth, string msg)
        {
            if (!truth)
                throw new Exception(msg + " at " + parser.Pos.ToString() + "\n\n" + parser.CurrentLine);
        }

        PasStringParser parser;

        bool HasText(string wantedText) { return parser.ThisText(wantedText); }
        void ReqText(string wantedText) { Require(HasText(wantedText), string.Format("expecting '{0}'", wantedText)); }
        bool HasSemi() { return HasText(";"); }
        void ReqSemi() { ReqText(";"); }
        bool HasDots() { return HasText(":"); }
        void ReqDots() { ReqText(":"); }
        bool HasComma() { return HasText(","); }
        bool HasChar(char wantedChar) { return parser.ThisChar(wantedChar); }
        bool HasChar(string wantedCharSet) { return parser.ThisChar(wantedCharSet); }
        bool HasChar(string wantedCharSet, ref char charRead) { return parser.ThisChar(wantedCharSet, ref charRead); }

        string ReqWord()
        {
            string name = null;
            Require(HasWord(ref name), "expecting a word");
            return name;
        }

        bool HasWord(string wantedWord) { return parser.ThisWord(wantedWord); }
        bool HasWord(ref string wordRead) { return parser.AnyWord(ref wordRead); }
        void ReqWord(string wantedWord) { Require(HasWord(wantedWord), string.Format("expecting word '{0}'", wantedWord)); }

        PasValue ReqValue()
        {
            PasValue value = null;
            Require(HasValue(ref value), "expecting a value");
            return value;
        }

        bool HasPar() { return HasText("("); }
        bool HasEndPar() { return HasText(")"); }
        void ReqEndPar() { ReqText(")"); }
        bool HasBra() { return HasText("["); }
        bool HasEndBra() { return HasText("]"); }
        void ReqEndBra() { ReqText("]"); }

        static string[] ReservedWords = new[] { "and", "array", "as", "asm", 
            "begin", "case", "class", "const", "constructor", "destructor", "dispinterface", "div",
            "do", "downto", "else", "end", "except", "exports", "file", "finalization",
            "finally", "for", "function", "goto", "if", "implementation", "in", "inherited",
            "initialization", "inline", "interface", "is", "label", "library", "mod", "nil",
            "not", "object", "of", "or", "out", "packed", "procedure", "program",
            "property", "raise", "record", "repeat", "resourcestring", "set", "shl", "shr",
            "string", "then", "threadvar", "to", "try", "type", "unit", "until",
            "uses", "var", "while", "with", "xor"};

        bool IsReserved(string word)
        {
            return ReservedWords.Any(rw => rw.Equals(word, StringComparison.OrdinalIgnoreCase));
        }

        bool HasIdent(ref string identRead)
        {
            var priorPos = parser.Pos;
            if (HasWord(ref identRead))
            {
                if (!IsReserved(identRead))
                    return true;
                parser.Pos = priorPos;
            }
            return false;
        }

        bool HasIdent()
        {
            string foo = null;
            return HasIdent(ref foo);
        }

        bool HasIdent(ref PasValue valueRead)
        {
            string identRead = null;
            if (HasIdent(ref identRead))
            {
                valueRead = new PasValue { Kind = PasValueKind.Name, StrData = identRead };
                return true;
            }
            return false;
        }

        string ReqIdent()
        {
            string identRead = null;
            Require(HasIdent(ref identRead), "expecting an identifier");
            return identRead;
        }

        PasUnit HasUnit(string code)
        {
            parser = new PasStringParser(code);
            if (HasWord("unit"))
            {
                var unit = new PasUnit();
                unit.Name = ReqIdent();
                ReqSemi();
                ReqWord("interface");
                HasUses(unit.InterfaceUses);
                HasDecls(unit.InterfaceDecls, false, PasVisibility.Public);
                ReqWord("implementation");
                HasUses(unit.ImplementationUses);
                HasDecls(unit.ImplementationDecls, true, PasVisibility.Private);
                if (HasWord("begin"))
                    HasStats(unit.InitializationCodes);
                else
                {
                    if (HasWord("initialization"))
                        HasStats(unit.InitializationCodes);
                    if (HasWord("finalization"))
                        HasStats(unit.FinalizationCodes);
                }
                ReqWord("end");
                ReqText(".");
                return unit;
            }
            return null;
        }

        bool HasUses(List<PasUse> unitNames)
        {
            if (HasWord("uses"))
            {
                do
                {
                    var use = ReqIdent();
                    unitNames.Add(new PasUse { UnitName = use });
                } while (HasComma());
                ReqSemi();
                return true;
            }
            return false;
        }

        bool HasDecls(List<PasDecl> decls, bool allowImpl)
        {
            return HasDecls(decls, allowImpl, PasVisibility.Default);
        }

        bool HasDecls(List<PasDecl> decls, bool allowImpl, PasVisibility visibility)
        {
            int loops = 0;
            while (HasTypeSection(decls) || HasVarSection(decls, visibility) || HasConstSection(decls, visibility) ||
                HasProcedure(decls, allowImpl, visibility))
            {
                HasSemi();
                ++loops;
            }

            // resolvo forwards
            for (var i = decls.Count - 1; i >= 0; i--)
                if (decls[i] is PasClassTypeDecl)
                {
                    var frwd = decls[i] as PasClassTypeDecl;
                    if (frwd.IsForwarded)
                    {
                        var impl = decls.FirstOrDefault(d => d is PasClassTypeDecl && !(d as PasClassTypeDecl).IsForwarded && d != frwd && d.Name == frwd.Name);
                        if (impl != null)
                            decls.Remove(frwd);
                    }
                }

            return loops > 0;
        }

        bool HasTypeSection(List<PasDecl> decls)
        {
            if (HasWord("type"))
            {
                PasTypeDecl decl = null;
                Require(HasNameAndType(ref decl), "expecting a type decl");
                decls.Add(decl);
                while (HasSemi())
                    if (HasNameAndType(ref decl))
                        decls.Add(decl);
                    else
                        break;
                return true;
            }
            return false;
        }

        bool HasVarSection(List<PasDecl> decls, PasVisibility visibility)
        {
            if (HasWord("var"))
            {
                Require(HasVarsAndType(decls, visibility), "expecting a var decl");
                while (HasSemi())
                    if (!HasVarsAndType(decls, visibility))
                        break;
                return true;
            }
            return false;
        }

        bool HasConstSection(List<PasDecl> decls, PasVisibility visibility)
        {
            if (HasWord("const"))
            {
                Require(HasConstAndValue(decls, visibility), "expecting a const decl");
                while (HasSemi())
                    if (!HasConstAndValue(decls, visibility))
                        break;
                return true;
            }
            return false;
        }

        bool HasNameAndType(ref PasTypeDecl typeRead)
        {
            var priorPos = parser.Pos;
            string nameRead = null;
            if (HasIdent(ref nameRead))
            {
                if (HasText("="))
                {
                    typeRead = ReqType();
                    typeRead.Name = nameRead;
                    return true;
                }
                parser.Pos = priorPos;
            }
            return false;
        }

        bool HasType(ref PasTypeDecl typeRead)
        {
            return HasEnumType(ref typeRead) || HasMetaClassType(ref typeRead) || HasClassType(ref typeRead) ||
                HasRecordType(ref typeRead) || HasArrayType(ref typeRead) || HasPointerType(ref typeRead) ||
                HasSetType(ref typeRead) || HasProcedureType(ref typeRead) || HasStringType(ref typeRead) ||
                HasAliasType(ref typeRead);
        }

        PasTypeDecl ReqType()
        {
            PasTypeDecl typeRead = null;
            Require(HasType(ref typeRead), "expecting type");
            return typeRead;
        }

        PasRef ReqTypeRef()
        {
            return new PasRef { Name = null, Decl = ReqType() };
        }

        bool HasEnumType(ref PasTypeDecl typeRead)
        {
            if (HasPar())
            {
                var enumBody = new PasEnumTypeDecl();
                do
                {
                    var constName = ReqIdent();
                    enumBody.Consts.Add(new PasEnumConst { Name = constName });
                } while (HasComma());
                ReqEndPar();
                typeRead = enumBody;
                return true;
            }
            return false;
        }

        bool HasMetaClassType(ref PasTypeDecl typeRead)
        {
            var priorPos = parser.Pos;
            if (HasWord("class"))
            {
                if (HasWord("of"))
                {
                    var classBody = new PasMetaclassTypeDecl();
                    classBody.ClassDomain = ReqTypeRef();
                    typeRead = classBody;
                    return true;
                }
                parser.Pos = priorPos;
            }
            return false;
        }

        bool HasClassType(ref PasTypeDecl typeRead)
        {
            if (HasWord("class"))
            {
                var classBody = new PasClassTypeDecl();
                var priorPos = parser.Pos;
                if (HasSemi())
                {
                    classBody.IsForwarded = true;
                    parser.Pos = priorPos;
                }
                else if (HasWord("of"))
                {
                    parser.Pos = priorPos;
                    return false;
                }
                else
                {
                    if (HasPar())
                    {
                        string ancestorName = null;
                        if (HasIdent(ref ancestorName))
                        {
                            classBody.Ancestor = new PasRef { Name = ancestorName };
                            while (HasComma())
                                classBody.Interfaces.Add(new PasRef { Name = ReqIdent() });
                        }
                        ReqEndPar();
                    }

                    var visibility = PasVisibility.Default;
                    while (true)
                    {
                        if (!HasVisibility(ref visibility))
                            if (HasVarsAndType(classBody.Decls, visibility) ||
                                HasProcedure(classBody.Decls, false, visibility) ||
                                HasProperty(classBody.Decls, visibility))
                            {
                                if (!HasSemi())
                                    break;
                            }
                            else
                                break;
                    }
                    ReqWord("end");
                }
                typeRead = classBody;
                return true;
            }
            return false;
        }

        bool HasVisibility(ref PasVisibility visibilityRead)
        {
            int wordIndex = -1;
            if (parser.ThisWord(new[] { "private", "protected", "public", "published" }, ref wordIndex))
            {
                switch (wordIndex)
                {
                    case 0:
                        visibilityRead = PasVisibility.Private;
                        break;
                    case 1:
                        visibilityRead = PasVisibility.Protected;
                        break;
                    case 2:
                        visibilityRead = PasVisibility.Public;
                        break;
                    case 3:
                        visibilityRead = PasVisibility.Published;
                        break;
                    default:
                        throw new Exception("internal error");
                }
                return true;
            }
            return false;
        }

        bool HasRecordType(ref PasTypeDecl typeRead)
        {
            var oldpos = parser.Pos;
            var ispacked = HasWord("packed");
            if (HasWord("record"))
            {
                var recordBody = new PasRecordTypeDecl();
                recordBody.Packed = ispacked;
                Require(HasVarsAndType(recordBody.Vars, PasVisibility.Default), "expecting record vars");
                while (HasSemi())
                    if (!HasVarsAndType(recordBody.Vars, PasVisibility.Default))
                        break;
                ReqWord("end");
                typeRead = recordBody;
                return true;
            }
            parser.Pos = oldpos;
            return false;
        }

        bool HasInterval(ref PasValue intervalRead)
        {
            var priorPos = parser.Pos;
            PasValue first = null;
            PasValue last = null;
            if (HasSimple(ref first))
            {
                if (HasText(".."))
                    if (HasValue(ref last))
                    {
                        intervalRead = new PasValue { Kind = PasValueKind.Interval };
                        intervalRead.Args.Add(first);
                        intervalRead.Args.Add(last);
                        return true;
                    }
                parser.Pos = priorPos;
            }
            return false;
        }

        bool HasArrayType(ref PasTypeDecl typeRead)
        {
            var oldpos = parser.Pos;
            var ispacked = HasWord("packed");
            if (HasWord("array"))
            {
                PasValue interval = null;
                if (HasBra())
                {
                    if (!HasInterval(ref interval))
                        Require(HasSimple(ref interval), "expecting an interval");
                    ReqEndBra();
                }
                ReqWord("of");
                var arrayType = new PasArrayTypeDecl { Interval = interval };
                arrayType.ItemType = ReqTypeRef();
                arrayType.Packed = ispacked;
                typeRead = arrayType;
                return true;
            }
            parser.Pos = oldpos;
            return false;
        }

        bool HasPointerType(ref PasTypeDecl typeRead)
        {
            if (HasText("^"))
            {
                var pointerBody = new PasPointerTypeDecl();
                pointerBody.DataDomain = ReqTypeRef();
                typeRead = pointerBody;
                return true;
            }
            return false;
        }

        bool HasSetType(ref PasTypeDecl typeRead)
        {
            if (HasWord("set"))
            {
                ReqWord("of");
                var setBody = new PasSetTypeDecl();
                setBody.ItemTypeRef = ReqTypeRef();
                typeRead = setBody;
                return true;
            }
            return false;
        }

        bool HasProcedureApproach(ref PasProcedureApproach approachRead)
        {
            int procType = -1;
            if (parser.ThisWord(new[] { "procedure", "function", "constructor", "destructor" }, ref procType))
            {
                switch (procType)
                {
                    case 0:
                        approachRead = PasProcedureApproach.Procedure;
                        break;
                    case 1:
                        approachRead = PasProcedureApproach.Function;
                        break;
                    case 2:
                        approachRead = PasProcedureApproach.Constructor;
                        break;
                    case 3:
                        approachRead = PasProcedureApproach.Destructor;
                        break;
                    default:
                        throw new Exception("internal error");
                }
                return true;
            }
            return false;
        }

        bool HasProcedureTypeModifiers(PasProcedureTypeDecl procedureBody)
        {
            var loops = 0;
            while (HasProcedureTypeModifierFlag(procedureBody))
                ++loops;
            return loops > 0;
        }

        bool HasProcedureTypeModifierFlag(PasProcedureTypeDecl procedureBody)
        {
            var priorPos = parser.Pos;
            if (HasText(";"))
            {
                int readIndex = -1;
                if (parser.ThisWord(new[] { "stdcall", "overload" }, ref readIndex))
                    switch (readIndex)
                    {
                        case 0:
                            procedureBody.IsStdCall = true;
                            return true;
                        case 1:
                            procedureBody.IsOverload = true;
                            return true;
                        default:
                            throw new Exception("internal error");
                    }
                parser.Pos = priorPos;
            }
            return false;
        }

        bool HasProcedureType(ref PasTypeDecl typeRead)
        {
            var approach = PasProcedureApproach.Procedure;
            if (HasProcedureApproach(ref approach))
            {
                var procedureType = new PasProcedureTypeDecl { Approach = approach };
                if (HasPar())
                {
                    HasParams(procedureType.Params);
                    ReqEndPar();
                }
                if (procedureType.Approach == PasProcedureApproach.Function)
                {
                    ReqDots();
                    procedureType.ReturnDomain = ReqTypeRef();
                }
                HasProcedureTypeModifiers(procedureType);
                typeRead = procedureType;
                return true;
            }
            return false;
        }

        bool HasHex(ref string hexRead)
        {
            if (HasChar('$'))
            {
                Require(parser.ThisStr(StringParser.HexCharSet, ref hexRead), "expecting hex");
                return true;
            }
            return false;
        }

        bool HasDigits(ref string digitsRead)
        {
            return parser.ThisStr(StringParser.DigitCharSet, ref digitsRead);
        }

        bool HasHexOrDec(out long intRead, out char intFormat)
        {
            string digits = null;
            intRead = 0;
            intFormat = '\0';
            if (HasHex(ref digits))
            {
                intRead = long.Parse(digits, System.Globalization.NumberStyles.HexNumber);
                intFormat = 'H';
            }
            else if (HasDigits(ref digits))
            {
                intRead = long.Parse(digits);
                intFormat = 'I';
            }
            else
                return false;
            return true;
        }

        bool HasStringType(ref PasTypeDecl typeRead)
        {
            if (HasWord("string") || HasWord("ansistring") || HasWord("widestring"))
            {
                var stringType = new PasStringTypeDecl { MaxLen = 0 };
                if (HasBra())
                {
                    long maxLen;
                    char foo;
                    Require(HasHexOrDec(out maxLen, out foo), "expecting maxlen");
                    ReqEndBra();
                    stringType.MaxLen = (int)maxLen;
                }
                typeRead = stringType;
                return true;
            }
            return false;
        }

        bool HasAliasType(ref PasTypeDecl typeRead)
        {
            string nameRead = null;
            if (!HasIdent(ref nameRead))
                return false;
            typeRead = new PasAliasTypeDecl { TargetType = new PasRef { Name = nameRead } };
            return true;
        }

        bool HasVarsAndType(List<PasDecl> decls, PasVisibility visibility)
        {
            var priorPos = parser.Pos;
            string varName = null;
            if (HasIdent(ref varName))
            {
                var varNames = new List<string>();
                varNames.Add(varName);
                while (HasComma())
                    varNames.Add(ReqIdent());
                if (HasDots())
                {
                    var typeRef = ReqTypeRef();
                    PasValue varValue = null;
                    if (HasText("="))
                        varValue = ReqValue();
                    foreach (var varName2 in varNames)
                        decls.Add(new PasVarDecl { Name = varName2, TypeRef = typeRef, Visibility = visibility, InitialValue = varValue });
                    return true;
                }
                parser.Pos = priorPos;
            }
            return false;
        }

        bool HasConstAndValue(List<PasDecl> decls, PasVisibility visibility)
        {
            string constName = null;
            if (HasIdent(ref constName))
            {
                PasRef typeRef = null;
                if (HasDots())
                    typeRef = ReqTypeRef();
                ReqText("=");
                var constDecl = new PasConstDecl { Name = constName, TypeRef = typeRef, Visibility = visibility };
                constDecl.Value = ReqValue();
                return true;
            }
            return false;
        }

        bool HasParams(List<PasParamDecl> paramList)
        {
            var count = 0;
            while (HasParam(paramList))
            {
                ++count;
                if (!HasSemi())
                    break;
            }
            return count > 0;
        }

        bool HasParamBinding(ref PasParamBinding bindingRead, PasParamBinding defaultBinding)
        {
            bindingRead = defaultBinding;
            int wordIndex = -1;
            if (parser.ThisWord(new[] { "const", "var", "in", "out" }, ref wordIndex))
            {
                switch (wordIndex)
                {
                    case 0:
                        bindingRead = PasParamBinding.Const;
                        break;
                    case 1:
                        bindingRead = PasParamBinding.Var;
                        break;
                    case 2:
                        bindingRead = PasParamBinding.In;
                        break;
                    case 3:
                        bindingRead = PasParamBinding.Out;
                        break;
                    default:
                        throw new Exception("internal error");
                }
                return true;
            }
            return false;
        }

        bool HasParam(List<PasParamDecl> paramList)
        {
            var priorPos = parser.Pos;
            var paramBinding = PasParamBinding.Copy;
            HasParamBinding(ref paramBinding, paramBinding);
            string paramName = null;
            if (HasIdent(ref paramName))
            {
                var paramNames = new List<string>();
                paramNames.Add(paramName);
                while (HasComma())
                    paramNames.Add(ReqIdent());
                if (HasDots())
                {
                    var typeRef = ReqTypeRef();
                    PasValue paramValue = null;
                    if (HasText("="))
                        paramValue = ReqValue();
                    foreach (var paramName2 in paramNames)
                        paramList.Add(new PasParamDecl { Binding = paramBinding, Name = paramName2, TypeRef = typeRef, DefaultValue = paramValue });
                }
                return true;
            }
            parser.Pos = priorPos;
            return false;
        }

        bool HasProcedure(List<PasDecl> decls, bool allowImpl, PasVisibility visibility)
        {
            var approach = PasProcedureApproach.Procedure;
            if (HasProcedureApproach(ref approach))
            {
                var procedureName = ReqIdent();
                var procedureDecl = new PasProcedureDecl { Name = procedureName, Approach = approach };
                if (HasText("."))
                {
                    procedureDecl.InClassName = procedureName;
                    procedureDecl.Name = ReqIdent();
                }
                procedureDecl.Visibility = visibility;
                if (HasPar())
                {
                    HasParams(procedureDecl.Params);
                    ReqEndPar();
                }
                if (procedureDecl.Approach == PasProcedureApproach.Function)
                {
                    ReqDots();
                    procedureDecl.ReturnType = ReqTypeRef();
                }
                HasProcedureModifiers(procedureDecl);
                if (allowImpl && !(procedureDecl.IsForward || procedureDecl.IsExternal))
                {
                    ReqSemi();
                    HasDecls(procedureDecl.Decls, true);
                    ReqWord("begin");
                    HasStats(procedureDecl.Codes);
                    ReqWord("end");
                }
                decls.Add(procedureDecl);
                return true;
            }
            return false;
        }

        bool HasProcedureModifiers(PasProcedureDecl procedureDecl)
        {
            var loops = 0;
            while (HasProcedureModifierFlag(procedureDecl))
                ++loops;
            return loops > 0;
        }

        bool HasProcedureModifierFlag(PasProcedureDecl procedure)
        {
            var priorPos = parser.Pos;
            if (HasText(";"))
            {
                int readIndex = -1;
                if (parser.ThisWord(new[] { "virtual", "abstract", "override", "dynamic", 
                   "stdcall", "overload", "reintroduce", "forward", "external" }, ref readIndex))
                    switch (readIndex)
                    {
                        case 0:
                            procedure.IsVirtual = true;
                            return true;
                        case 1:
                            procedure.IsAbstract = true;
                            return true;
                        case 2:
                            procedure.IsOverride = true;
                            return true;
                        case 3:
                            procedure.IsDynamic = true;
                            return true;
                        case 4:
                            procedure.IsStdCall = true;
                            return true;
                        case 5:
                            procedure.IsOverload = true;
                            return true;
                        case 6:
                            procedure.IsReintroduce = true;
                            return true;
                        case 7:
                            procedure.IsForward = true;
                            return true;
                        case 8:
                            procedure.IsExternal = true;
                            procedure.ExternalLib = ReqWord();
                            return true;
                        default:
                            throw new Exception("internal error");
                    }
                parser.Pos = priorPos;
            }
            return false;
        }

        bool HasWith(List<PasStat> codes)
        {
            if (HasWord("with"))
            {
                var with = new PasWith();
                codes.Add(with);
                with.SubjectValue = ReqValue();
                ReqWord("do");
                HasStat(with.Codes);
                return true;
            }
            return false;
        }

        bool HasRaise(List<PasStat> codes)
        {
            if (HasWord("raise"))
            {
                var raise = new PasRaise();
                codes.Add(raise);
                HasValue(ref raise.ExceptionValue);
                return true;
            }
            return false;
        }

        bool HasAssignment(List<PasStat> codes)
        {
            var priorPos = parser.Pos;
            PasValue left = null;
            if (HasComplex(ref left))
            {
                if (HasText(":="))
                {
                    var right = ReqValue();
                    codes.Add(new PasAssignment { LeftSide = left, RightSide = right });
                    return true;
                }
                parser.Pos = priorPos;
            }
            return false;
        }

        bool HasOnExcept(List<PasExcept> excepts)
        {
            if (HasWord("on"))
            {
                var except = new PasExcept();
                var priorPos = parser.Pos;
                var varName = ReqIdent();
                if (HasText(":"))
                    except.VarName = varName;
                else
                    parser.Pos = priorPos;
                except.ExceptionDomain = ReqTypeRef();
                excepts.Add(except);
                ReqWord("do");
                HasStat(except.Codes);
                return true;
            }
            return false;
        }

        bool HasTry(List<PasStat> codes)
        {
            if (HasWord("try"))
            {
                var pasTry = new PasTry();
                codes.Add(pasTry);
                HasStats(pasTry.Codes);
                if (HasWord("except"))
                {
                    pasTry.HasExceptionHandler = true;
                    while (HasOnExcept(pasTry.Exceptions))
                        if (!HasText(";"))
                            break;
                    if (HasWord("else"))
                    {
                        pasTry.HasElseExceptionCodes = true;
                        HasStat(pasTry.ElseExceptionCodes);
                        HasText(";");
                    }
                    else
                    {
                        pasTry.HasUntypedExceptionCodes = HasStats(pasTry.UntypedExceptionCodes);
                        HasText(";");
                    }
                }
                if (HasWord("finally"))
                    HasStats(pasTry.FinallyCodes);
                ReqWord("end");
                return true;
            }
            return false;
        }

        bool HasStat(List<PasStat> codes)
        {
            return HasBegin(codes) || HasIf(codes) || HasFor(codes) || HasWhile(codes) || HasRepeat(codes) ||
                HasCase(codes) || HasTry(codes) || HasAssignment(codes) || HasRaise(codes) || HasWith(codes) ||
                HasCall(codes) || HasInherited(codes);
        }

        bool HasStats(List<PasStat> codes)
        {
            int count = 0;
            while (HasStat(codes))
            {
                ++count;
                if (!HasSemi())
                    break;
            }
            return count > 0;
        }

        bool HasInherited(List<PasStat> codes)
        {
            if (HasWord("inherited"))
            {
                var inh = new PasInherited();
                codes.Add(inh);
                HasValue(ref inh.Value);
                return true;
            }
            return false;
        }

        bool HasCall(List<PasStat> codes)
        {
            var priorPos = parser.Pos;
            // nao pode ser uma palavra reservada como "raise" ou "end"
            string wordRead = null;
            if (HasWord(ref wordRead))
            {
                parser.Pos = priorPos;
                if (IsReserved(wordRead))
                    return false;
            }
            // qualquer valor que nao seja uma atribuicao
            PasValue value = null;
            if (HasComplex(ref value))
            {
                if (HasText(":="))
                {
                    parser.Pos = priorPos;
                    return false;
                }
                var call = new PasCall { Value = value };
                codes.Add(call);
                return true;
            }
            return false;
        }

        bool HasBegin(List<PasStat> codes)
        {
            if (HasWord("begin"))
            {
                var begin = new PasBegin();
                codes.Add(begin);
                HasStats(begin.Codes);
                ReqWord("end");
                return true;
            }
            return false;
        }

        bool HasIf(List<PasStat> codes)
        {
            if (HasWord("if"))
            {
                var ifStat = new PasIf();
                codes.Add(ifStat);
                ifStat.Condition = ReqValue();
                ReqWord("then");
                HasStat(ifStat.TrueCodes);
                if (HasWord("else"))
                    HasStat(ifStat.FalseCodes);
                return true;
            }
            return false;
        }

        bool HasWhile(List<PasStat> codes)
        {
            if (HasWord("while"))
            {
                var whileStat = new PasWhile();
                codes.Add(whileStat);
                whileStat.Condition = ReqValue();
                ReqWord("do");
                HasStat(whileStat.Codes);
                return true;
            }
            return false;
        }

        bool HasFor(List<PasStat> codes)
        {
            if (HasWord("for"))
            {
                var forStat = new PasFor();
                codes.Add(forStat);
                var iteratorName = ReqIdent();
                forStat.Iterator = new PasValue { Kind = PasValueKind.Name, StrData = iteratorName };
                ReqText(":=");
                forStat.ValueFrom = ReqValue();
                var stepIndex = 0;
                Require(parser.ThisWord(new[] { "to", "downto" }, ref stepIndex), "expecting To/DownTo");
                forStat.Step = (stepIndex == 1) ? PasForStep.DownTo : PasForStep.UpTo;
                forStat.ValueTo = ReqValue();
                ReqWord("do");
                HasStat(forStat.Codes);
                return true;
            }
            return false;
        }

        bool HasRepeat(List<PasStat> codes)
        {
            if (HasWord("repeat"))
            {
                var repeatStat = new PasRepeat();
                codes.Add(repeatStat);
                HasStats(repeatStat.Codes);
                ReqWord("until");
                repeatStat.ExitCondition = ReqValue();
                return true;
            }
            return false;
        }

        bool HasCase(List<PasStat> codes)
        {
            if (HasWord("case"))
            {
                var caseStat = new PasCase();
                codes.Add(caseStat);
                caseStat.SubjectValue = ReqValue();
                ReqWord("of");
                var itemCount = 0;
                while (HasCaseItem(caseStat.Items))
                {
                    ++itemCount;
                    if (!HasSemi())
                        break;
                }
                Require(itemCount > 0, "expecting case items");
                if (HasWord("else"))
                    HasStats(caseStat.DefaultCodes);
                ReqWord("end");
                return true;
            }
            return false;
        }

        bool HasCaseItem(List<PasCaseItem> items)
        {
            var priorPos = parser.Pos;
            PasValue value = null;
            if (HasValue(ref value))
            {
                var caseItem = new PasCaseItem();
                caseItem.Values.Add(value);
                while (HasComma())
                    caseItem.Values.Add(ReqValue());
                if (HasDots())
                {
                    HasStat(caseItem.Codes);
                    items.Add(caseItem);
                    return true;
                }
                parser.Pos = priorPos;
            }
            return false;
        }

        bool HasProperty(List<PasDecl> decls, PasVisibility visibility)
        {
            if (HasWord("property"))
            {
                var prop = new PasProperty();
                prop.Name = ReqIdent();
                prop.Visibility = visibility;
                if (HasBra())
                {
                    Require(HasParams(prop.IndexParams), "expecting index params");
                    ReqEndBra();
                }
                if (HasDots())
                    prop.TypeRef = ReqTypeRef();
                if (HasWord("read"))
                    prop.ReaderValue = ReqValue();
                if (HasWord("write"))
                    prop.WriterValue = ReqValue();
                if (HasWord("default"))
                    prop.DefaultValue = ReqValue();
                if (HasWord("stored"))
                    prop.StoredCond = ReqValue();
                var pre = parser.Pos;
                if (HasSemi())
                    if (HasWord("default"))
                        prop.IsDefault = true;
                    else
                        parser.Pos = pre;
                decls.Add(prop);
                return true;
            }
            return false;
        }

        bool HasSymbol(ref PasSymbol symbolRead)
        {
            int symbolIndex = -1;
            if (parser.ThisWord(new[] { "nil", "true", "false", "null" }, ref symbolIndex))
            {
                symbolRead = PasSymbol.None;
                switch (symbolIndex)
                {
                    case 0:
                        symbolRead = PasSymbol.Nil;
                        break;
                    case 1:
                        symbolRead = PasSymbol.True;
                        break;
                    case 2:
                        symbolRead = PasSymbol.False;
                        break;
                    case 3:
                        symbolRead = PasSymbol.Null;
                        break;
                    default:
                        throw new Exception("internal error");
                }
                return true;
            }
            return false;
        }

        bool HasSymbol(ref PasValue symbolRead)
        {
            PasSymbol symbol = PasSymbol.None;
            if (HasSymbol(ref symbol))
            {
                symbolRead = new PasValue { Kind = PasValueKind.Symbol, SymbolData = symbol };
                return true;
            }
            return false;
        }

        bool HasSimple(ref PasValue valueRead)
        {
            return HasParVal(ref valueRead) || HasBraVal(ref valueRead) || HasStrLit(ref valueRead) ||
                HasIntLit(ref valueRead) || HasFloatLit(ref valueRead) || HasSymbol(ref valueRead) ||
                HasIdent(ref valueRead);
        }

        void LinkValues(ref PasValue priorValue, PasValue nextValue)
        {
            nextValue.Prior = priorValue;
            priorValue.Next = nextValue;
            priorValue = nextValue;
        }

        bool HasDotSufix(ref PasValue priorValue)
        {
            var oldpos = parser.Pos;
            if (HasText("."))
            {
                if (HasChar('.'))
                {
                    parser.Pos = oldpos;
                    return false;
                }
                PasValue member = null;
                Require(HasIdent(ref member), "expecting dot member");
                LinkValues(ref priorValue, member);
                return true;
            }
            return false;
        }

        bool HasParSufix(ref PasValue priorValue)
        {
            if (HasPar())
            {
                PasValue callParams = new PasValue { Kind = PasValueKind.Params };
                PasValue paramValue = null;
                if (HasValue(ref paramValue))
                {
                    callParams.Args.Add(paramValue);
                    while (HasComma())
                        callParams.Args.Add(ReqValue());
                }
                ReqEndPar();
                LinkValues(ref priorValue, callParams);
                return true;
            }
            return false;
        }

        bool HasBraSufix(ref PasValue priorValue)
        {
            if (HasBra())
            {
                PasValue callParams = new PasValue { Kind = PasValueKind.Brackets };
                PasValue paramValue = null;
                if (HasValue(ref paramValue))
                {
                    callParams.Args.Add(paramValue);
                    while (HasComma())
                        callParams.Args.Add(ReqValue());
                }
                ReqEndBra();
                LinkValues(ref priorValue, callParams);
                return true;
            }
            return false;
        }

        bool HasPtrSufix(ref PasValue priorValue)
        {
            if (HasText("^"))
            {
                var ptrSufix = new PasValue { Kind = PasValueKind.DataAt };
                ptrSufix.Args.Add(priorValue);
                LinkValues(ref priorValue, ptrSufix);
                return true;
            }
            return false;
        }

        bool HasComplex(ref PasValue valueRead)
        {
            var unaryOp = PasValueOperator.None;
            if (HasUnary(ref unaryOp))
            {
                PasValue arg = null;
                Require(HasComplex(ref arg), "expecting unary argument");
                valueRead = new PasValue { Kind = PasValueKind.Operation, Operator = unaryOp };
                valueRead.Args.Add(arg);
                return true;
            }
            if (HasSimple(ref valueRead))
            {
                while (HasDotSufix(ref valueRead) || HasParSufix(ref valueRead) || HasBraSufix(ref valueRead) ||
                    HasPtrSufix(ref valueRead))
                    ;
                return true;
            }
            return false;
        }

        bool HasUnary(ref PasValueOperator operatorRead)
        {
            int opIndex = 0;
            if (parser.ThisWord(new[] { "not" }, ref opIndex))
            {
                switch (opIndex)
                {
                    case 0:
                        operatorRead = PasValueOperator.Not;
                        break;
                    default:
                        throw new Exception("internal error");
                }
                return true;
            }
            if (parser.ThisText(new[] { "+", "-", "@" }, ref opIndex))
            {
                switch (opIndex)
                {
                    case 0:
                        operatorRead = PasValueOperator.Positive;
                        break;
                    case 1:
                        operatorRead = PasValueOperator.Negative;
                        break;
                    case 2:
                        operatorRead = PasValueOperator.AddressOf;
                        break;
                    default:
                        throw new Exception("internal error");
                }
                return true;
            }
            return false;
        }

        bool HasBinary(ref PasValueOperator operatorRead)
        {
            int opIndex = 0;
            if (parser.ThisWord(new[] { "and", "or", "xor", "is", "in", "div", "mod", "shl", "shr", "as" }, ref opIndex))
            {
                switch (opIndex)
                {
                    case 0:
                        operatorRead = PasValueOperator.And;
                        break;
                    case 1:
                        operatorRead = PasValueOperator.Or;
                        break;
                    case 2:
                        operatorRead = PasValueOperator.Xor;
                        break;
                    case 3:
                        operatorRead = PasValueOperator.InstanceOf;
                        break;
                    case 4:
                        operatorRead = PasValueOperator.Belongs;
                        break;
                    case 5:
                        operatorRead = PasValueOperator.IntDiv;
                        break;
                    case 6:
                        operatorRead = PasValueOperator.Remainder;
                        break;
                    case 7:
                        operatorRead = PasValueOperator.ShiftLeft;
                        break;
                    case 8:
                        operatorRead = PasValueOperator.ShiftRight;
                        break;
                    case 9:
                        operatorRead = PasValueOperator.CastAs;
                        break;
                    default:
                        throw new Exception("internal error");
                }
                return true;
            }
            if (parser.ThisText(new[] { ">=", "<=", "<>", "+", "-", "*", "/", "<", ">", "=", ".." }, ref opIndex))
            {
                switch (opIndex)
                {
                    case 0:
                        operatorRead = PasValueOperator.NonLess;
                        break;
                    case 1:
                        operatorRead = PasValueOperator.NonGreater;
                        break;
                    case 2:
                        operatorRead = PasValueOperator.Inequal;
                        break;
                    case 3:
                        operatorRead = PasValueOperator.Sum;
                        break;
                    case 4:
                        operatorRead = PasValueOperator.Subtract;
                        break;
                    case 5:
                        operatorRead = PasValueOperator.Multiply;
                        break;
                    case 6:
                        operatorRead = PasValueOperator.Divide;
                        break;
                    case 7:
                        operatorRead = PasValueOperator.Less;
                        break;
                    case 8:
                        operatorRead = PasValueOperator.Greater;
                        break;
                    case 9:
                        operatorRead = PasValueOperator.Equal;
                        break;
                    case 10:
                        operatorRead = PasValueOperator.Interval;
                        break;
                    default:
                        throw new Exception("internal error");
                }
                return true;
            }
            return false;
        }

        PasValue Slice(List<object> items, int startIndex, int endIndex)
        {
            // encontro o operador de menor precedencia e seu indice
            int divIndex = -1;
            int divPrec = -1;
            for (int i = startIndex; i <= endIndex; i++)
                if (items[i] is PasValueOperator)
                {
                    int tempPrec = PasValue.Precedence((PasValueOperator)items[i]);
                    if (divIndex == -1 || tempPrec > divPrec)
                    {
                        divPrec = tempPrec;
                        divIndex = i;
                    }
                }
            PasValue sliced;
            if (divIndex == -1)
            {
                // se nao ha um divisor, so pode have um elemento, e este eh um valor
                Require(startIndex == endIndex && items[startIndex] is PasValue, "not a value");
                sliced = items[startIndex] as PasValue;
            }
            else if (divIndex == startIndex)
            {
                // critica falsos operadores unarios
                Require(endIndex - startIndex == 1 && items[startIndex] is PasValueOperator && items[endIndex] is PasValue, "not an unary op");
                sliced = new PasValue { Kind = PasValueKind.Operation, Operator = (PasValueOperator)items[startIndex] };
                sliced.Args.Add(items[endIndex] as PasValue);
            }
            else
            {
                Require(divIndex > startIndex && divIndex < endIndex, "no middle");
                sliced = new PasValue { Kind = PasValueKind.Operation, Operator = (PasValueOperator)items[divIndex] };
                sliced.Args.Add(Slice(items, startIndex, divIndex - 1));
                sliced.Args.Add(Slice(items, divIndex + 1, endIndex));
            }
            return sliced;
        }

        bool HasValue(ref PasValue valueRead)
        {
            var priorPos = parser.Pos;
            var itemsRead = new List<object>();
            PasValue oneValue = null;
            if (HasComplex(ref oneValue))
            {
                itemsRead.Add(oneValue);
                PasValueOperator op = PasValueOperator.None;
                while (HasBinary(ref op))
                {
                    itemsRead.Add(op);
                    Require(HasComplex(ref oneValue), "expecting binary operand");
                    itemsRead.Add(oneValue);
                }
                valueRead = Slice(itemsRead, 0, itemsRead.Count - 1);
                return true;
            }
            parser.Pos = priorPos;
            return false;
        }

        bool HasQuoted(ref string strRead)
        {
            if (HasChar('\''))
            {
                var buff = new StringBuilder();
                char ch = '\x00';
                while (parser.AnyChar(ref ch))
                    if (ch == '\'')
                        break;
                    else
                        buff.Append(ch);
                strRead = buff.ToString();
                return true;
            }
            return false;
        }

        bool HasChrLit(ref string strRead)
        {
            if (HasChar('#'))
            {
                long asc;
                char foo;
                Require(HasHexOrDec(out asc, out foo), "expecting asc code");
                strRead = ((Char)asc).ToString();
                return true;
            }
            return false;
        }

        bool HasStrLit(ref PasValue valueRead)
        {
            parser.Skip();
            var buff = new StringBuilder();
            string strPiece = null;
            int piecesRead = 0;
            while (HasQuoted(ref strPiece) || HasChrLit(ref strPiece))
            {
                piecesRead++;
                buff.Append(strPiece);
            }
            if (piecesRead > 0)
            {
                valueRead = new PasValue { Kind = PasValueKind.StrLiteral, StrData = buff.ToString() };
                return true;
            }
            return false;
        }

        bool HasIntLit(ref PasValue valueRead)
        {
            parser.Skip();
            var priorPos = parser.Pos;
            int signal = 1;
            char charRead = '\x0';
            if (HasChar("-+", ref charRead))
                signal = (charRead == '-') ? -1 : 1;
            long intPart;
            char intFormat;
            if (HasHexOrDec(out intPart, out intFormat))
            {
                bool isInt = true;
                var befPt = parser.Pos;
                if (HasChar('.'))
                    if (HasChar('.'))
                        parser.Pos = befPt;
                    else
                        isInt = false;
                if (isInt)
                {
                    valueRead = new PasValue
                    {
                        Kind = intFormat == 'H' ? PasValueKind.HexLiteral : PasValueKind.IntLiteral,
                        IntData = signal * intPart
                    };
                    return true;
                }
            }
            parser.Pos = priorPos;
            return false;
        }

        bool HasFloatLit(ref PasValue valueRead)
        {
            parser.Skip();
            var priorPos = parser.Pos;
            var buff = new StringBuilder();
            char signalRead = '\x0';
            if (HasChar("-+", ref signalRead))
                buff.Append(signalRead);
            string digitsRead = null;
            if (HasDigits(ref digitsRead))
                buff.Append(digitsRead);
            if (HasChar('.'))
            {
                buff.Append('.');
                if (HasDigits(ref digitsRead))
                {
                    buff.Append(digitsRead);
                    if (HasChar("Ee"))
                    {
                        buff.Append('E');
                        Require(HasChar("+-", ref signalRead), "expecting '+/-'");
                        buff.Append(signalRead);
                        Require(HasDigits(ref digitsRead), "expecting cientific");
                        buff.Append(digitsRead);
                    }
                    valueRead = new PasValue { Kind = PasValueKind.FloatLiteral, FloatData = float.Parse(buff.ToString()) };
                    return true;
                }
            }
            parser.Pos = priorPos;
            return false;
        }

        bool HasParVal(ref PasValue valueRead)
        {
            if (HasPar())
            {
                valueRead = new PasValue { Kind = PasValueKind.Parenthesis };
                do
                {
                    valueRead.Args.Add(ReqValue());
                } while (HasComma());
                ReqEndPar();
                return true;
            }
            return false;
        }

        bool HasBraVal(ref PasValue valueRead)
        {
            if (HasBra())
            {
                valueRead = new PasValue { Kind = PasValueKind.Brackets };
                PasValue item = null;
                if (HasValue(ref item))
                {
                    valueRead.Args.Add(item);
                    while (HasComma())
                        valueRead.Args.Add(ReqValue());
                }
                ReqEndBra();
                return true;
            }
            return false;
        }
    }
}
