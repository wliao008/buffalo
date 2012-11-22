using Buffalo.Common;
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
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(new FileInfo(AssemblyPath).Directory.FullName);
            var parameters = new ReaderParameters
            {
                AssemblyResolver = resolver,
            };
            this.AssemblyDefinition = AssemblyDefinition.ReadAssembly(AssemblyPath, parameters);
            //populate the type definition first
            foreach (var m in this.AssemblyDefinition.Modules)
                m.Types
                    .ToList().ForEach(x => this.TypeDefinitions.Add(x));
            //What if aspects are defined in a different assembly?
            this.TypeDefinitions.AddRange(this.FindAspectTypeDefinition());
            //extract aspects from the type definitions
            this.TypeDefinitions
                .Where(x => x.BaseType != null 
                    && (x.BaseType.FullName == typeof(MethodBoundaryAspect).FullName 
                    || x.BaseType.FullName == typeof(MethodAroundAspect).FullName))
                .ToList()
                .ForEach(x => Aspects.Add(new Aspect { Name = x.FullName, TypeDefinition = x }));
            //set the original types
            SetUnderlyingAspectTypes(AssemblyPath);
            Aspects.ForEach(x =>
            {
                x.AssemblyLevelStatus = this.CheckAspectStatus(this.AssemblyDefinition, x);
            });

            //var classdll = AssemblyDefinition.ReadAssembly(Aspects[0].TypeDefinition.BaseType.Module.FullyQualifiedName);
            //var aspectclass = classdll.MainModule.GetType(Aspects[0].Name);
            //var aspector = aspectclass.Methods.First(m => m.IsConstructor);
            //var ctoref = this.AssemblyDefinition.MainModule.Import(aspector);
            //this.AssemblyDefinition.MainModule.Types.Add(new TypeDefinition("", Aspects[0].Name, aspectclass.Attributes));

            //finally, get the eligible methods
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
            var tdefs = new List<TypeDefinition>();
            foreach (var ca in this.AssemblyDefinition.CustomAttributes)
            {
                var car = ca.AttributeType.Resolve();
                if (car.BaseType.FullName == typeof(MethodBoundaryAspect).FullName
                    || car.BaseType.FullName == typeof(MethodAroundAspect).FullName)
                {
                    tdefs.Add(car);
                }
            }

            //loop thru the custom attributes of each type, resolve them to find aspects
            foreach (var type in types)
            {
                if (type.CustomAttributes.Count == 0) continue;
                var cas = type.CustomAttributes;
                foreach (var ca in cas)
                {
                    var car = ca.AttributeType.Resolve();
                    if (car.BaseType.FullName == typeof(MethodBoundaryAspect).FullName
                        || car.BaseType.FullName == typeof(MethodAroundAspect).FullName)
                    {
                        if(!tdefs.Contains(car))
                            tdefs.Add(car);
                    }
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
                {
                    Console.WriteLine("\t" + a.Name);
                }
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

                if (aspect.Type != null 
                    && (aspect.Type.BaseType == typeof(MethodBoundaryAspect)
                    || aspect.Type.BaseType == typeof(MethodAroundAspect))
                    && def.CustomAttributes[i].AttributeType.FullName.Equals(aspect.Name))
                {
                    attrFound = true;
                    if (def.CustomAttributes[i].Properties.Count == 0)
                    {
                        status = Enums.Status.Applied;
                    }
                    else
                    {
                        var exclude = def.CustomAttributes[i].Properties.First(x => x.Name == "AttributeExclude");
                        if ((bool)exclude.Argument.Value == true)
                        {
                            status = Enums.Status.Excluded;
                            //Console.WriteLine(def.CustomAttributes[i].AttributeType.Name + " removed");
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
                MethodReference attrCtor = this.AssemblyDefinition.MainModule.Import(
                    aspect.Type.GetConstructor(Type.EmptyTypes));
                //var methodRef = this.AssemblyDefinition.MainModule.Import(aspect.Type);
                
                def.CustomAttributes.Add(new CustomAttribute(attrCtor));
                //Console.WriteLine("Injecting custome attr for: " + def.ToString());
            }

            return status;
        }

        internal static void SetUnderlyingAspectTypes(string assemblyPath)
        {
            ///TODO: This block would not be needed if I can get the underlying types
            ///directly from cecil
            AppDomain domain = AppDomain.CreateDomain("domain" + DateTime.Now.Ticks);
            BoundaryObject boundary = (BoundaryObject)
                domain.CreateInstanceAndUnwrap(
                typeof(BoundaryObject).Assembly.FullName,
                typeof(BoundaryObject).FullName);
            AppDomainArgs ada = new AppDomainArgs();
            ada.AssemblyPath = assemblyPath;
            ada.Aspects = Aspects;
            ada.UnderlyingAspectTypes = UnderlyingAspectTypes;
            BoundaryObject.SetTypes(ada, assemblyPath);
            domain.DomainUnload += new EventHandler(domain_DomainUnload);
            AppDomain.Unload(domain);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static void domain_DomainUnload(object sender, EventArgs e)
        {
            Console.WriteLine("Unloading domain...");
        }
    }

    class BoundaryObject : MarshalByRefObject
    {
        public static void SetTypes(AppDomainArgs ada, string path)
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
                else
                {
                    //aspect.Type = dll2.FirstOrDefault(x => x.FullName.Equals(aspect.TypeDefinition.FullName));
                    Weaver.SetUnderlyingAspectTypes(aspect.TypeDefinition.BaseType.Module.FullyQualifiedName);
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