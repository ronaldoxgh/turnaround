using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PasCode;
using CsCode;
using Utils;

namespace Pas2Cs
{
    public class PasToCsConverter
    {
        private CsNamespace _namespace;
        private ElemAssociator _associations = new ElemAssociator();
        private PendingSolver _pendings = new PendingSolver();

        public CsNamespace ConvertPasUnit(PasUnit pasUnit)
        {
            var csNamespace = new CsNamespace();
            _associations.Assign(pasUnit, csNamespace);
            _namespace = csNamespace;
            csNamespace.Name = pasUnit.Name;
            foreach (var pasUse in pasUnit.InterfaceUses)
                ConvertPasUse(pasUse, csNamespace);
            foreach (var pasUse in pasUnit.ImplementationUses)
                ConvertPasUse(pasUse, csNamespace);
            foreach (var pasDecl in pasUnit.InterfaceDecls)
                ConvertPasDecl(pasDecl, csNamespace);
            foreach (var pasDecl in pasUnit.ImplementationDecls)
                ConvertPasDecl(pasDecl, csNamespace);
            _namespace = null;
            _pendings.SolveAll(_associations);
            return csNamespace;
        }

        void ConvertPasUse(PasUse pasUse, CsNamespace csNamespace)
        {
            var csUsing = csNamespace.Usings.FirstOrDefault(u => u.Namespace == pasUse.UnitName);
            if (csUsing == null)
                csNamespace.Usings.Add(new CsUsing { Namespace = pasUse.UnitName });
        }

        void ConvertPasDecl(PasDecl pasDecl, CsNamespace csNamespace)
        {
            if (pasDecl is PasEnumTypeDecl)
                ConvertPasEnumTypeDecl(pasDecl as PasEnumTypeDecl, csNamespace);
            else if (pasDecl is PasArrayTypeDecl)
                ConvertPasArrayTypeDecl(pasDecl as PasArrayTypeDecl, csNamespace);
            else if (pasDecl is PasSetTypeDecl)
                ConvertPasSetTypeDecl(pasDecl as PasSetTypeDecl, csNamespace);
            else if (pasDecl is PasRecordTypeDecl)
                ConvertPasRecordTypeDecl(pasDecl as PasRecordTypeDecl, csNamespace);
            else if (pasDecl is PasPointerTypeDecl)
                ConvertPasPointerTypeDecl(pasDecl as PasPointerTypeDecl, csNamespace);
            else if (pasDecl is PasAliasTypeDecl)
                ConvertPasAliasTypeDecl(pasDecl as PasAliasTypeDecl, csNamespace);
            else if (pasDecl is PasStringTypeDecl)
                ConvertPasStringTypeDecl(pasDecl as PasStringTypeDecl, csNamespace);
            else if (pasDecl is PasProcedureTypeDecl)
                ConvertPasProcedureTypeDecl(pasDecl as PasProcedureTypeDecl, csNamespace);
            else if (pasDecl is PasClassTypeDecl)
                ConvertPasClassTypeDecl(pasDecl as PasClassTypeDecl, csNamespace);
            else if (pasDecl is PasMetaclassTypeDecl)
                ConvertPasMetaclassTypeDecl(pasDecl as PasMetaclassTypeDecl, csNamespace);
            else if (pasDecl is PasVarDecl)
                ConvertPasVarDecl(pasDecl as PasVarDecl, csNamespace);
            else if (pasDecl is PasConstDecl)
                ConvertPasConstDecl(pasDecl as PasConstDecl, csNamespace);
            else if (pasDecl is PasProcedureDecl)
                ConvertPasProcedureDecl(pasDecl as PasProcedureDecl, csNamespace);
            else
                throw new Exception(string.Format("Tipo desconhecido: {0}", pasDecl.GetType().Name));
        }

        void ConvertPasMetaclassTypeDecl(PasMetaclassTypeDecl pasMetaclass, CsNamespace csNamespace)
        {
            ///TODO throw new NotImplementedException();
        }

        void ConvertPasEnumTypeDecl(PasEnumTypeDecl pasEnum, CsNamespace csNamespace)
        {
            var csEnum = new CsEnumTypeDecl();
            csEnum.Name = pasEnum.Name;
            foreach (var pasConst in pasEnum.Consts)
            {
                var csConst = new CsEnumConst { Name = pasConst.Name, EnumDomain = csEnum };
                csEnum.Consts.Add(csConst);
                _associations.Assign(pasConst, csConst);
            }
            csNamespace.Decls.Add(csEnum);
            _associations.Assign(pasEnum, csEnum);
        }

