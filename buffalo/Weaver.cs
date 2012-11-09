using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reflection = System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
//using Mono.Cecil.Rocks;
using System.Collections.Specialized;
using System.Text;

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

        internal void Inject(string outPath)
        {
            var injectors = new List<IInjectable>();

            //apply the around aspect if necessary
            var aroundAspectExist = this.EligibleMethods.Values.Any(x => x.Any(y => y.Type.BaseType == typeof(MethodAroundAspect)));
            if (aroundAspectExist)
                injectors.Add(new MethodAroundInjector());

            //apply the boundary aspect if necessary
            var boundaryAspectExist = this.EligibleMethods.Values.Any(x => x.Any(y => y.Type.BaseType == typeof(MethodBoundaryAspect)));
            if (boundaryAspectExist)
                injectors.Add(new MethodBoundaryInjector());

            //inject
            injectors.ForEach(x => x.Inject(this.AssemblyDefinition, this.EligibleMethods));

            //write out the modified assembly
            this.AssemblyDefinition.Write(outPath);
            Console.WriteLine("DONE");
        }

        private void Init()
        {
            //initialize the variables
            Aspects = new List<Aspect>();
            NewMethodNames = new StringCollection();
            this.TypeDefinitions = new List<TypeDefinition>();
            this.EligibleMethods = new Dictionary<MethodDefinition, List<Aspect>>();
            this.AssemblyDefinition = AssemblyDefinition.ReadAssembly(AssemblyPath);
            //populate the type definition first
            foreach (var m in this.AssemblyDefinition.Modules)
                m.Types.ToList().ForEach(x => this.TypeDefinitions.Add(x));
            //extract aspects from the type definitions
            this.TypeDefinitions
                .Where(x => x.BaseType != null 
                    && (x.BaseType.FullName == typeof(MethodBoundaryAspect).FullName 
                    || x.BaseType.FullName == typeof(MethodAroundAspect).FullName))
                .ToList()
                .ForEach(x => Aspects.Add(new Aspect { Name = x.FullName, TypeDefinition = x }));
            //set the original types
            SetUnderlyingAspectTypes();
            Aspects.ForEach(x =>
            {
                x.AssemblyLevelStatus = this.CheckAspectStatus(this.AssemblyDefinition, x);
            });
            //finally, get the eligible methods
            Aspects
                .Where(x => x.AssemblyLevelStatus != Buffalo.Enums.Status.Excluded)
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

        private void InjectAroundAspect()
        {
            var once = false;
            var ems = this.EligibleMethods.ToList();
            var eligibleAroundMethods = ems.Where(x => x.Value.Any(y => y.Type.BaseType == typeof(MethodAroundAspect)));
            foreach (var d in eligibleAroundMethods)
            {
                var method = d.Key;
                var aspects = d.Value;

                if (!method.ReturnType.FullName.Equals("System.Void"))
                {
#if DEBUG
                    Console.WriteLine("Around aspect cannot be applied to: \n{0}\nit only applies to void return type.", method.FullName);
#endif
                    continue;
                }

                var il = method.Body.GetILProcessor();

                //create a replacement function
                var methodName = string.Format("{0}{1}", method.Name, DateTime.Now.Ticks);
                var aroundAspect = aspects.SingleOrDefault(x => x.Type.BaseType == typeof(MethodAroundAspect));
                var aroundMethod = aroundAspect.TypeDefinition.Methods.SingleOrDefault(x => x.FullName.Contains("Invoke(Buffalo.MethodArgs)"));
                var instProceed = aroundMethod.Body.Instructions.FirstOrDefault(x => x.ToString().Contains("callvirt System.Void Buffalo.MethodArgs::Proceed()"));
                //TypeReference voidref = this.AssemblyDefinition.MainModule.Import(typeof(void));
                MethodDefinition newmethod = new MethodDefinition(methodName, method.Attributes, method.ReturnType);
                //newmethod.Body.SimplifyMacros();

                if (!once)
                {
                    //copy the variables in aspect to the target type, this should happen only once?
                    aroundAspect.TypeDefinition.Fields.ToList()
                        .ForEach(x =>
                        {
                            //var constructorInfo = typeof(Instruction).GetConstructor(Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance, null, new[] { typeof(OpCode), typeof(object) }, null);
                            //var newInstruction = (Instruction)constructorInfo.Invoke(new[] { x.OpCode, instruction.Operand });
                            //var fieldDefinition = newInstruction.Operand as FieldDefinition;
                            //if (newInstruction.Operand is TypeReference)
                            //{
                            //    this.AssemblyDefinition.MainModule.Import(newInstruction.Operand as TypeReference);
                            //}

                            var fd = new FieldDefinition(x.Name, x.Attributes, x.FieldType);                            
                            method.DeclaringType.Fields.Add(fd);
                        });

                    once = true;
                }


                method.Parameters.ToList().ForEach(x => newmethod.Parameters.Add(new ParameterDefinition(x.ParameterType)));
                aroundMethod.Body.Variables.ToList().ForEach(x => newmethod.Body.Variables.Add(new VariableDefinition(x.VariableType)));
                
                aroundMethod.Body.Instructions.ToList().ForEach(instruction =>
                {
                    var constructorInfo = typeof(Instruction).GetConstructor(Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance, null, new[] { typeof(OpCode), typeof(object) }, null);
                    var newInstruction = (Instruction)constructorInfo.Invoke(new[] { instruction.OpCode, instruction.Operand});
                    var fieldDefinition = newInstruction.Operand as FieldDefinition;
                    if (fieldDefinition != null)
                    {
                        this.AssemblyDefinition.MainModule.Import(fieldDefinition.FieldType);
                        newInstruction.Operand = method.DeclaringType.Fields.First(x => x.Name == fieldDefinition.Name);
                    }

                    if (newInstruction.Operand is MethodReference)
                    {
                        //Try really hard to import type
                        var methodReference = (MethodReference)newInstruction.Operand;

                        this.AssemblyDefinition.MainModule.Import(methodReference.MethodReturnType.ReturnType);
                        this.AssemblyDefinition.MainModule.Import(methodReference.DeclaringType);
                        this.AssemblyDefinition.MainModule.Import(methodReference);
                    }
                    if (newInstruction.Operand is TypeReference)
                    {
                        this.AssemblyDefinition.MainModule.Import(newInstruction.Operand as TypeReference);
                    }
                    newmethod.Body.Instructions.Add(newInstruction);
                });
                NewMethodNames.Add(methodName);
                method.DeclaringType.Methods.Add(newmethod);

                var invokeMethod = aroundAspect.TypeDefinition.Methods.SingleOrDefault(x => x.FullName.Contains("Invoke(Buffalo.MethodArgs)"));
                int i = 0;
                bool found = false;
                for (i = 0; i < invokeMethod.Body.Instructions.Count; ++i)
                {
                    if (invokeMethod.Body.Instructions[i].ToString()
                        .Contains("callvirt System.Void Buffalo.MethodArgs::Proceed()"))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    //make a call to the original method
                    var ldarg0 = Instruction.Create(OpCodes.Ldarg_0);
                    newmethod.Body.Instructions[i - 1] = ldarg0;
                    newmethod.Body.Instructions[i] = Instruction.Create(OpCodes.Call, method);
                    int count = i;
                    if (method.Parameters.Count > 0)
                    {
                        for (int j = 0; j < method.Parameters.Count; ++j)
                        {
                            var ins = Instruction.Create(OpCodes.Ldarg, method.Parameters[j]);
                            newmethod.Body.Instructions.Insert(count++, ins);
                        }
                    }
                    //newmethod.Body.OptimizeMacros();
                }

                //finally, all calls to the original methods should be changed to the newly
                //generated method
                foreach (var type in this.AssemblyDefinition.MainModule.Types)
                {
                    foreach (var m in type.Methods.Where(x => !NewMethodNames.Contains(x.Name)))
                    {
                        for (int j = 0; j < m.Body.Instructions.Count; ++j)
                        {
                            if (m.Body.Instructions[j].ToString().Contains(method.FullName))
                            {
                                //m.Body.Instructions[j].OpCode = OpCodes.Call;
                                m.Body.Instructions[j].Operand = newmethod;
                            }
                        }
                    }
                }
            }
        }
        
        private static void SetUnderlyingAspectTypes()
        {
            ///TODO: This block would not be needed if I can get the underlying types
            ///directly from cecil
            AppDomain domain = AppDomain.CreateDomain("domain");
            BoundaryObject boundary = (BoundaryObject)
                domain.CreateInstanceAndUnwrap(
                typeof(BoundaryObject).Assembly.FullName,
                typeof(BoundaryObject).FullName);
            AppDomainArgs ada = new AppDomainArgs();
            ada.AssemblyPath = AssemblyPath;
            ada.Aspects = Aspects;
            ada.UnderlyingAspectTypes = UnderlyingAspectTypes;
            //domain.DoCallBack(new CrossAppDomainDelegate(LoadAssembly));
            BoundaryObject.DoSomething(ada, AssemblyPath);
            domain.DomainUnload += new EventHandler(domain_DomainUnload);
            AppDomain.Unload(domain);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static void domain_DomainUnload(object sender, EventArgs e)
        {
            Console.WriteLine("Unloading domain...");
        }

        /*
        private static void LoadAssembly()
        {
            ///TODO: need to pass vars to and from appdomains: http://stackoverflow.com/a/1250847/150607
            var _assemblyPath = @"C:\Users\wliao\Documents\Visual Studio 2010\Projects\buffalo\client\bin\Debug\client.exe";
            var assembly = System.Reflection.Assembly.LoadFrom(_assemblyPath);
            var types = assembly.GetTypes().ToList();
        }
        */

        private void PrintEligibleMethods()
        {
            foreach (var de in this.EligibleMethods)
            {
                Console.WriteLine(de.Key.FullName);
                foreach (var a in de.Value)
                {
                    Console.WriteLine("\t" + a.Name);
                }
            }
        }

        [Obsolete]
        private void CheckEligibleMethods()
        {
            Aspects.ForEach(x => this.CheckEligibleMethods(x));
        }

        private void CheckEligibleMethods(Aspect aspect)
        {
            foreach (var t in this.TypeDefinitions.Where(x => !x.Name.Equals("<Module>")))
            {
                var status = this.CheckAspectStatus(t, aspect);
#if DEBUG
                Console.WriteLine("\t{0}: {1}", t.Name, status.ToString());
#endif
                //if (status == Buffalo.Enums.Status.Excluded)
                //    continue;

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

        private List<MethodDefinition> GetMethodDefinitions(TypeDefinition typeDef, Buffalo.Enums.Status typeStatus, Aspect aspect)
        {
            if (typeDef.Name.Contains("Test"))
            {
                System.Diagnostics.Debug.WriteLine("Test");
            }

            var list = new List<MethodDefinition>();
            foreach (var method in typeDef.Methods)
            {
                var status = this.CheckAspectStatus(method, aspect);
                //only add methods that are not excluded
                //if (status != Status.Applied && typeStatus != Status.Applied)
                //    continue;

                if (status == Buffalo.Enums.Status.Applied)
                {
                    list.Add(method);
                }
                else
                {
                    if (typeStatus == Buffalo.Enums.Status.Applied && status != Buffalo.Enums.Status.Excluded)
                    {
                        status = Buffalo.Enums.Status.Applied;
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
        private Buffalo.Enums.Status CheckAspectStatus(ICustomAttributeProvider def, Aspect aspect)
        {
            Buffalo.Enums.Status status = aspect.AssemblyLevelStatus;

            bool attrFound = false;
            for (int i = 0; i < def.CustomAttributes.Count; ++i)
            {
                if (def.CustomAttributes[i].AttributeType.FullName.Equals("System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
                {
                    status = Enums.Status.Excluded;
                    break;
                }

                if (aspect.Type != null 
                    && (aspect.Type.BaseType == typeof(MethodBoundaryAspect)
                    || aspect.Type.BaseType == typeof(MethodAroundAspect))
                    && def.CustomAttributes[i].AttributeType.FullName.Equals(aspect.Name))
                {
                    attrFound = true;
                    if (def.CustomAttributes[i].Properties.Count == 0)
                    {
                        status = Buffalo.Enums.Status.Applied;
                    }
                    else
                    {
                        var exclude = def.CustomAttributes[i].Properties.First(x => x.Name == "AttributeExclude");
                        if ((bool)exclude.Argument.Value == true)
                        {
                            status = Buffalo.Enums.Status.Excluded;
                            //Console.WriteLine(def.CustomAttributes[i].AttributeType.Name + " removed");
                            def.CustomAttributes.RemoveAt(i);
                        }
                    }
                }
            }

            if (!attrFound && aspect.AssemblyLevelStatus == Buffalo.Enums.Status.Applied)
            {
                //this aspect is applied on the assembly level and
                //as a result the type and method might not have the
                //attributed annotated, this is to programmatically add
                //in the annotation so IL can be generated correctly.
                MethodReference attrCtor = this.AssemblyDefinition.MainModule.Import(
                    aspect.Type.GetConstructor(Type.EmptyTypes));
                //var methodRef = this.AssemblyDefinition.MainModule.Import(aspect.Type);
                
                def.CustomAttributes.Add(new CustomAttribute(attrCtor));
                //Console.WriteLine("Injecting custome attr for: " + def.ToString());
            }

            return status;
        }
    }

    class BoundaryObject : MarshalByRefObject
    {
        public static void DoSomething(AppDomainArgs ada, string path)
        {
            ///TODO: need to pass vars to and from appdomains: http://stackoverflow.com/a/1250847/150607
            var _assemblyPath = path;
            var assembly = System.Reflection.Assembly.LoadFrom(_assemblyPath);
            var types = assembly.GetTypes().ToList();

            foreach (var aspect in ada.Aspects)
            {
                var type = types.FirstOrDefault(x => x.FullName.Equals(aspect.TypeDefinition.FullName));
                if (type != null)
                {
                    aspect.Type = type;
                }
            }
        }
    }

    class AppDomainArgs : MarshalByRefObject
    {
        internal string AssemblyPath { get; set; }

        internal List<Aspect> Aspects { get; set; }

        internal Dictionary<string, Type> UnderlyingAspectTypes { get; set; }
    }
}