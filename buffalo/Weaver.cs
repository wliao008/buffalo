﻿using Buffalo.Common;
using Buffalo.Extensions;
using Buffalo.Injectors;
using Buffalo.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
//using Mono.Cecil.Rocks;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Reflection = System.Reflection;
using Mono.Collections.Generic;

namespace Buffalo
{
    internal class Weaver
    {
        public Weaver(string assemblyPath)
        {
            ///TODO: Maybe just don't do anything if file not found
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException();

            AssemblyPath = assemblyPath;
            this.Init();
        }

        static internal string AssemblyPath { get; set; }

        static internal List<Aspect> Aspects { get; set; }

        static internal Dictionary<string, Type> UnderlyingAspectTypes { get; set; }

        internal Dictionary<MethodDefinition, List<Aspect>> EligibleMethods { get; set; }

        internal List<TypeDefinition> TypeDefinitions { get; set; }

        internal AssemblyDefinition AssemblyDefinition { get; set; }

        internal StringCollection NewMethodNames { get; set; }

        internal void Inject_BAK(string outPath)
        {
            var injectors = new List<IInjectable>();

            //apply the around aspect if necessary
            var aroundAspectExist = this.EligibleMethods.Values.Any(x => 
                x.Any(y => y.BuffaloAspect == Enums.BuffaloAspect.MethodAroundAspect));
            if (aroundAspectExist)
                injectors.Add(new MethodAroundInjector());

            //apply the boundary aspect if necessary
            var boundaryAspectExist = this.EligibleMethods.Values.Any(x =>
                x.Any(y => y.BuffaloAspect == Enums.BuffaloAspect.MethodBoundaryAspect));
            if (boundaryAspectExist)
                injectors.Add(new MethodBoundaryInjector());

            //inject
            injectors.ForEach(x => x.Inject(this.AssemblyDefinition, this.EligibleMethods));

            //write out the modified assembly
            this.AssemblyDefinition.Write(outPath);
            Console.WriteLine("DONE");
        }

        internal void Inject(string outPath)
        {
            var ems = this.EligibleMethods.ToList();
            var eligibleBoundaryMethods = ems.Where(x => x.Value.Any(y =>
                y.BuffaloAspect == Enums.BuffaloAspect.MethodBoundaryAspect));

            foreach (var d in eligibleBoundaryMethods)
            {
                var method = d.Key;
                var aspects = d.Value;
                var il = method.Body.GetILProcessor();
                //var maInstructions = new List<Instruction>();
                var aspectVarInstructions = new List<Instruction>();
                var beforeInstructions = new List<Instruction>();
                var var = method.AddMethodArgsVariable(this.AssemblyDefinition);
                for (int i = 0; i < aspects.Count; ++i)
                {
                    var varAspectName = "asp" + System.DateTime.Now.Ticks;
                    var varAspectRef = this.AssemblyDefinition.MainModule.Import(aspects[i].TypeDefinition);
                    var varAspect = new VariableDefinition(varAspectName, varAspectRef);
                    method.Body.Variables.Add(varAspect);
                    var varAspectIdx = method.Body.Variables.Count - 1;
                    var ctor = aspects[i].TypeDefinition.Methods.First(x => x.IsConstructor);
                    var ctoref = this.AssemblyDefinition.MainModule.Import(ctor);
                    aspectVarInstructions.Add(Instruction.Create(OpCodes.Newobj, ctoref));
                    aspectVarInstructions.Add(Instruction.Create(OpCodes.Stloc, varAspect));
                    var before = method.FindMethodReference(aspects[i], Enums.AspectType.OnBefore);
                    if (before != null)
                    {
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                        var aspectBefore = aspects[i].TypeDefinition.Methods.FirstOrDefault(
                            x => x.Name == Enums.AspectType.OnBefore.ToString());
                        var aspectBeforeRef = this.AssemblyDefinition.MainModule.Import(aspectBefore);
                        beforeInstructions.Add(Instruction.Create(OpCodes.Callvirt, aspectBeforeRef));
                    }
                }

                int varIdx = var.VarIdx;
                //maInstructions.ForEach(x => method.Body.Instructions.Insert(varIdx++, x));
                aspectVarInstructions.ForEach(x => method.Body.Instructions.Insert(varIdx++, x));

                int beforeIdx = varIdx;
                //perform this only if user overrides Before() in the aspect
                if (beforeInstructions.Count > 0)
                {
                    beforeInstructions.ForEach(x => method.Body.Instructions.Insert(beforeIdx++, x));
                }
            }

            this.AssemblyDefinition.Write(outPath);
        }