        void ConvertPasArrayTypeDecl(PasArrayTypeDecl pasArray, CsNamespace csNamespace)
        {
            /*
            var csArray = new CsClassDomain();
            csArray.Name = pasArray.Name;
            csArray.AncestorName = CsDomainNameOf(pasArray);
            csNamespace.Decls.Add(csArray);
            */
        }

        void ConvertPasSetTypeDecl(PasSetTypeDecl pasSet, CsNamespace csNamespace)
        {
            var csSet = new CsClassTypeDecl();
            csSet.Name = pasSet.Name;
            csSet.AncestorRef = new CsRef { Decl = new CsClassTypeDecl { Name = "Set<" + CsTypeNameOf(pasSet.ItemTypeRef) + ">" } };
            csNamespace.Decls.Add(csSet);
            _associations.Assign(pasSet, csSet);
        }

        void ConvertPasRecordTypeDecl(PasRecordTypeDecl pasRecord, CsNamespace csNamespace)
        {
            var csStruct = new CsStructTypeDecl();
            csStruct.Name = pasRecord.Name;
            foreach (var pasVar in pasRecord.Vars)
                ConvertPasDecl(pasVar, csStruct);
            csNamespace.Decls.Add(csStruct);
            _associations.Assign(pasRecord, csStruct);
        }

        void ConvertPasDecl(PasDecl pasDecl, CsStructTypeDecl csStruct)
        {
            if (pasDecl is PasVarDecl)
                ConvertPasVarDecl(pasDecl as PasVarDecl, csStruct);
            else
                throw new Exception(string.Format("Tipo desconhecido: {0}", pasDecl.GetType().Name));
        }

        string CsTypeNameOf(PasRef pasTypeRef)
        {
            if (pasTypeRef == null)
                return null;
            if (pasTypeRef.Decl != null)
                return CsTypeNameOf(pasTypeRef.Decl as PasTypeDecl);
            return CsTypeNameOf(pasTypeRef.Name);
        }

        string CsTypeNameOf(string pasTypeName)
        {
            if (pasTypeName == null)
                return null;
            else if (pasTypeName.Equals("Integer"))
                return "int";
            else if (pasTypeName.Equals("Boolean"))
                return "bool";
            else if (pasTypeName.Equals("TObject"))
                return "object";
            else
                return pasTypeName;
        }

        string CsTypeNameOf(PasTypeDecl pasType)
        {
            if (pasType == null)
                return null;
            if (pasType is PasStringTypeDecl)
                return "string";
            if (pasType is PasAliasTypeDecl)
                return CsTypeNameOf((pasType as PasAliasTypeDecl).TargetType);
            if (pasType is PasArrayTypeDecl)
                return CsTypeNameOf((pasType as PasArrayTypeDecl).ItemType) + "[]";
            return pasType.Name;
        }

        CsRef CsRefOf(PasRef pasDeclRef)
        {
            if (pasDeclRef == null || pasDeclRef.Decl == null)
                return null;
            var csDeclRef = new CsRef();
            _pendings.Add(csDeclRef, pasDeclRef);
            return csDeclRef;
        }

        CsRef CsRefOf(PasDecl pasDecl)
        {
            if (pasDecl == null)
                return null;
            var csDeclRef = new CsRef();
            _pendings.Add(csDeclRef, new PasRef { Decl = pasDecl });
            return csDeclRef;
        }

        void ConvertPasVarDecl(PasVarDecl pasVar, List<CsStat> csCodes)
        {
            var csVar = new CsLocalVarDecl();
            csVar.Name = pasVar.Name;
            csVar.TypeRef = ConvertPasTypeRef(pasVar.TypeRef);
            csCodes.Add(csVar);
            ///_associations.Assign(pasVar, csVar);
        }

        void ConvertPasVarDecl(PasVarDecl pasVar, CsStructTypeDecl csStruct)
        {
            var csVar = new CsField();
            csVar.Name = pasVar.Name;
            csVar.TypeRef = ConvertPasTypeRef(pasVar.TypeRef);
            csStruct.Decls.Add(csVar);
            _associations.Assign(pasVar, csVar);
        }

        void ConvertPasPointerTypeDecl(PasPointerTypeDecl pasPointer, CsNamespace csNamespace)
        {
            var csAlias = new CsAliasTypeDecl();
            csAlias.Name = pasPointer.Name;
            csAlias.TargetTypeName = CsTypeNameOf(pasPointer.DataDomain);
            csNamespace.Decls.Add(csAlias);
            _associations.Assign(pasPointer, csAlias);
        }

