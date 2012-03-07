using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PasCode
{
    public partial class PasReader
    {
        class SetDef
        {
            public int DefStart;
            public int DefEnd;
            public string Symbol;
            public IfDef Parent;
        }

        class IfDef
        {
            public string Symbol;
            public IfDef Parent;
            public int IfStart = -1;
            public int IfEnd = -1;
            public int ElseStart = -1;
            public int ElseEnd = -1;
            public int EndStart = -1;
            public int EndEnd = -1;
            public bool Logic = true;
            public List<object> ThenChildren = new List<object>();
            public bool ReadingElse = false;
            public List<object> ElseChildren = new List<object>();
        }

        List<object> GetDefs(string unitText)
        {
            var defs = new List<object>();
            IfDef currentIf = null;
            var dirEnd = 0;
            while (true)
            {
                var dirStart = unitText.IndexOf("{$", dirEnd);
                if (dirStart == -1)
                    break;
                dirEnd = unitText.IndexOf("}", dirStart);
                if (dirEnd == -1)
                    throw new Exception("Diretiva incompleta");
                dirEnd++;
                var dirCode = unitText.Substring(dirStart + 2, dirEnd - 1 - (dirStart + 2)).Trim().ToLower();
                var dirPair = dirCode.Split(' ');
                if (dirPair[0] == "ifdef" || dirPair[0] == "ifndef")
                {
                    var newIf = new IfDef();
                    newIf.IfStart = dirStart;
                    newIf.IfEnd = dirEnd;
                    newIf.Logic = dirPair[0] == "ifdef";
                    newIf.Symbol = dirPair[1];
                    if (currentIf != null)
                    {
                        if (currentIf.ReadingElse)
                            currentIf.ElseChildren.Add(newIf);
                        else
                            currentIf.ThenChildren.Add(newIf);
                        newIf.Parent = currentIf;
                    }
                    else
                        defs.Add(newIf);
                    currentIf = newIf;
                }
                else if (dirPair[0] == "else")
                {
                    if (currentIf == null)
                        throw new Exception("'Else' sem if");
                    currentIf.ElseStart = dirStart;
                    currentIf.ElseEnd = dirEnd;
                    currentIf.ReadingElse = true;
                }
                else if (dirPair[0] == "endif")
                {
                    if (currentIf == null)
                        throw new Exception("'EndIf' sem if");
                    currentIf.EndStart = dirStart;
                    currentIf.EndEnd = dirEnd;
                    currentIf = currentIf.Parent;
                }
                else if (dirPair[0] == "define")
                {
                    var newDef = new SetDef();
                    newDef.DefStart = dirStart;
                    newDef.DefEnd = dirEnd;
                    newDef.Symbol = dirPair[1];
                    newDef.Parent = currentIf;
                    if (currentIf != null)
                        if (currentIf.ReadingElse)
                            currentIf.ElseChildren.Add(newDef);
                        else
                            currentIf.ThenChildren.Add(newDef);
                    else
                        defs.Add(newDef);
                }
                else
                    throw new Exception("Diretiva desconhecida: " + dirCode);
            }
            return defs;
        }

        void Scan(List<object> defs, string sourceCode, StringBuilder destCode, ref int readPos, List<string> preDefs)
        {
            foreach (var def in defs)
                if (def is SetDef)
                {
                    var setDef = def as SetDef;
                    destCode.Append(sourceCode.Substring(readPos, setDef.DefStart - readPos));
                    readPos = setDef.DefEnd;
                    preDefs.Add(setDef.Symbol);
                }
                else if (def is IfDef)
                {
                    var ifDef = def as IfDef;
                    destCode.Append(sourceCode.Substring(readPos, ifDef.IfStart - readPos));
                    var passed = preDefs.Contains(ifDef.Symbol) == ifDef.Logic;
                    if (passed)
                    {
                        readPos = ifDef.IfEnd;
                        Scan(ifDef.ThenChildren, sourceCode, destCode, ref readPos, preDefs);
                        if (ifDef.ElseStart != -1)
                        {
                            destCode.Append(sourceCode.Substring(readPos, ifDef.ElseStart - readPos));
                            readPos = ifDef.EndStart;
                        }
                    }
                    else if (ifDef.ElseStart != -1)
                    {
                        readPos = ifDef.ElseEnd;
                        Scan(ifDef.ElseChildren, sourceCode, destCode, ref readPos, preDefs);
                    }
                    else
                        readPos = ifDef.EndStart;
                    destCode.Append(sourceCode.Substring(readPos, ifDef.EndStart - readPos));
                    readPos = ifDef.EndEnd;
                }
        }

        string SolvePreCompiler(string unitText, string defines)
        {
            var preDefs = defines.ToLower().Split(';').ToList();
            var defs = GetDefs(unitText);
            var cleanCode = new StringBuilder();
            var readPos = 0;
            Scan(defs, unitText, cleanCode, ref readPos, preDefs);
            cleanCode.Append(unitText.Substring(readPos));

            ///
            File.WriteAllText(@"c:\temp\rlutils.txt", cleanCode.ToString());
            ///

            return cleanCode.ToString();
        }



    }
}
