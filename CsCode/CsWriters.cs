using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace CsCode
{
    public class CsWriter
    {
        StringBuilder outLines;
        int indentSize = 4;
        int indent = 0;
        string IndentStr { get { return new string(' ', indent * indentSize); } }

        void Writeln() { Writeln(""); }
        void Writeln(string line)
        {
            if (line != "")
                outLines.AppendLine(IndentStr + line);
            else
                outLines.AppendLine();
        }

        public void WriteCsFile(CsNamespace ns, string fileName)
        {
            File.WriteAllText(fileName, WriteCsCode(ns));
        }

        public string WriteCsCode(CsNamespace ns)
        {
            outLines = new StringBuilder();
            try
            {
                Writeln("namespace " + ns.Name);
                Writeln("{");
                ++indent;
                foreach (var use in ns.Usings)
                    Writeln("using " + use + ";");
                Writeln();
                WriteDecls(ns.Decls);
                --indent;
                Writeln("}");
                return outLines.ToString();
            }
            finally
            {
                outLines = null;
            }
        }

        void WriteDecls(List<CsDecl> decls)
        {
            foreach (var decl in decls)
                if (decl is CsType)
                    WriteType(decl as CsType);
                else if (decl is CsMethodDecl)
                    WriteMethod(decl as CsMethodDecl);
                else if (decl is CsField)
                    WriteField(decl as CsField);
                else if (decl is CsProperty)
                    WriteProperty(decl as CsProperty);
                else
                    throw new Exception(string.Format("Tipo desconhecido: {0}", decl.GetType().Name));
        }

        void WriteType(CsType type)
        {
            if (type is CsClassTypeDecl)
                WriteClass(type as CsClassTypeDecl);
            else if (type is CsEnumTypeDecl)
                WriteEnum(type as CsEnumTypeDecl);
            else if (type is CsAliasTypeDecl)
                WriteAlias(type as CsAliasTypeDecl);
            else if (type is CsStructTypeDecl)
                WriteStruct(type as CsStructTypeDecl);
            else
                throw new Exception(string.Format("Subtipo desconhecido: {0}", type.GetType().Name));
        }

        void WriteEnum(CsEnumTypeDecl en)
        {
            var consts = "";
            foreach (var cons in en.Consts)
                consts += (consts != "" ? ", " : "") + cons.Name;
            Writeln(VisibilityText(en.Visibility) + "enum " + en.Name + " {" + consts + "}");
            Writeln();
        }

        void WriteAlias(CsAliasTypeDecl als)
        {
            Writeln(VisibilityText(als.Visibility) + "class " + als.Name + ": " + als.TargetTypeName + " {}");
            Writeln();
        }

        CsClassTypeDecl writingClass;
        void WriteClass(CsClassTypeDecl cls)
        {
            var saved = writingClass;
            writingClass = cls;
            try
            {
                var intfs = "";
                if (cls.AncestorRef != null)
                    intfs = cls.AncestorRef.Decl.Name;
                foreach (var intf in cls.Interfaces)
                    intfs += (intfs != "" ? ", " : "") + intf;
                Writeln(VisibilityText(cls.Visibility) + (cls.IsStatic ? "static " : "") + "class " + cls.Name + (intfs != "" ? ": " + intfs : ""));
                Writeln("{");
                ++indent;
                WriteDecls(cls.Decls);
                --indent;
                Writeln("}");
                Writeln();
            }
            finally
            {
                writingClass = saved;
            }
        }

        void WriteStruct(CsStructTypeDecl str)
        {
            Writeln(VisibilityText(str.Visibility) + "struct " + str.Name);
            Writeln("{");
            ++indent;
            WriteDecls(str.Decls);
            --indent;
            Writeln("}");
            Writeln();
        }

        string ParamsText(List<CsParamDecl> pars)
        {
            var ret = "";
            foreach (var par in pars)
                ret += (ret != "" ? ", " : "") + ParamText(par);
            return ret;
        }

        string BindText(CsParamBind bind)
        {
            switch (bind)
            {
                case CsParamBind.Copy:
                    return "";
                case CsParamBind.Ref:
                    return "ref ";
            }
            throw new Exception(string.Format("Tipo desconhecido: {0}", bind.ToString()));
        }

        string ParamText(CsParamDecl par)
        {
            return BindText(par.Bind) + CsRefText(par.TypeRef) + " " + par.Name;
        }

        CsMethodDecl writingMethod;
        void WriteMethod(CsMethodDecl decl)
        {
            var saved = writingMethod;
            writingMethod = decl;
            try
            {
                string typeAndName;
                if (decl.IsConstructor)
                    typeAndName = writingClass.Name;
                else if (decl.IsDestructor)
                    typeAndName = "~" + writingClass.Name;
                else
                    typeAndName = CsRefText(decl.ReturnType) + " " + decl.Name;
                Writeln(
                    VisibilityText(decl.Visibility) +
                    (decl.IsStatic ? "static " : "") +
                    (decl.IsAbstract ? "abstract " : "") +
                    (decl.IsOverride ? "override " : "") +
                    (decl.IsVirtual ? "virtual " : "") +
                    typeAndName +
                    "(" + ParamsText(decl.Params) + ")" +
                    (decl.BaseCall != null ? ": " + BaseCallStr(decl.BaseCall, false) : ""));
                if (decl.ReturnType != null)
                    decl.Codes.Add(new CsCall { Value = new CsValue { Kind = CsValueKind.Name, StrData = "return Result" } });
                WriteCodes(decl.Codes, CodesStyle.Block, false);
                Writeln();
            }
            finally
            {
                writingMethod = saved;
            }
        }

        enum CodesStyle { Block, BlockOrIndentedLine, BlockOrUnindentedLine, LinesOnly }
        void WriteCodes(List<CsStat> codes, CodesStyle style, bool hasElse)
        {
            if (codes.Count == 1 && codes[0] is CsBegin)
            {
                Writeln("{");
                ++indent;
                WriteCodes((codes[0] as CsBegin).Codes, CodesStyle.LinesOnly, false);
                --indent;
                Writeln("}");
            }
            else
            {
                var isLine = codes.Count <= 1;
                bool doBlock;
                bool doIndent;
                if (isLine)
                {
                    doBlock = style == CodesStyle.Block;
                    doIndent = style == CodesStyle.Block || style == CodesStyle.BlockOrIndentedLine;
                }
                else
                {
                    doBlock = style == CodesStyle.Block || style == CodesStyle.BlockOrIndentedLine || style == CodesStyle.BlockOrUnindentedLine;
                    doIndent = style != CodesStyle.LinesOnly;
                }

                if (doBlock)
                {
                    Writeln("{");
                    hasElse = false;
                }
                if (doIndent)
                    ++indent;
                if (codes.Count == 0 && !doBlock)
                    Writeln(";");
                else
                    foreach (var code in codes)
                        WriteCode(code, hasElse);
                if (doIndent)
                    --indent;
                if (doBlock)
                    Writeln("}");
            }
        }

        void WriteCode(CsStat code) { WriteCode(code, false); }
        void WriteCode(CsStat code, bool nextIf)
        {
            if (code is CsBegin)
                WriteBegin(code as CsBegin);
            else if (code is CsIf)
                WriteIf(code as CsIf, false, nextIf);
            else if (code is CsWhile)
                WriteWhile(code as CsWhile);
            else if (code is CsFor)
                WriteFor(code as CsFor);
            else if (code is CsRepeat)
                WriteRepeat(code as CsRepeat);
            else if (code is CsSwitch)
                WriteSwitch(code as CsSwitch);
            else if (code is CsAssignment)
                WriteAssignment(code as CsAssignment);
            else if (code is CsTry)
                WriteTry(code as CsTry);
            else if (code is CsCall)
                WriteValueCall(code as CsCall);
            else if (code is CsBase)
                WriteBaseCall(code as CsBase);
            else if (code is CsLocalVarDecl)
                WriteLocalVar(code as CsLocalVarDecl);
            else if (code is CsThrow)
                WriteThrow(code as CsThrow);
            else
                throw new Exception(string.Format("Subtipo desconhecido: {0}", code.GetType().Name));
        }

        void WriteBegin(CsBegin beginCode)
        {
            WriteCodes(beginCode.Codes, CodesStyle.BlockOrUnindentedLine, false);
        }

        void WriteIf(CsIf ifCode) { WriteIf(ifCode, false, false); }
        void WriteIf(CsIf ifCode, bool isElse, bool needsElse)
        {
            var hasElse = needsElse || ifCode.FalseCodes.Count > 0;
            Writeln((isElse ? "else " : "") + "if (" + ValueText(ifCode.Condition) + ")");
            WriteCodes(ifCode.TrueCodes, CodesStyle.BlockOrIndentedLine, hasElse);
            if (ifCode.FalseCodes.Count == 1 && ifCode.FalseCodes[0] is CsIf)
                WriteIf(ifCode.FalseCodes[0] as CsIf, true, hasElse);
            else if (hasElse)
            {
                Writeln("else");
                WriteCodes(ifCode.FalseCodes, CodesStyle.BlockOrIndentedLine, hasElse);
            }
        }

        void WriteWhile(CsWhile whileCode)
        {
            var cond = ValueText(whileCode.Condition);
            Writeln(string.Format("while ({0})", cond));
            WriteCodes(whileCode.Codes, CodesStyle.BlockOrIndentedLine, false);
        }

        void WriteFor(CsFor forCode)
        {
            Writeln(string.Format("for ({0} = {1}; {0} {4} {2}; {0}{3})",
                forCode.IteratorName,
                ValueText(forCode.ValueFrom),
                ValueText(forCode.ValueTo),
                forCode.Step == CsForStep.UpTo ? "++" : "--",
                forCode.Step == CsForStep.DownTo ? ">=" : "<="));
            WriteCodes(forCode.Codes, CodesStyle.BlockOrIndentedLine, false);
        }

        void WriteRepeat(CsRepeat repeatCode)
        {
            Writeln("do");
            Writeln("{");
            ++indent;
            WriteCodes(repeatCode.Codes, CodesStyle.LinesOnly, false);
            --indent;
            var cond = CsTranslator.TranslateExpr("!(" + ValueText(repeatCode.ExitCondition) + ")");
            Writeln("} " + string.Format("while ({0});", cond));
        }

        void WriteSwitch(CsSwitch switchCode)
        {
            Writeln(string.Format("switch ({0})", ValueText(switchCode.SubjectValue)));
            Writeln("{");
            ++indent;
            foreach (var cas in switchCode.Cases)
                WriteSwitchCase(cas);
            if (switchCode.DefaultCodes.Count > 0)
            {
                Writeln("default:");
                ++indent;
                WriteCodes(switchCode.DefaultCodes, CodesStyle.LinesOnly, false);
                --indent;
            }
            --indent;
            Writeln("}");
        }

        void WriteSwitchCase(CsSwitchCase cas)
        {
            foreach (var val in cas.Values)
                Writeln("case " + ValueText(val) + ":");
            ++indent;
            WriteCodes(cas.Codes, CodesStyle.LinesOnly, false);
            Writeln("break;");
            --indent;
        }

        void WriteAssignment(CsAssignment assignmentCode)
        {
            Writeln(ValueText(assignmentCode.LeftSide) + " = " + ValueText(assignmentCode.RightSide) + ";");
        }

        void WriteThrow(CsThrow throwCode)
        {
            if (throwCode.ExceptionValue == null)
                Writeln("throw;");
            else
                Writeln("throw " + ValueText(throwCode.ExceptionValue) + ";");
        }

        void WriteTry(CsTry tryCode)
        {
            Writeln("try");
            WriteCodes(tryCode.Codes, CodesStyle.Block, false);
            if (tryCode.HasExceptionHandler)
            {
                if (tryCode.Exceptions.Count > 0)
                {
                    foreach (var ex in tryCode.Exceptions)
                    {
                        var tp = CsRefText(ex.ExceptionDomain);
                        var vr = ex.Name;
                        if (!string.IsNullOrEmpty(tp) && !string.IsNullOrEmpty(vr))
                            Writeln(string.Format("catch ({0} {1})", tp, vr));
                        else if (!string.IsNullOrEmpty(tp))
                            Writeln(string.Format("catch ({0})", tp));
                        else
                            Writeln("catch");
                        WriteCodes(ex.Codes, CodesStyle.Block, false);
                    }
                    if (tryCode.HasElseExceptionCodes)
                    {
                        Writeln("catch (Exception e)");
                        WriteCodes(tryCode.ElseExceptionCodes, CodesStyle.Block, false);
                    }
                }
                else
                {
                    Writeln("catch");
                    WriteCodes(tryCode.UntypedExceptionCodes, CodesStyle.Block, false);
                }
            }
            if (tryCode.FinallyCodes.Count > 0)
            {
                Writeln("finally");
                WriteCodes(tryCode.FinallyCodes, CodesStyle.Block, false);
            }
        }

        void WriteValueCall(CsCall valueCallCode)
        {
            var aux = ValueText(valueCallCode.Value);
            if (aux.Equals("Exit", StringComparison.OrdinalIgnoreCase))
            {
                if (writingMethod.ReturnType != null)
                    Writeln("return Result;");
                else
                    Writeln("return;");
            }
            else if (Regex.IsMatch(aux, @"^\w+\.\w+$"))
                Writeln(aux + "();");
            else
                Writeln(aux + ";");
        }

        void WriteLocalVar(CsLocalVarDecl localVar)
        {
            if (localVar.TypeRef.Decl is CsDelegateDomain && string.IsNullOrEmpty(localVar.TypeRef.Name))
                Writeln("var " + localVar.Name + " = " + DelegateBodyText(localVar.TypeRef.Decl as CsDelegateDomain) + ";");
            else
                Writeln(CsRefText(localVar.TypeRef) + " " + localVar.Name +
                    (localVar.InitialValue != null ? " = " + ValueText(localVar.InitialValue) : "") + ";");
        }

        string DelegateBodyText(CsDelegateDomain csDel)
        {
            var backup = outLines;
            outLines = new StringBuilder();
            WriteCodes(csDel.Codes, CodesStyle.LinesOnly, false);
            var gen = outLines.ToString();
            outLines = backup;
            return "(" + ParamsText(csDel.Params) + ") => {" + gen + "}";
        }

        string BaseCallStr(CsBase baseCallCode, bool includeName = true)
        {
            if (baseCallCode.Value != null)
            {
                var aux = ValueText(baseCallCode.Value);
                if (!includeName && aux.Equals(writingMethod.Name, StringComparison.OrdinalIgnoreCase))
                    return "base()";
                return "base." + aux;
            }
            else
            {
                var paramNames = new StringBuilder();
                writingMethod.Params.ForEach(p =>
                {
                    if (paramNames.Length > 0)
                        paramNames.Append(", ");
                    paramNames.Append(p.Name);
                });
                return "base" + (includeName ? "." + writingMethod.Name : "") + "(" + paramNames + ")";
            }
        }

        void WriteBaseCall(CsBase baseCallCode)
        {
            Writeln(BaseCallStr(baseCallCode));
        }

        string VisibilityText(CsClassVisibility vis)
        {
            switch (vis)
            {
                case CsClassVisibility.Default:
                    return "";
                case CsClassVisibility.Internal:
                    return "internal ";
                case CsClassVisibility.Private:
                    return "private ";
                case CsClassVisibility.Protected:
                    return "protected ";
                case CsClassVisibility.Public:
                    return "public ";
            }
            throw new Exception(string.Format("Tipo desconhecido: {0}", vis.ToString()));
        }

        string VisibilityText(CsNamespaceVisibility vis)
        {
            switch (vis)
            {
                case CsNamespaceVisibility.Default:
                    return "";
                case CsNamespaceVisibility.Internal:
                    return "internal ";
                case CsNamespaceVisibility.Private:
                    return "private ";
                case CsNamespaceVisibility.Public:
                    return "public ";
            }
            throw new Exception(string.Format("Tipo desconhecido: {0}", vis.ToString()));
        }

        string CsRefText(CsRef nameRef)
        {
            if (nameRef != null && nameRef.Decl != null)
                return CsTranslator.TranslateExpr(nameRef.Decl.Name);
            else
                return "void";
        }

        void WriteProperty(CsProperty decl)
        {
            var aux =
                VisibilityText(decl.Visibility) +
                (decl.IsStatic ? "static " : "") +
                CsRefText(decl.TypeRef) + " " +
                decl.Name;
            if (decl.ReaderValue != null || decl.WriterValue != null)
            {
                aux += " {";
                if (decl.ReaderValue != null)
                    aux += " get { return " + ValueText(decl.ReaderValue) + "; }";
                if (decl.WriterValue != null)
                    aux += " set { " + ValueText(decl.WriterValue) + " = value; }";
                aux += " }";
            }
            else
                aux += " { get; set; }";
            Writeln(aux);
        }

        void WriteField(CsField decl)
        {
            Writeln(VisibilityText(decl.Visibility) + (decl.IsStatic ? "static " : "") +
                CsRefText(decl.TypeRef) + " " + decl.Name + (decl.InitialValue != null ? " = " + ValueText(decl.InitialValue) : "") + ";");
        }

        string ValuesJoinText(List<CsValue> values) { return ValuesJoinText(values, ", "); }
        string ValuesJoinText(List<CsValue> values, string sep)
        {
            var cat = "";
            foreach (var val in values)
                cat += (cat != "" ? sep : "") + ValueText(val);
            return cat;
        }

        string StringText(string data)
        {
            var chs = data.ToCharArray();
            var sb = new StringBuilder();
            sb.Append("\"");
            foreach (var ch in chs)
            {
                var asc = (byte)ch;
                if (ch < 32 || ch > 126 || ch == 34 || ch == '\\')
                    sb.Append("\\x" + string.Format("{0:X2}", asc));
                else
                    sb.Append(ch);
            }
            sb.Append("\"");
            return sb.ToString();
        }

        string NamedRefText(CsRef namedRef, string defaultName)
        {
            if (namedRef != null && namedRef.Decl != null)
            {
                if (namedRef.Decl is CsEnumConst)
                {
                    var enumConst = namedRef.Decl as CsEnumConst;
                    if (enumConst.EnumDomain != null)
                        return enumConst.EnumDomain.Name + "." + enumConst.Name;
                    else
                        return enumConst.Name;
                }
                else
                    return namedRef.Decl.Name;
            }
            else
                return defaultName;
        }

        string ValueText(CsValue value)
        {
            return CsTranslator.TranslateExpr(ValueTextToTranslate(value));
        }

        string ValueTextToTranslate(CsValue value)
        {
            if (value == null)
                return "";
            switch (value.Kind)
            {
                case CsValueKind.Parenthesis:
                    return "(" + ValuesJoinText(value.Args) + ")";
                case CsValueKind.Brackets:
                    return (value.Prior != null ? ValueText(value.Prior) : "") + "[" + ValuesJoinText(value.Args) + "]";
                case CsValueKind.IntLiteral:
                    return value.IntData.ToString();
                case CsValueKind.HexLiteral:
                    return "0x" + value.IntData.ToString("X");
                case CsValueKind.StrLiteral:
                    return StringText(value.StrData);
                case CsValueKind.FloatLiteral:
                    return value.FloatData.ToString();
                case CsValueKind.Name:
                    {
                        var aux = NamedRefText(value.NameRef, value.StrData);
                        if (value.Prior != null && aux == "Create")
                            return "new " + ValueText(value.Prior) + (value.Next == null || value.Next.Kind != CsValueKind.CallParams ? "()" : "");
                        else
                            return (value.Prior != null ? ValueText(value.Prior) + "." : "") + aux;
                    }
                case CsValueKind.Operation:
                    return OperationText(value);
                case CsValueKind.CallParams:
                    return (value.Prior != null ? ValueText(value.Prior) : "") + "(" + ValuesJoinText(value.Args) + ")";
                case CsValueKind.SpecialSymbol:
                    return SymbolText(value.SymbolData);
                case CsValueKind.DataAt:
                    return "dataat /*" + "(" + ValuesJoinText(value.Args) + ")" + "*/";
            }
            throw new Exception(string.Format("Tipo desconhecido: {0}", value.GetType().Name));
        }

        string SymbolText(CsSymbol symbol)
        {
            switch (symbol)
            {
                case CsSymbol.False:
                    return "false";
                case CsSymbol.Nil:
                    return "null";
                case CsSymbol.None:
                    return "";
                case CsSymbol.Null:
                    return "null";
                case CsSymbol.True:
                    return "true";
            }
            throw new Exception(string.Format("Tipo desconhecido: {0}", symbol.ToString()));
        }

        string BinOpText(List<CsValue> args, string op)
        {
            return ValueText(args[0]) + " " + op + " " + ValueText(args[1]);
        }

        string FuncOpText(List<CsValue> args, string func)
        {
            return func + "(" + ValuesJoinText(args) + ")";
        }

        string OperationText(CsValue value)
        {
            switch (value.Operator)
            {
                case CsValueOperator.None:
                    return "";
                case CsValueOperator.Positive:
                    return "+" + ValuesJoinText(value.Args);
                case CsValueOperator.Negative:
                    return "-" + ValuesJoinText(value.Args);
                case CsValueOperator.NotMask:
                    return "~" + ValuesJoinText(value.Args);
                case CsValueOperator.AndMask:
                    return BinOpText(value.Args, "&");
                case CsValueOperator.OrMask:
                    return BinOpText(value.Args, "|");
                case CsValueOperator.XorMask:
                    return BinOpText(value.Args, "^");
                case CsValueOperator.Concat:
                    return BinOpText(value.Args, "+");
                case CsValueOperator.Sum:
                    return BinOpText(value.Args, "+");
                case CsValueOperator.Subtract:
                    return BinOpText(value.Args, "-");
                case CsValueOperator.Multiply:
                    return BinOpText(value.Args, "*");
                case CsValueOperator.Divide:
                    return BinOpText(value.Args, "/");
                case CsValueOperator.IntDiv:
                    return BinOpText(value.Args, "/");
                case CsValueOperator.Remainder:
                    return BinOpText(value.Args, "%");
                case CsValueOperator.ShiftLeft:
                    return BinOpText(value.Args, "<<");
                case CsValueOperator.ShiftRight:
                    return BinOpText(value.Args, ">>");
                case CsValueOperator.Equal:
                    return BinOpText(value.Args, "==");
                case CsValueOperator.Inequal:
                    return BinOpText(value.Args, "!=");
                case CsValueOperator.Less:
                    return BinOpText(value.Args, "<");
                case CsValueOperator.Greater:
                    return BinOpText(value.Args, ">");
                case CsValueOperator.NonLess:
                    return BinOpText(value.Args, ">=");
                case CsValueOperator.NonGreater:
                    return BinOpText(value.Args, "<=");
                case CsValueOperator.Not:
                    return "!" + ValuesJoinText(value.Args);
                case CsValueOperator.And:
                    return BinOpText(value.Args, "&&");
                case CsValueOperator.Or:
                    return BinOpText(value.Args, "||");
                case CsValueOperator.Xor:
                    return BinOpText(value.Args, "^^");
                case CsValueOperator.Interval:
                    return FuncOpText(value.Args, "Range");
                case CsValueOperator.Union:
                    return FuncOpText(value.Args, "Union");
                case CsValueOperator.Intersection:
                    return FuncOpText(value.Args, "Intersection");
                case CsValueOperator.Diference:
                    return FuncOpText(value.Args, "Diference");
                case CsValueOperator.Belongs:
                    return FuncOpText(value.Args, "Belongs");
                case CsValueOperator.CastAs:
                    return BinOpText(value.Args, "as");
                case CsValueOperator.InstanceOf:
                    return BinOpText(value.Args, "is");
                case CsValueOperator.AddressOf:
                    return "addressof";///
            }
            throw new Exception(string.Format("Tipo desconhecido: {0}", value.GetType().Name));
        }

    }
}
