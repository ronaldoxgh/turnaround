using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace CsCode
{
    public static class CsTranslator
    {
        public static string TranslateExpr(string expr)
        {
            if (Regex.IsMatch(expr, @"^StringReplace\(.+\)$", RegexOptions.IgnoreCase))
                return TranslateStringReplace(expr);
            if (Regex.IsMatch(expr, @"^SameText\(.+\)$", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(expr, @"^AnsiSameText\(.+\)$", RegexOptions.IgnoreCase))
                return TranslateSameText(expr);
            if (Regex.IsMatch(expr, @"^Trim\(.+\)$", RegexOptions.IgnoreCase))
                return TranslateTrim(expr);
            if (Regex.IsMatch(expr, @"^IfThen\(.+\)$", RegexOptions.IgnoreCase))
                return TranslateIfThen(expr);
            if (Regex.IsMatch(expr, @"^AnsiStartsText\(.+\)$", RegexOptions.IgnoreCase))
                return TranslateAnsiStartsText(expr);
            if (Regex.IsMatch(expr, @"^AnsiEndsText\(.+\)$", RegexOptions.IgnoreCase))
                return TranslateAnsiEndsText(expr);
            if (Regex.IsMatch(expr, @"^Copy\(.+\)$", RegexOptions.IgnoreCase))
                return TranslateCopy(expr);
            if (Regex.IsMatch(expr, @"^Abs\(.+\)$", RegexOptions.IgnoreCase))
                return "Math." + expr;
            if (Regex.IsMatch(expr, @"^FreeMem\(.+\)$", RegexOptions.IgnoreCase))
                return SplitParams(expr)[0] + " = null";
            if (Regex.IsMatch(expr, @"^Inc\(.+\)$", RegexOptions.IgnoreCase))
                return SplitParams(expr)[0] + "++";
            if (Regex.IsMatch(expr, @"^Dec\(.+\)$", RegexOptions.IgnoreCase))
                return SplitParams(expr)[0] + "--";
            if (Regex.IsMatch(expr, @"^Length\(.+\)$", RegexOptions.IgnoreCase))
                return MayParens(SplitParams(expr)[0]) + ".Length";
            if (Regex.IsMatch(expr, @"^SizeOf\(.+\)$", RegexOptions.IgnoreCase))
                return "sizeof(" + SplitParams(expr)[0] + ")";
            if (Regex.IsMatch(expr, @"^.+\.Free$", RegexOptions.IgnoreCase))
                return expr.Substring(0, expr.LastIndexOf('.')) + " = null";
            if (Regex.IsMatch(expr, @"^System.Str\(.+\)$", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(expr, @"^Str\(.+\)$", RegexOptions.IgnoreCase))
                return TranslateSystemStr(expr);
            if (expr.Equals("Self", StringComparison.OrdinalIgnoreCase))
                return "this";
            if (expr.Equals("TObjectList", StringComparison.OrdinalIgnoreCase) ||
                expr.Equals("TList", StringComparison.OrdinalIgnoreCase))
                return "List<object>";
            if (expr.Equals("TStringList", StringComparison.OrdinalIgnoreCase))
                return "List<string>";
            if (expr.Equals("TMemoryStream", StringComparison.OrdinalIgnoreCase))
                return "MemoryStream";
            if (expr.Equals("TFileStream", StringComparison.OrdinalIgnoreCase))
                return "System.IO.FileStream";
            if (expr.Equals("TStream", StringComparison.OrdinalIgnoreCase))
                return "System.IO.Stream";
            if (Regex.IsMatch(expr, @"^new\ FileStream\("))
                return TranslateNewFileStream(expr);
            if (Regex.IsMatch(expr, @"^\!\(\!\(.+\)\)$"))
                return Regex.Replace(Regex.Replace(expr, @"^\!\(!\(", ""), @"\)\)$", "");
            if (Regex.IsMatch(expr, @"^GetMem\(\w+\,.+\)"))
                return TranslateGetMem(expr);

            return expr;
        }

        private static string TranslateGetMem(string expr)
        {
            var spl = SplitParams(expr);
            expr = spl[0] + " = new byte[" + spl[1] + "]";
            return expr;
        }

        private static string TranslateNewFileStream(string expr)
        {
            // new FileStream(AFileName, fmCreate)
            var spl = SplitParams(expr);
            if (spl[1].Equals("fmCreate", StringComparison.OrdinalIgnoreCase))
                expr = string.Format("System.IO.File.Create({0})", spl[0]);

            return expr;
        }

        private static string TranslateSystemStr(string aux)
        {
            var spl = SplitParams(aux);
            aux = spl[1] + " = " + spl[0] + ".ToString().Replace(',', '.')";
            return aux;
        }

        private static string TranslateCopy(string aux)
        {
            var spl = SplitParams(aux);
            var ini = spl[1];
            if (Regex.IsMatch(ini, @"^\d+$"))
                ini = (int.Parse(spl[1]) - 1).ToString();
            else
                ini = MayParens(spl[1]) + " - 1";
            aux = MayParens(spl[0]) + string.Format(".Substring({0}, {1})", ini, spl[2]);
            return aux;
        }

        private static string TranslateAnsiStartsText(string aux)
        {
            var spl = SplitParams(aux);
            aux = MayParens(spl[1]) + ".StartsWith(" + spl[0] + MayStrComp(spl[0]) + ")";
            return aux;
        }

        private static string MayStrComp(string aux)
        {
            var needed = !(Regex.IsMatch(aux, "^\".+\"$") && !Regex.IsMatch(aux, "[a-zA-Z]"));
            return needed ? ", StringComparison.OrdinalIgnoreCase" : "";
        }

        private static string TranslateAnsiEndsText(string aux)
        {
            var spl = SplitParams(aux);
            aux = MayParens(spl[1]) + ".EndsWith(" + spl[0] + MayStrComp(spl[0]) + ")";
            return aux;
        }

        private static string TranslateIfThen(string aux)
        {
            var spl = SplitParams(aux);
            aux = MayParens(spl[0]) + " ? " + MayParens(spl[1]) + " : " + MayParens(spl[2]);
            return aux;
        }

        private static string MayParens(string aux)
        {
            if (Regex.IsMatch(aux, "^\".*\"$") || Regex.IsMatch(aux, @"^\w+$") ||
                Regex.IsMatch(aux, @"[\w\.\[\]\(\)]+"))
                return aux;
            return "(" + aux + ")";
        }

        private static string TranslateTrim(string aux)
        {
            var spl = SplitParams(aux);
            aux = MayParens(spl[0]);
            return aux;
        }

        private static string TranslateSameText(string aux)
        {
            var spl = SplitParams(aux);
            var left = MayParens(spl[0]);
            var right = spl[1];
            if (Regex.IsMatch(right, "[a-zA-Z]"))
                aux = left + string.Format(".Equals({0}" + MayStrComp(right) + ")", right);
            else
                aux = left + " == " + right;
            return aux;
        }

        private static string[] SplitParams(string aux)
        {
            aux = aux.Substring(aux.IndexOf('(') + 1);
            aux = aux.Substring(0, aux.LastIndexOf(')'));
            var chars = aux.ToCharArray();
            var quot = false;
            var pars = 0;
            for (var i = 0; i < chars.Length; i++)
                if (chars[i] == '"')
                    quot = !quot;
                else if (!quot && pars == 0 && chars[i] == ',')
                    chars[i] = '\t';
                else if (!quot && (chars[i] == '(' || chars[i] == '['))
                    pars++;
                else if (!quot && (chars[i] == ')' || chars[i] == ']'))
                    pars--;
            var spl = new String(chars).Split('\t');
            for (var i = 0; i < spl.Length; i++)
                spl[i] = spl[i].Trim();
            return spl;
        }

        private static string TranslateStringReplace(string aux)
        {
            // StringReplace(Value, "\x0D\x0A", "\x0A", [rfReplaceAll])
            var spl = SplitParams(aux);
            if (spl.Length == 4 && spl[3] == "[rfReplaceAll]")
                aux = MayParens(spl[0]) + string.Format(".Replace({0}, {1})", spl[1], spl[2]);
            return aux;
        }
    }
}
