using PasCode;
using CsCode;
using Pas2Cs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

namespace DelphiToCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var srcf = @"c:\temp\RLXLSFilter.pas";///
            var dstf = @"c:\temp\RLXLSFilter.cs";///

            var p = new PasReader().ReadUnitFile(srcf, "DELPHI;MSWINDOWS;DELPHI7;VCL");
            p.Solve();
            var c = new PasToCsConverter().ConvertPasUnit(p);
            new CsWriter().WriteCsFile(c, dstf);
        }
    }
}