        void ConvertPasStringTypeDecl(PasStringTypeDecl pasString, CsNamespace csNamespace)
        {
            var csAlias = new CsAliasTypeDecl();
            csAlias.Name = pasString.Name;
            csAlias.TargetTypeName = "String";
            csNamespace.Decls.Add(csAlias);
            _associations.Assign(pasString, csAlias);
        }

        void ConvertPasAliasTypeDecl(PasAliasTypeDecl pasAlias, CsNamespace csNamespace)
        {
            var csAlias = new CsAliasTypeDecl();
            csAlias.Name = pasAlias.Name;
            csAlias.TargetTypeName = CsTypeNameOf(pasAlias.TargetType);
            csNamespace.Decls.Add(csAlias);
            _associations.Assign(pasAlias, csAlias);
        }

        void ConvertPasProcedureTypeDecl(PasProcedureTypeDecl pasProcedure, CsNamespace csNamespace)
        {
            var csDelegate = new CsDelegateDomain();
            csDelegate.Name = pasProcedure.Name;
            ///TODO delegate
            csNamespace.Decls.Add(csDelegate);
            _associations.Assign(pasProcedure, csDelegate);
        }

        void ConvertPasClassTypeDecl(PasClassTypeDecl pasClass, CsNamespace csNamespace)
        {
            var csClass = new CsClassTypeDecl();
            csClass.Name = pasClass.Name;
            if (pasClass.Ancestor != null)
                csClass.AncestorRef = ConvertPasTypeRef(pasClass.Ancestor);
            csClass.IsStatic = false;
            foreach (var intf in pasClass.Interfaces)
                csClass.Interfaces.Add(ConvertPasTypeRef(intf));
            foreach (var decl in pasClass.Decls)
                ConvertPasDecl(decl, csClass);
            csNamespace.Decls.Add(csClass);
            _associations.Assign(pasClass, csClass);
        }

        CsClassTypeDecl NamespaceClass(CsNamespace csNamespace)
        {
            var found = csNamespace.Decls.Find(p => p.Name == csNamespace.Name) as CsClassTypeDecl;
            if (found == null)
            {
                found = new CsClassTypeDecl();
                found.Name = csNamespace.Name;
                found.IsStatic = true;
                csNamespace.Decls.Add(found);
            }
            return found;
        }

        void ConvertPasVarDecl(PasVarDecl pasVar, CsNamespace csNamespace)
        {
            ConvertPasVarDecl(pasVar, NamespaceClass(csNamespace), true);
        }

        void ConvertPasConstDecl(PasConstDecl pasConst, CsNamespace csNamespace)
        {
            ConvertPasConstDecl(pasConst, NamespaceClass(csNamespace), true);
        }

        void ConvertPasProcedureDecl(PasProcedureDecl pasProcedure, CsNamespace csNamespace)
        {
            if (!string.IsNullOrEmpty(pasProcedure.InClassName))
            {
                var csClass = csNamespace.Decls.Find(e => e.Name == pasProcedure.InClassName) as CsClassTypeDecl;
                if (csClass == null)
                    throw new Exception(string.Format("Classe não encontrada: {0}", pasProcedure.InClassName));
                ConvertPasProcedureDecl(pasProcedure, csClass, false);
            }
            else
                ConvertPasProcedureDecl(pasProcedure, NamespaceClass(csNamespace), true);
        }

        void ConvertPasDecl(PasDecl pasDecl, CsClassTypeDecl csClass)
        {
            if (pasDecl is PasProcedureDecl)
                ConvertPasProcedureDecl(pasDecl as PasProcedureDecl, csClass, false);
            else if (pasDecl is PasProperty)
                ConvertPasPropertyDecl(csClass, pasDecl as PasProperty, false);
            else if (pasDecl is PasVarDecl)
                ConvertPasVarDecl(pasDecl as PasVarDecl, csClass, false);
            else
                throw new Exception(string.Format("Tipo desconhecido: {0}", pasDecl.GetType().Name));
        }

        CsClassVisibility CsVisibilityOf(PasVisibility pasVisibility)
        {
            switch (pasVisibility)
            {
                case PasVisibility.Default:
                    return CsClassVisibility.Default;
                case PasVisibility.Private:
                    return CsClassVisibility.Private;
                case PasVisibility.Protected:
                    return CsClassVisibility.Protected;
                case PasVisibility.Public:
                    return CsClassVisibility.Public;
                case PasVisibility.Published:
                    return CsClassVisibility.Public;
            }
            return CsClassVisibility.Public;
        }

