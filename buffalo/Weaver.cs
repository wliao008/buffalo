using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using System;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace Buffalo
{
    public class Weaver
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

        public void Inject(string outPath)
        {
            //var methodBoundaryAspectDef = this.AssemblyDefinition.MainModule.Import(typeof(MethodBoundaryAspect)).Resolve();
            //var aspects = this.FindMethodBoundaryAspect();
            //var beforeMethod = methodBoundaryType.Methods.First(x => x.Name == "Before");
            //var befores = aspects.Select(x => x.Methods.First(y => y.Name == "Before")).ToList();
            
            //foreach (var method in this.EligibleMethods)
            //{
            //    if (method.Key.FullName.Contains("Function2a"))
            //    {
            //        System.Diagnostics.Debug.WriteLine("Function2a");
            //    }
            //    var aspects = method.Value;
            //    var insts = method.Key.Body.Instructions;
            //    insts.Insert(0, Instruction.Create(OpCodes.Nop));
            //    for (int i = 0, j = 0; i <= aspects.Count; i += 2, ++j)
            //    {
            //        var before = aspects[j].TypeDefinition.Methods.First(x => x.Name.Equals("Before"));
            //        insts.Insert(i + 1, Instruction.Create(OpCodes.Ldarg_0));
            //        insts.Insert(i + 2, Instruction.Create(OpCodes.Call, before));
            //    }
            //}

            this.InjectPeps();
            //write out the modified assembly
            this.AssemblyDefinition.Write(outPath);
            Console.WriteLine("DONE");
        }

        private void Init()
        {
            //initialize the variables
            Aspects = new List<Aspect>();
            this.TypeDefinitions = new List<TypeDefinition>();
            this.EligibleMethods = new Dictionary<MethodDefinition, List<Aspect>>();
            this.AssemblyDefinition = AssemblyDefinition.ReadAssembly(AssemblyPath);
            //populate the type definition first
            foreach (var m in this.AssemblyDefinition.Modules)
                m.Types.ToList().ForEach(x => this.TypeDefinitions.Add(x));
            //extract aspects from the type definitions
            this.TypeDefinitions
                .Where(x => x.BaseType != null 
                    && x.BaseType.FullName == typeof(MethodBoundaryAspect).FullName)
                .ToList()
                .ForEach(x => Aspects.Add(new Aspect { Name = x.FullName, TypeDefinition = x }));
            //set the original types
            SetUnderlyingAspectTypes();
            //check each aspect if it's applied on the assembly level
            //foreach (var ca in this.AssemblyDefinition.CustomAttributes)
            //{
            //    var t = ca.AttributeType.Resolve();
            //    if (t.BaseType.FullName.Equals(typeof(MethodBoundaryAspect).FullName))
            //    {
            //        var aspect = Aspects.First(x => x.Name.Equals(ca.AttributeType.FullName));
            //        aspect.IsAssemblyLevel = true;
            //    }
            //}
            Aspects.ForEach(x =>
            {
                x.AssemblyLevelStatus = this.CheckAspectStatus(this.AssemblyDefinition, x);
            });
            //finally, get the eligible methods
            Aspects
                .Where(x => x.AssemblyLevelStatus != Status.Excluded)
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

        private void InjectPeps()
        {
            //assuming the aspects are all PEPS
            foreach (var d in this.EligibleMethods)
            {
                var method = d.Key;
                var aspects = d.Value;
                var count = method.Body.Instructions.Count;
                var il = method.Body.GetILProcessor();
                //var write = il.Create(OpCodes.Call, ModuleDefinition.Import(typeof(Console).GetMethod("WriteLine", new[] { typeof(object) })));
                Instruction write = null;
                MethodReference exception = aspects[0].TypeDefinition.Methods.FirstOrDefault(x => x.Name.Equals("Exception"));
                if (exception == null)
                {
                    exception = this.AssemblyDefinition.MainModule.Import(typeof(MethodBoundaryAspect).GetMethod("Exception"));
                }

                write = il.Create(OpCodes.Call, exception);
                var ret = il.Create(OpCodes.Ret);
                var pop = il.Create(OpCodes.Pop);
                var leave = il.Create(OpCodes.Leave, ret);
                var last = method.Body.Instructions.Last();
                method.Body.Instructions[count - 1] =
                    Instruction.Create(OpCodes.Leave_S, ret);

                var in1 = il.Create(OpCodes.Stloc_2);
                var in2 = il.Create(OpCodes.Nop);
                var in3 = il.Create(OpCodes.Ldarg_0);
                var in4 = il.Create(OpCodes.Ldloc_2);
                var in5 = il.Create(OpCodes.Nop);
                var in6 = il.Create(OpCodes.Nop);

                il.InsertAfter(method.Body.Instructions.Last(), write);
                il.InsertAfter(write, leave);
                il.InsertAfter(leave, ret);

                var handler = new ExceptionHandler(ExceptionHandlerType.Catch)
                {
                    TryStart = method.Body.Instructions.First(),
                    TryEnd = write,
                    HandlerStart = write,
                    HandlerEnd = ret,
                    CatchType = this.AssemblyDefinition.MainModule.Import(typeof(Exception)),
                };
                method.Body.ExceptionHandlers.Add(handler);
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
            BoundaryObject.DoSomething(ada);
            domain.DomainUnload += new EventHandler(domain_DomainUnload);
            AppDomain.Unload(domain);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static void domain_DomainUnload(object sender, EventArgs e)
        {
            Console.WriteLine("Unloading domain...");
        }

        private static void LoadAssembly()
        {
            ///TODO: need to pass vars to and from appdomains: http://stackoverflow.com/a/1250847/150607
            var _assemblyPath = @"C:\Users\wliao\Documents\Visual Studio 2010\Projects\buffalo\client\bin\Debug\client.exe";
            var assembly = System.Reflection.Assembly.LoadFrom(_assemblyPath);
            var types = assembly.GetTypes().ToList();
            
            //foreach (var aspect in ada.Aspects)
            //{
            //    var type = types.FirstOrDefault(x => x.FullName.Equals(aspect.TypeDefinition.FullName));
            //    if (type != null)
            //    {
            //        aspect.Type = type;
            //    }
            //}

            //foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            //{
            //    Console.WriteLine(a.FullName);
            //    Console.WriteLine("");
            //}
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
                if (status == Status.Excluded)
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

        private List<MethodDefinition> GetMethodDefinitions(TypeDefinition typeDef, Status typeStatus, Aspect aspect)
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

                if (status == Status.Applied)
                {
                    list.Add(method);
                }
                else
                {
                    if (typeStatus == Status.Applied && status != Status.Excluded)
                    {
                        status = Status.Applied;
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
        private Status CheckAspectStatus(ICustomAttributeProvider def, Aspect aspect)
        {
            Status status = aspect.AssemblyLevelStatus;

            bool attrFound = false;
            for (int i = 0; i < def.CustomAttributes.Count; ++i)
            {
                //var t = def.CustomAttributes[i].AttributeType.Resolve();
                if (aspect.Type != null 
                    && aspect.Type.BaseType == typeof(MethodBoundaryAspect)
                    && def.CustomAttributes[i].AttributeType.FullName.Equals(aspect.Name))
                {
                    attrFound = true;
                    if (def.CustomAttributes[i].Properties.Count == 0)
                    {
                        status = Status.Applied;
                    }
                    else
                    {
                        var exclude = def.CustomAttributes[i].Properties.First(x => x.Name == "AttributeExclude");
                        if ((bool)exclude.Argument.Value == true)
                        {
                            status = Status.Excluded;
                            //Console.WriteLine(def.CustomAttributes[i].AttributeType.Name + " removed");
                            def.CustomAttributes.RemoveAt(i);
                        }
                    }
                }
            }

            if (!attrFound && aspect.AssemblyLevelStatus == Status.Applied)
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

        private void AddAttribute(ICustomAttributeProvider def, Aspect aspect)
        {
        }

        private void SetTypes()
        {
            //byte[] asmBytes = File.ReadAllBytes(this.AssemblyPath);
            //var domain = AppDomain.CreateDomain("aspects");
            //var assembly = domain.Load(asmBytes);
            //var assembly = domain.Load(System.Reflection.AssemblyName.GetAssemblyName(this.AssemblyPath));

            //AppDomain domain = AppDomain.CreateDomain("aspects");
            //domain.AssemblyResolve += (s, e) =>
            //{
            //    Console.WriteLine("Resolving...");
            //    return domain.Load(System.Reflection.AssemblyName.GetAssemblyName(this.AssemblyPath));
            //};

            //System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFrom(this.AssemblyPath);
            //var domain = AppDomain.CreateDomain("aspects");
            //domain.DoCallBack(new CrossAppDomainDelegate(this.LoadAssembly));
            //domain.DomainUnload += (s, e) =>
            //{
            //    Console.WriteLine("Unloading temp app domain");
            //};
            //AppDomain.Unload(domain);
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
        }

        /*
        public List<MethodInfo> GetAllMethods()
        {
            List<MethodInfo> ret = new List<MethodInfo>();
            Assembly assembly = Assembly.LoadFrom(this.AssemblyPath);

            MethodBoundaryAspect MyAttribute =
            (MethodBoundaryAspect)Attribute.GetCustomAttribute(assembly, typeof(MethodBoundaryAspect));
            if (MyAttribute == null)
            {
                Console.WriteLine("Aspect not applied to namespace");
            }
            else
            {
                Console.WriteLine("Aspect applied to namespace");
            }

            var namespaces = assembly.GetCustomAttributes(typeof(MethodBoundaryAspect), false);
            if (namespaces.Count() > 0)
            {
                //aspect applied on assembly, should get all methods
                var tmptypes = assembly.GetTypes().ToList();
                List<Type> types = new List<Type>();
                tmptypes.ForEach(x =>
                {
                    if (!this.Exclude(x))
                    {
                        types.Add(x);
                    }
                });

                var methods = types
                              .SelectMany(t => t.GetMethods())
                              //.Where(m => m.GetCustomAttributes(typeof(Aspect), false).Length > 0)
                              .ToArray();

                foreach (var m in methods)
                {
                    if (!this.Exclude(m))
                    {
                        ret.Add(m);
                    }
                }
            }

            return ret;
        }

        private List<MethodInfo> GetMethodsFromType(Type type)
        {
            return type.GetMethods().ToList();
        }

        private bool Exclude(MethodInfo method)
        {
            var attrs = method.GetCustomAttributesData();
            return this.Exclude(attrs);
        }

        private bool Exclude(Type type)
        {
            var attrs = type.GetCustomAttributesData();
            return this.Exclude(attrs);
        }

        private bool Exclude(IList<CustomAttributeData> attrs)
        {
            foreach (var a in attrs)
            {
                foreach (var arg in a.NamedArguments)
                {
                    if (arg.MemberInfo.Name.Equals("AttributeExclude"))
                    {
                        return (bool)arg.TypedValue.Value;
                    }
                }
            }

            return false;
        }
        */
    }

    class BoundaryObject : MarshalByRefObject
    {
        public static void DoSomething(AppDomainArgs ada)
        {
            ///TODO: need to pass vars to and from appdomains: http://stackoverflow.com/a/1250847/150607
            var _assemblyPath = @"C:\Users\wliao\Documents\Visual Studio 2010\Projects\buffalo\client\bin\Debug\client.exe";
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

/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace SimpleAop
{
    class Program
    {
        static void Main(string[] args)
        {
            new Weaver();
        }
    }

    class Weaver
    {
        private TypeReference helloType;
        private MethodReference preMethod;
        private MethodReference catchExceptionMethod;
        public ModuleDefinition ModuleDefinition { get; set; }
        public Weaver()
        {
            this.Init();
        }

        private void Init()
        {
            this.ModuleDefinition = ModuleDefinition.ReadModule(@"C:\Users\wliao\Documents\Visual Studio 2010\Projects\Tests\SimpleHello\SimpleHello\bin\Debug\SimpleHello.exe");
            helloType = ModuleDefinition.Import(typeof(SimpleHello.Hello));
            var helloDefinition = helloType.Resolve();
            preMethod = ModuleDefinition.Import(helloDefinition.Methods.First(x => x.Name == "Pre"));
            catchExceptionMethod = ModuleDefinition.Import(helloDefinition.Methods.First(x => x.Name == "CatchException"));
            var md = GetSay();
            Inject(md);
            var sd3 = GetSimpleDivWith4();
            Inject2(sd3);
            ModuleDefinition.Write(@"C:\Users\wliao\Documents\Visual Studio 2010\Projects\Tests\SimpleAop\SimpleAop\bin\Debug\Modified.exe");
        }

        private void Inject(MethodDefinition method)
        {
            var instructions = method.Body.Instructions;
            instructions.Insert(0, Instruction.Create(OpCodes.Nop));
            instructions.Insert(1, Instruction.Create(OpCodes.Ldarg_0));
            instructions.Insert(2, Instruction.Create(OpCodes.Call, preMethod));
        }

        private void Inject2(MethodDefinition method)
        {
            var il = method.Body.GetILProcessor();
            var write = il.Create(OpCodes.Call, ModuleDefinition.Import (typeof (Console).GetMethod ("WriteLine", new [] { typeof (object)})));
            //var write = il.Create(OpCodes.Call, catchExceptionMethod);
            var ret = il.Create(OpCodes.Ret);
            var pop = il.Create(OpCodes.Pop);
            var leave = il.Create(OpCodes.Leave, ret);
            var last = method.Body.Instructions.Last();
            method.Body.Instructions[method.Body.Instructions.Count - 1] = Instruction.Create(OpCodes.Leave_S, ret);
            //il.InsertBefore(method.Body.Instructions.Last(), il.Create(OpCodes.Pop));

            var in1 = il.Create(OpCodes.Stloc_2);
            var in2 = il.Create(OpCodes.Nop);
            var in3 = il.Create(OpCodes.Ldarg_0);
            var in4 = il.Create(OpCodes.Ldloc_2);
            var in5 = il.Create(OpCodes.Nop);
            var in6 = il.Create(OpCodes.Nop);

            //IL_000d: stloc.2
            //IL_000e: nop
            //IL_000f: ldarg.0
            //IL_0010: ldloc.2

            //il.InsertBefore(method.Body.Instructions.Last(), il.Create(OpCodes.Nop));
            //il.InsertAfter(method.Body.Instructions.Last(), in1);
            //il.InsertAfter(in1, in2);
            //il.InsertAfter(in2, in3);
            //il.InsertAfter(in3, in4);
            //il.InsertAfter(in4, write);

            //il.InsertBefore(write.Previous.Previous, il.Create(OpCodes.Ldarg_0));
            //il.InsertBefore(write.Previous.Previous.Previous, il.Create(OpCodes.Nop));
            //il.InsertBefore(write.Previous.Previous.Previous.Previous, il.Create(OpCodes.Stloc_2));

            //IL_0016: nop
            //IL_0017: nop
            //
            //il.InsertAfter(write, in5);
            //il.InsertAfter(in5, in6);

            il.InsertAfter(method.Body.Instructions.Last(), write);
            il.InsertAfter(write, leave);
            il.InsertAfter(leave, ret);
            //il.InsertAfter(pop, ret);

            var handler = new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                TryStart = method.Body.Instructions.First(),
                TryEnd = write,
                HandlerStart = write,
                HandlerEnd = ret,
                CatchType = ModuleDefinition.Import(typeof(Exception)),
            };
            method.Body.ExceptionHandlers.Add(handler);
        }

        private MethodDefinition GetSay()
        {
            foreach (TypeDefinition ty in ModuleDefinition.Types)
            {
                foreach (MethodDefinition md in ty.Methods)
                {
                    if (md.Name == "Say")
                    {
                        return md;
                    }
                }
            }

            return null;
        }

        private MethodDefinition GetSimpleDivWith4()
        {
            foreach (TypeDefinition ty in ModuleDefinition.Types)
            {
                foreach (MethodDefinition md in ty.Methods)
                {
                    if (md.Name == "SimpleDivWith4")
                    {
                        return md;
                    }
                }
            }

            return null;
        }
    }
}
*/