        private void Init()
        {
            //initialize the variables
            Aspects = new List<Aspect>();
            NewMethodNames = new StringCollection();
            this.TypeDefinitions = new List<TypeDefinition>();
            this.EligibleMethods = new Dictionary<MethodDefinition, List<Aspect>>();
            //set the resolver in case assembly is in different directory
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(new FileInfo(AssemblyPath).Directory.FullName);
            var parameters = new ReaderParameters { AssemblyResolver = resolver };
            this.AssemblyDefinition = AssemblyDefinition.ReadAssembly(AssemblyPath, parameters);
            //populate the type definition first
            foreach (var m in this.AssemblyDefinition.Modules)
                m.Types.ToList().ForEach(x => this.TypeDefinitions.Add(x));
            //if aspects are defined in a different assembly?
            var typedefs = this.FindAspectTypeDefinition();
            this.TypeDefinitions = this.TypeDefinitions.Union(typedefs).ToList();
            //extract aspects from the type definitions
            this.TypeDefinitions
                .Where(x => x.BaseType != null)
                .ToList()
                .ForEach(x =>
                {
                    Buffalo.Common.Enums.BuffaloAspect? ba = null;
                    if (x.BaseType.FullName == typeof(MethodBoundaryAspect).FullName)
                        ba = Enums.BuffaloAspect.MethodBoundaryAspect;
                    else if (x.BaseType.FullName == typeof(MethodAroundAspect).FullName)
                        ba = Enums.BuffaloAspect.MethodAroundAspect;
                    if(ba.HasValue)
                        Aspects.Add(new Aspect { Name = x.FullName, TypeDefinition = x, BuffaloAspect = ba.Value });
                });
            Aspects.ForEach(x => x.AssemblyLevelStatus = this.CheckAspectStatus(this.AssemblyDefinition, x));
            //finally, get all the eligible methods for each aspect
            Aspects
                .Where(x => x.AssemblyLevelStatus != Enums.Status.Excluded)
                .ToList()
                .ForEach(x =>
                {
#if DEBUG
                    Console.WriteLine("Aspect {0}: {1}", x.Name, x.AssemblyLevelStatus.ToString());
                    Console.WriteLine("============================================");
#endif
                    this.CheckEligibleMethods(x);
                    Console.WriteLine("");
                });
        }

        private List<TypeDefinition> FindAspectTypeDefinition()
        {
            //look for aspect in this assembly, if aspect is defined in a different
            //assembly, import it here.
            var types = this.AssemblyDefinition.MainModule.Types;
            var tdefs = this.FindAspectsFromAttributes(this.AssemblyDefinition.CustomAttributes);

            //loop thru the custom attributes of each type, resolve them to find aspects
            foreach (var type in types)
            {
                //if (type.CustomAttributes.Count == 0) continue;
                var tmp = this.FindAspectsFromAttributes(type.CustomAttributes);
                tdefs.AddRange(tmp);
                foreach (var m in type.Methods)
                {
                    tdefs.AddRange(this.FindAspectsFromAttributes(m.CustomAttributes));
                }
            }

            return tdefs;
        }

        private List<TypeDefinition> FindAspectsFromAttributes(Collection<CustomAttribute> customAttributes)
        {
            var tdefs = new List<TypeDefinition>();
            foreach (var ca in customAttributes)
            {
                var car = ca.AttributeType.Resolve();
                if (car.BaseType.FullName == typeof(MethodBoundaryAspect).FullName
                    || car.BaseType.FullName == typeof(MethodAroundAspect).FullName)
                {
                    if (!tdefs.Contains(car))
                        tdefs.Add(car);
                }
            }
            return tdefs;
        }