        void AddDecls(List<CsStat> csCodes, List<PasDecl> pasDecls)
        {
            foreach (var pasDecl in pasDecls)
                ConvertPasDecl(pasDecl, csCodes);
        }

        void ConvertPasDecl(PasDecl pasDecl, List<CsStat> csCodes)
        {
            if (pasDecl is PasVarDecl)
                ConvertPasVarDecl(pasDecl as PasVarDecl, csCodes);
            else if (pasDecl is PasProcedureDecl)
                ConvertPasProcedureDecl(pasDecl as PasProcedureDecl, csCodes);
            else if (pasDecl is PasPointerTypeDecl)
                ConvertPasPointerTypeDecl(pasDecl as PasPointerTypeDecl, _namespace);
            else if (pasDecl is PasArrayTypeDecl)
                ConvertPasArrayTypeDecl(pasDecl as PasArrayTypeDecl, _namespace);
            else
                throw new Exception(string.Format("Tipo desconhecido: {0}", pasDecl.GetType().Name));
        }

        private void ConvertPasProcedureDecl(PasProcedureDecl pasProcedure, List<CsStat> csCodes)
        {
            var csDelegate = new CsDelegateDomain();
            csDelegate.Name = pasProcedure.Name + "Delegate";
            ConvertPasParams(pasProcedure.Params, csDelegate.Params);
            AddDecls(csDelegate.Codes, pasProcedure.Decls);
            AddCodes(csDelegate.Codes, pasProcedure.Codes);
            csDelegate.ReturnType = ConvertPasTypeRef(pasProcedure.ReturnType);
            var csVar = new CsLocalVarDecl();
            csVar.Name = pasProcedure.Name;
            csVar.TypeRef = new CsRef { Decl = csDelegate };
            csCodes.Add(csVar);
            ///_mappings[pasProcedure]= csVar;
        }

        CsMethodDecl convertingMethod;
        void ConvertPasProcedureDecl(PasProcedureDecl pasProcedure, CsClassTypeDecl csClass, bool isStatic)
        {
            /// TODO procurar com mesma lista de parametros
            var csMethod = csClass.Decls.Find(e => e.Name == pasProcedure.Name) as CsMethodDecl;
            if (csMethod != null)
            {
                var saved = convertingMethod;
                convertingMethod = csMethod;
                try
                {
                    AddDecls(csMethod.Codes, pasProcedure.Decls);
                    AddCodes(csMethod.Codes, pasProcedure.Codes);
                }
                finally
                {
                    convertingMethod = saved;
                }
            }
            else
            {
                csMethod = new CsMethodDecl();
                var saved = convertingMethod;
                convertingMethod = csMethod;
                try
                {
                    csMethod.Name = pasProcedure.Name;
                    csMethod.ReturnType = ConvertPasTypeRef(pasProcedure.ReturnType);
                    csMethod.IsConstructor = pasProcedure.Approach == PasProcedureApproach.Constructor;
                    csMethod.IsDestructor = pasProcedure.Approach == PasProcedureApproach.Destructor;
                    csMethod.IsOverride = pasProcedure.IsOverride;
                    csMethod.IsVirtual = pasProcedure.IsVirtual;
                    csMethod.IsStatic = pasProcedure.IsStatic || isStatic;
                    csMethod.IsAbstract = pasProcedure.IsAbstract;
                    csMethod.Visibility = CsVisibilityOf(pasProcedure.Visibility);
                    ConvertPasParams(pasProcedure.Params, csMethod.Params);

                    if (pasProcedure.ReturnType != null)
                        csMethod.Codes.Add(new CsLocalVarDecl { Name = "Result", TypeRef = csMethod.ReturnType });

                    AddDecls(csMethod.Codes, pasProcedure.Decls);
                    AddCodes(csMethod.Codes, pasProcedure.Codes);
                    csClass.Decls.Add(csMethod);
                    _associations.Assign(pasProcedure, csMethod);
                }
                finally
                {
                    convertingMethod = saved;
                }
            }
        }

        void AddCodes(List<CsStat> csCodes, List<PasStat> pasCodes)
        {
            foreach (var pasCode in pasCodes)
                AddCode(csCodes, pasCode);
        }

        void ConvertPasParams(List<PasParamDecl> pasParams, List<CsParamDecl> csParams)
        {
            foreach (var pasParam in pasParams)
                ConvertPasParam(pasParam, csParams);
        }

        CsRef CsAliasType(string typeName)
        {
            return new CsRef { Decl = new CsAliasTypeDecl { Name = typeName } };
        }

