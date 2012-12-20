Buffalo
====
Buffalo is an [aspect oriented programming] (http://en.wikipedia.org/wiki/Aspect-oriented_programming) framework using [Mono Cecil] (http://www.mono-project.com/Cecil) for the .NET platform. Buffalo is attribute based, so you can just utilize your existing .NET skill; so the learning curve is low to get started.

Sample Usage
====
To use Buffalo, first add the reference to the Buffalo.dll and BuffaloAOP.exe

Right click on the project property, go to Build Events, paste the following into "Post-build event command line":
```
"$(TargetDir)BuffaloAOP.exe" "$(TargetPath)"
```

The above will cause BuffaloAOP to inject aspect into the project assembly. Hopefully this manual step will not be needed in the later version once it is hooked into MSBuild.

Let us create create an aspect that will log various point of method execution, as follow:

```csharp
public class TraceAspect : MethodBoundaryAspect
{
    public override void OnBefore(MethodArgs args)
    {
        //do something before the execution of a method
        Console.WriteLine(args.Name + " is about to execute");
    }
}
```

Suppose I have the following simple console program:
```csharp
class Program
{
    static void Main(string[] args)
    {
        var t = new Test();
        var result = t.Add(1,4);
        Console.WriteLine(result);
    }
}

public class Test
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}
```
will produce the output:
```
5
```
I can now apply the trace aspect as follow:
```csharp
[Trace]
public int Add(int a, int b)
{
    return a + b;
}    
```
When ran, it will produce the following output:
```
Add is about to execute
5
```
More Advanced Usage
====
You can also use Buffalo to completely replace a method. Take the Add() method from above as an example, if you want to double the parameter value (for whatever reason), you can do something like follow:

```csharp
public class DoubleAdd : MethodAroundAspect
{
    public override object Invoke(MethodArgs args)
    {
        for (int i = 0; i < args.ParameterArray.Length; ++i )
        {
            args.ParameterArray[i] = (int)args.ParameterArray[i] * 2;
        }

        return args.Proceed();
    }
}
```
Note that DoubleAdd inherit from MethodAroundAspect, and override Invoke(). It doubles all the parameter value, then calls args.Proceed() to call into the original method. You can apply the aspect:
```csharp
[DoubleAdd]
public int Add(int a, int b)
{
    return a + b;
}    
```
When ran:
```
var result = t.Add(1,4);
```

it will produce the following output:
```
10
```