        private void PrintEligibleMethods()
        {
            foreach (var de in this.EligibleMethods)
            {
                Console.WriteLine(de.Key.FullName);
                foreach (var a in de.Value)
                    Console.WriteLine("\t" + a.Name);
            }
        }

        private void CheckEligibleMethods(Aspect aspect)
        {
            foreach (var t in this.TypeDefinitions.Where(x => !x.Name.Equals("<Module>")
                && (x.BaseType == null || (x.BaseType.FullName != typeof(MethodBoundaryAspect).FullName
                    && x.BaseType.FullName != typeof(MethodAroundAspect).FullName))))
            {
                var status = this.CheckAspectStatus(t, aspect);
#if DEBUG
                Console.WriteLine("\t{0}: {1}", t.Name, status.ToString());
#endif
                if (status == Enums.Status.Excluded)
                    continue;

                var mths = this.GetMethodDefinitions(t, status, aspect);
                mths.ForEach(x =>
                {
                    if (!this.EligibleMethods.ContainsKey(x))
                    {
                        this.EligibleMethods.Add(x, new List<Aspect>() { aspect });
                    }
                    else
                    {
                        var aspects = this.EligibleMethods[x];
                        aspects.Add(aspect);
                    }
                });
            }
        }

        private List<MethodDefinition> GetMethodDefinitions(TypeDefinition typeDef, Enums.Status typeStatus, Aspect aspect)
        {
            var list = new List<MethodDefinition>();
            foreach (var method in typeDef.Methods)
            {
                var status = this.CheckAspectStatus(method, aspect);
                if (status == Enums.Status.Applied)
                {
                    list.Add(method);
                }
                else
                {
                    if (typeStatus == Enums.Status.Applied && status != Enums.Status.Excluded)
                    {
                        status = Enums.Status.Applied;
                        list.Add(method);
                    }
                }
#if DEBUG
                Console.WriteLine("\t\t{0}: {1}", method.Name, status.ToString());
#endif
            }

            return list;
        }

        /// <summary>
        /// A TypeDefinition and MethodDefinition both implement the
        /// ICustomAttributeProvider interface, so it can be used here
        /// to determined if a method is marked as exclude or not.
        /// </summary>
        private Enums.Status CheckAspectStatus(ICustomAttributeProvider def, Aspect aspect)
        {
            Enums.Status status = aspect.AssemblyLevelStatus;

            bool attrFound = false;
            for (int i = 0; i < def.CustomAttributes.Count; ++i)
            {
                if (def.CustomAttributes[i].AttributeType.FullName.Equals("System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
                {
                    status = Enums.Status.Excluded;
                    break;
                }

                if (aspect.TypeDefinition != null
                    && (aspect.BuffaloAspect == Enums.BuffaloAspect.MethodBoundaryAspect
                    || aspect.BuffaloAspect == Enums.BuffaloAspect.MethodAroundAspect)
                    && def.CustomAttributes[i].AttributeType.FullName.Equals(aspect.Name))
                {
                    attrFound = true;
                    if (def.CustomAttributes[i].Properties.Count == 0)
                    {
                        status = Enums.Status.Applied;
                    }
                    else
                    {
                        var exclude = def.CustomAttributes[i].Properties.FirstOrDefault(x => x.Name == "AttributeExclude");
                        if (exclude.Argument.Value != null && (bool)exclude.Argument.Value == true)
                        {
                            status = Enums.Status.Excluded;
                            def.CustomAttributes.RemoveAt(i);
                        }
                    }
                }
            }

            if (!attrFound && aspect.AssemblyLevelStatus == Enums.Status.Applied)
            {
                //this aspect is applied on the assembly level and
                //as a result the type and method might not have the
                //attributed annotated, this is to programmatically add
                //in the annotation so IL can be generated correctly.
                var ctor = aspect.TypeDefinition.Methods.First(x => x.IsConstructor);
                var ctoref = this.AssemblyDefinition.MainModule.Import(ctor);
                def.CustomAttributes.Add(new CustomAttribute(ctoref));
            }

            return status;
        }
    }
}