        Dictionary<string, string> typeTable = new Dictionary<string, string> { 
        { "integer", "int" }, { "int32", "int" }, { "word", "ushort" }, { "byte", "byte" }, { "cardinal", "uint" },
        { "double", "double" }, { "boolean", "bool" }, { "int64", "long" } };

        CsRef ConvertPasTypeRef(PasRef pasTypeRef)
        {
            if (pasTypeRef == null)
                return null;
            if (pasTypeRef.Decl is PasStringTypeDecl)
                return CsAliasType("String");
            if (pasTypeRef.Decl is PasAliasTypeDecl)
            {
                var pat = pasTypeRef.Decl as PasAliasTypeDecl;
                string eqv;
                if (typeTable.TryGetValue(pat.TargetType.Name.ToLower(), out eqv))
                    return CsAliasType(eqv);
                else
                    return CsAliasType(pat.TargetType.Name);
            }
            return CsRefOf(pasTypeRef);
        }

        void ConvertPasParam(PasParamDecl pasParam, List<CsParamDecl> csParams)
        {
            var csParam = new CsParamDecl();
            csParam.Name = pasParam.Name;
            csParam.TypeRef = ConvertPasTypeRef(pasParam.TypeRef);
            switch (pasParam.Binding)
            {
                case PasParamBinding.Const:
                    csParam.Bind = CsParamBind.Copy;
                    break;
                case PasParamBinding.Copy:
                    csParam.Bind = CsParamBind.Copy;
                    break;
                case PasParamBinding.In:
                    csParam.Bind = CsParamBind.Copy;
                    break;
                case PasParamBinding.Out:
                    csParam.Bind = CsParamBind.Ref;
                    break;
                case PasParamBinding.Var:
                    csParam.Bind = CsParamBind.Ref;
                    break;
                default:
                    csParam.Bind = CsParamBind.Copy;
                    break;
            }
            csParams.Add(csParam);
            _associations.Assign(pasParam, csParam);
        }

        void ConvertPasPropertyDecl(CsClassTypeDecl csClass, PasProperty pasProperty, bool isStatic)
        {
            var csProperty = new CsProperty();
            csProperty.Name = pasProperty.Name;
            csProperty.TypeRef = ConvertPasTypeRef(pasProperty.TypeRef);
            csProperty.IsStatic = isStatic;
            csProperty.Visibility = CsVisibilityOf(pasProperty.Visibility);
            csProperty.ReaderValue = CsValueOf(pasProperty.ReaderValue);
            csProperty.WriterValue = CsValueOf(pasProperty.WriterValue);
            csClass.Decls.Add(csProperty);
            _associations.Assign(pasProperty, csProperty);
        }

        void ConvertPasVarDecl(PasVarDecl pasVar, CsClassTypeDecl csClass, bool isStatic)
        {
            var csField = new CsField();
            csField.Name = pasVar.Name;
            csField.Visibility = CsVisibilityOf(pasVar.Visibility);
            csField.TypeRef = ConvertPasTypeRef(pasVar.TypeRef);
            csField.InitialValue = CsValueOf(pasVar.InitialValue);
            csField.IsStatic = isStatic;
            csClass.Decls.Add(csField);
            _associations.Assign(pasVar, csField);
        }

        void ConvertPasConstDecl(PasConstDecl pasConst, CsClassTypeDecl csClass, bool isStatic)
        {
            var csField = new CsField();
            csField.Name = pasConst.Name;
            csField.Visibility = CsClassVisibility.Public;
            csField.TypeRef = ConvertPasTypeRef(pasConst.TypeRef);
            csField.IsConst = true;
            csField.IsStatic = isStatic;
            csField.InitialValue = CsValueOf(pasConst.Value);
            csClass.Decls.Add(csField);
            _associations.Assign(pasConst, csField);
        }

        void AddCode(List<CsStat> csCodes, PasStat pasStat)
        {
            if (pasStat is PasBegin)
                AddBegin(csCodes, pasStat as PasBegin);
            else if (pasStat is PasIf)
                AddIf(csCodes, pasStat as PasIf);
            else if (pasStat is PasWhile)
                AddWhile(csCodes, pasStat as PasWhile);
            else if (pasStat is PasFor)
                AddFor(csCodes, pasStat as PasFor);
            else if (pasStat is PasRepeat)
                AddRepeat(csCodes, pasStat as PasRepeat);
            else if (pasStat is PasCase)
                AddCase(csCodes, pasStat as PasCase);
            else if (pasStat is PasAssignment)
                AddAssignment(csCodes, pasStat as PasAssignment);
            else if (pasStat is PasTry)
                AddTry(csCodes, pasStat as PasTry);
            else if (pasStat is PasWith)
                AddWith(csCodes, pasStat as PasWith);
            else if (pasStat is PasRaise)
                AddRaise(csCodes, pasStat as PasRaise);
            else if (pasStat is PasCall)
                ConvertPasCall(csCodes, pasStat as PasCall);
            else if (pasStat is PasInherited)
                ConvertPasInherited(csCodes, pasStat as PasInherited);
            else
                throw new Exception(string.Format("Tipo desconhecido: {0}", pasStat.GetType().Name));
        }

