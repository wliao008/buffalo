﻿using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace Buffalo
{
    public class Weaver
    {
        public Weaver(string assemblyPath)
        {
            ///TODO: Maybe just don't do anything if file not found
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException();

            this.AssemblyPath = assemblyPath;
        }

        public string AssemblyPath { get; set; }

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