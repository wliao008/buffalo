Buffalo
====
Buffalo is an aspect oriented framework using [Mono Cecil] (http://www.mono-project.com/Cecil). It is attribute based, usage is very simple.

Sample Usage
====
To use Buffalo, first add the reference to the Buffalo.dll and BuffaloAOP.exe

Right click on the project property, go to Build Events, paste the following into "Post-build event command line":
```
"$(TargetDir)BuffaloAOP.exe" "$(TargetPath)"
```

The above will cause BuffaloAOP to inject aspect into the project assembly. Hopefully this manual step will not be needed in the later version once it is hooked into MSBuild.

Then create an aspect as follow:

```csharp
public class TraceAspect : MethodBoundaryAspect
{
    public override void Before(MethodArgs args)
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