        void AddBegin(List<CsStat> csCodes, PasBegin pasBegin)
        {
            var csBegin = new CsBegin();
            AddCodes(csBegin.Codes, pasBegin.Codes);
            csCodes.Add(csBegin);
        }

        void AddIf(List<CsStat> csCodes, PasIf pasIf)
        {
            var csIf = new CsIf();
            csIf.Condition = CsValueOf(pasIf.Condition);
            AddCodes(csIf.TrueCodes, pasIf.TrueCodes);
            AddCodes(csIf.FalseCodes, pasIf.FalseCodes);
            csCodes.Add(csIf);
        }

        void AddWhile(List<CsStat> csCodes, PasWhile pasWhile)
        {
            var csWhile = new CsWhile();
            csWhile.Condition = CsValueOf(pasWhile.Condition);
            AddCodes(csWhile.Codes, pasWhile.Codes);
            csCodes.Add(csWhile);
        }

        CsForStep CsStepOf(PasForStep pasStep)
        {
            switch (pasStep)
            {
                case PasForStep.DownTo:
                    return CsForStep.DownTo;
                case PasForStep.UpTo:
                    return CsForStep.UpTo;
            }
            return CsForStep.UpTo;
        }

        void AddFor(List<CsStat> csCodes, PasFor pasFor)
        {
            var csFor = new CsFor();
            csFor.IteratorName = pasFor.Iterator.StrData;
            csFor.ValueFrom = CsValueOf(pasFor.ValueFrom);
            csFor.ValueTo = CsValueOf(pasFor.ValueTo);
            csFor.Step = CsStepOf(pasFor.Step);
            AddCodes(csFor.Codes, pasFor.Codes);
            csCodes.Add(csFor);
        }

        void AddRepeat(List<CsStat> csCodes, PasRepeat pasRepeat)
        {
            var csRepeat = new CsRepeat();
            csRepeat.ExitCondition = CsValueOf(pasRepeat.ExitCondition);
            AddCodes(csRepeat.Codes, pasRepeat.Codes);
            csCodes.Add(csRepeat);
        }

        void AddCase(List<CsStat> csCodes, PasCase pasCase)
        {
            var csCase = new CsSwitch();
            csCase.SubjectValue = CsValueOf(pasCase.SubjectValue);
            foreach (var item in pasCase.Items)
                AddCaseItem(csCase, item);
            csCodes.Add(csCase);
        }

        void AddCaseItem(CsSwitch csSwitch, PasCaseItem pasItem)
        {
            var csItem = new CsSwitchCase();
            foreach (var val in pasItem.Values)
                csItem.Values.Add(CsValueOf(val));
            AddCodes(csItem.Codes, pasItem.Codes);
            csSwitch.Cases.Add(csItem);
        }

        void AddAssignment(List<CsStat> csCodes, PasAssignment pasAssignment)
        {
            var csAssignment = new CsAssignment();
            csAssignment.LeftSide = CsValueOf(pasAssignment.LeftSide);
            csAssignment.RightSide = CsValueOf(pasAssignment.RightSide);
            csCodes.Add(csAssignment);
        }

        void AddRaise(List<CsStat> csCodes, PasRaise pasRaise)
        {
            var csThrow = new CsThrow();
            csThrow.ExceptionValue = CsValueOf(pasRaise.ExceptionValue);
            csCodes.Add(csThrow);
        }

        int _generator = 0;
        void AddWith(List<CsStat> csCodes, PasWith pasWith)
        {
            var csVar = new CsLocalVarDecl { Name = "with" + ((++_generator).ToString()), TypeRef = new CsRef { Decl = new CsAliasTypeDecl { Name = "string", TargetTypeName = "string" } } };
            csCodes.Add(csVar);
            var csAssignment = new CsAssignment();
            csAssignment.LeftSide = new CsValue { Kind = CsValueKind.Name, StrData = csVar.Name };
            csAssignment.RightSide = CsValueOf(pasWith.SubjectValue);
            csCodes.Add(csAssignment);
            AddCodes(csCodes, pasWith.Codes);
        }

        void AddTry(List<CsStat> csCodes, PasTry pasTry)
        {
            var csTry = new CsTry();
            AddCodes(csTry.Codes, pasTry.Codes);
            foreach (var ex in pasTry.Exceptions)
                AddExcept(csTry, ex);
            csTry.HasExceptionHandler = pasTry.HasExceptionHandler;
            csTry.HasElseExceptionCodes = pasTry.HasElseExceptionCodes;
            csTry.HasUntypedExceptionCodes = pasTry.HasUntypedExceptionCodes;
            AddCodes(csTry.ElseExceptionCodes, pasTry.ElseExceptionCodes);
            AddCodes(csTry.UntypedExceptionCodes, pasTry.UntypedExceptionCodes);
            AddCodes(csTry.FinallyCodes, pasTry.FinallyCodes);
            csCodes.Add(csTry);
        }

        void AddExcept(CsTry csTry, PasExcept pasExcept)
        {
            var csCatch = new CsCatch();
            csCatch.Name = pasExcept.VarName;
            ///csCatch.Type = pasEx.Type;
            AddCodes(csCatch.Codes, pasExcept.Codes);
            csTry.Exceptions.Add(csCatch);
        }

        void ConvertPasCall(List<CsStat> csCodes, PasCall pasCall)
        {
            var csCall = new CsCall();
            csCall.Value = CsValueOf(pasCall.Value);
            csCodes.Add(csCall);
        }

        void ConvertPasInherited(List<CsStat> csCodes, PasInherited pasInherited)
        {
            var csBase = new CsBase();
            csBase.Value = CsValueOf(pasInherited.Value);

            if (convertingMethod.IsConstructor)
                convertingMethod.BaseCall = csBase;
            else if (convertingMethod.IsDestructor)
                convertingMethod.BaseCall = null;
            else
                csCodes.Add(csBase);
        }

        CsValueKind CsValueKindOf(PasValueKind pasKind)
        {
            switch (pasKind)
            {
                case PasValueKind.Parenthesis:
                    return CsValueKind.Parenthesis;
                case PasValueKind.Brackets:
                    return CsValueKind.Brackets;
                case PasValueKind.IntLiteral:
                    return CsValueKind.IntLiteral;
                case PasValueKind.HexLiteral:
                    return CsValueKind.HexLiteral;
                case PasValueKind.StrLiteral:
                    return CsValueKind.StrLiteral;
                case PasValueKind.FloatLiteral:
                    return CsValueKind.FloatLiteral;
                case PasValueKind.Name:
                    return CsValueKind.Name;
                case PasValueKind.Operation:
                    return CsValueKind.Operation;
                case PasValueKind.Params:
                    return CsValueKind.CallParams;
                case PasValueKind.Symbol:
                    return CsValueKind.SpecialSymbol;
                case PasValueKind.DataAt:
                    return CsValueKind.DataAt;
            }
            throw new Exception(string.Format("Tipo desconhecido: {0}", pasKind.ToString()));
        }

        CsValueOperator CsValueOperatorOf(PasValueOperator pasOperator)
        {
            switch (pasOperator)
            {
                case PasValueOperator.None:
                    return CsValueOperator.None;
                case PasValueOperator.Positive:
                    return CsValueOperator.Positive;
                case PasValueOperator.Negative:
                    return CsValueOperator.Negative;
                case PasValueOperator.NotMask:
                    return CsValueOperator.NotMask;
                case PasValueOperator.AndMask:
                    return CsValueOperator.AndMask;
                case PasValueOperator.OrMask:
                    return CsValueOperator.OrMask;
                case PasValueOperator.XorMask:
                    return CsValueOperator.XorMask;
                case PasValueOperator.Concat:
                    return CsValueOperator.Concat;
                case PasValueOperator.Sum:
                    return CsValueOperator.Sum;
                case PasValueOperator.Subtract:
                    return CsValueOperator.Subtract;
                case PasValueOperator.Multiply:
                    return CsValueOperator.Multiply;
                case PasValueOperator.Divide:
                    return CsValueOperator.Divide;
                case PasValueOperator.IntDiv:
                    return CsValueOperator.IntDiv;
                case PasValueOperator.Remainder:
                    return CsValueOperator.Remainder;
                case PasValueOperator.ShiftLeft:
                    return CsValueOperator.ShiftLeft;
                case PasValueOperator.ShiftRight:
                    return CsValueOperator.ShiftRight;
                case PasValueOperator.Equal:
                    return CsValueOperator.Equal;
                case PasValueOperator.Inequal:
                    return CsValueOperator.Inequal;
                case PasValueOperator.Less:
                    return CsValueOperator.Less;
                case PasValueOperator.Greater:
                    return CsValueOperator.Greater;
                case PasValueOperator.NonLess:
                    return CsValueOperator.NonLess;
                case PasValueOperator.NonGreater:
                    return CsValueOperator.NonGreater;
                case PasValueOperator.Not:
                    return CsValueOperator.Not;
                case PasValueOperator.And:
                    return CsValueOperator.And;
                case PasValueOperator.Or:
                    return CsValueOperator.Or;
                case PasValueOperator.Xor:
                    return CsValueOperator.Xor;
                case PasValueOperator.Interval:
                    return CsValueOperator.Interval;
                case PasValueOperator.Union:
                    return CsValueOperator.Union;
                case PasValueOperator.Intersection:
                    return CsValueOperator.Intersection;
                case PasValueOperator.Diference:
                    return CsValueOperator.Diference;
                case PasValueOperator.Belongs:
                    return CsValueOperator.Belongs;
                case PasValueOperator.CastAs:
                    return CsValueOperator.CastAs;
                case PasValueOperator.InstanceOf:
                    return CsValueOperator.InstanceOf;
                case PasValueOperator.AddressOf:
                    return CsValueOperator.AddressOf;
            }
            throw new Exception(string.Format("Tipo desconhecido: {0}", pasOperator.ToString()));
        }

        CsSymbol CsSymbolOf(PasSymbol pasSymbol)
        {
            switch (pasSymbol)
            {
                case PasSymbol.None:
                    return CsSymbol.None;
                case PasSymbol.Nil:
                    return CsSymbol.Nil;
                case PasSymbol.True:
                    return CsSymbol.True;
                case PasSymbol.False:
                    return CsSymbol.False;
                case PasSymbol.Null:
                    return CsSymbol.Null;
            }
            throw new Exception(string.Format("Tipo desconhecido: {0}", pasSymbol.ToString()));
        }

        CsValue CsValueOf(PasValue pasValue) { return CsValueOf(pasValue, null); }

        CsValue CsValueOf(PasValue pasValue, CsValue csNext)
        {
            if (pasValue == null)
                return null;
            var csValue = new CsValue();
            csValue.Kind = CsValueKindOf(pasValue.Kind);
            csValue.FloatData = pasValue.FloatData;
            csValue.IntData = pasValue.IntData;
            csValue.Operator = CsValueOperatorOf(pasValue.Operator);
            csValue.StrData = pasValue.StrData;
            csValue.SymbolData = CsSymbolOf(pasValue.SymbolData);
            foreach (var arg in pasValue.Args)
                csValue.Args.Add(CsValueOf(arg));
            csValue.Next = csNext;
            csValue.Prior = CsValueOf(pasValue.Prior, csValue);
            csValue.TypeRef = ConvertPasTypeRef(pasValue.TypeRef);
            csValue.NameRef = ConvertPasTypeRef(pasValue.DeclRef);
            return csValue;
        }

    }

    public class ElemAssociator
    {
        class MatchPair
        {
            public PasDecl PasElement;
            public CsRef CsEquiv;
        }

        private List<MatchPair> _pairs = new List<MatchPair>();

        public CsRef this[PasDecl pasElement]
        {
            get
            {
                var pair = _pairs.FirstOrDefault(p => p.PasElement == pasElement);
                if (pair != null)
                    return pair.CsEquiv;
                return null;
            }

            set { _pairs.Add(new MatchPair { PasElement = pasElement, CsEquiv = value }); }
        }

        public void Assign(PasDecl pasElement, CsDecl value)
        {
            _pairs.Add(new MatchPair { PasElement = pasElement, CsEquiv = new CsRef { Decl = value } });
        }
    }

    public class PendingSolver
    {
        class Pending
        {
            public CsRef ToSolve;
            public PasRef ToFollow;
        }

        private List<Pending> _pendings = new List<Pending>();

        public void Add(CsRef toSolve, PasRef toFollow)
        {
            _pendings.Add(new Pending { ToSolve = toSolve, ToFollow = toFollow });
        }

        /*
        public void Add(CsRef toSolve, PasDecl toFollow)
        {
            _pendings.Add(new Pending { ToSolve = toSolve, ToFollowDecl = toFollow });
        }
        */
        //

        public void SolveAll(ElemAssociator mappings)
        {
            foreach (var pend in _pendings)
                if (pend.ToFollow != null)
                {
                    var m = mappings[pend.ToFollow.Decl];
                    if (m != null)
                        pend.ToSolve.Decl = m.Decl;
                }
        }
    }

}
