---
title: "BenchmarkDotNet v0.10.10"
date: "2017-11-03"
tags:
- programming
- dotnet
- cs
- BenchmarkDotNet
- benchmarking
aliases:
- /blog/post/bdn-v0_10_10/
---

BenchmarkDotNet v0.10.10 has been released!
This release includes many new features like Disassembly Diagnoser, ParamsSources, .NET Core x86 support, Environment variables, and more!

<!--more-->

### Highlights

There are so many changes in this version!
In this post, I'm going to tell you about the most useful features.

#### Disassembly Diagnoser
This feature by [@adamsitnik](https://github.com/adamsitnik) allows looking at the assembly code of your methods.
It really helps when you investigate some CPU-bound benchmarks.
BenchmarkDotNet not only shows naive disasm, but it also prettify the listing can work with hardware counters: 

{{< img branchpredictor >}}

You can find the full info in the [Disassembling .NET Code with BenchmarkDotNet](http://adamsitnik.com/Disassembly-Diagnoser/) blog post by Adam Sitnik.

It turned out that this feature is useful for comparing assembly for different versions of .NET JIT-compilers.
Check out a recent post from Microsoft .NET Blog by Joseph Tremoulet:
  [RyuJIT Just-in-Time Compiler Optimization Enhancements](https://blogs.msdn.microsoft.com/dotnet/2017/10/16/ryujit-just-in-time-compiler-optimization-enhancements/).
In this post, Joseph compares RyuJIT assembly code for .NET Core 2.0 RTM and .NET Core 2.1.0-preview1-25719-04.

{{< img ryujit >}}

It's really great to see that BenchmarkDotNet helps to develop such products like .NET Core.
By the way, the development process is also useful for BenchmarkDotNet.
Here are some great bug reports by Joseph which were fixed by Adam:
  [#535](https://github.com/dotnet/BenchmarkDotNet/issues/535),
  [#536](https://github.com/dotnet/BenchmarkDotNet/issues/536),
  [#559](https://github.com/dotnet/BenchmarkDotNet/issues/559),
  [#562](https://github.com/dotnet/BenchmarkDotNet/issues/562).

Now BenchmarkDotNet can produce assembly for .NET Framework and .NET Core; Mono support is coming soon.
Of course, we have also many additional feature requests:
  [#543](https://github.com/dotnet/BenchmarkDotNet/issues/543),
  [#544](https://github.com/dotnet/BenchmarkDotNet/issues/544),
  [#545](https://github.com/dotnet/BenchmarkDotNet/issues/545),
  [#546](https://github.com/dotnet/BenchmarkDotNet/issues/546),
  [#560](https://github.com/dotnet/BenchmarkDotNet/issues/560)
  (any help will be highly appreciated).

#### ParamsSources

BenchmarkDotNet allows to parametrize your benchmark and run it against several parameter values.
v0.10.9 supported only a constant set of parameters which should be declared inside the attribute;

```cs
[Params(100, 200)]
public int A { get; set; }

[Params(10, 20)]
public int B { get; set; }
```

It's not always convenient, sometimes we need a way to define parameter values dynamically.
Now you can do it!

```cs
[ParamsSource(nameof(ValuesForA))]
public int A { get; set; } // property with public setter

[ParamsSource(nameof(ValuesForB))]
public int B; // public field

public IEnumerable<int> ValuesForA => new[] { 100, 200 }; // public property

public static IEnumerable<int> ValuesForB() => new[] { 10, 20 }; // public static method
```

See also:
  [#350](https://github.com/dotnet/BenchmarkDotNet/issues/350),
  [#571](https://github.com/dotnet/BenchmarkDotNet/issues/571)
   and [docs](http://benchmarkdotnet.org/Advanced/Params.htm).

#### .NET Core x86 support

Since .NET Core 2.0, we can run it not only on x64, but also on x86.
If we have both versions of runtime installed, we can try the same code on both platforms at the same time.
Here is a config for you:

```cs
public class CustomPathsConfig : ManualConfig
{
    public CustomPathsConfig() 
    {
        var dotnetCli32bit = NetCoreAppSettings
            .NetCoreApp20
            .WithCustomDotNetCliPath(@"C:\Program Files (x86)\dotnet\dotnet.exe", "32 bit cli");

        var dotnetCli64bit = NetCoreAppSettings
            .NetCoreApp20
            .WithCustomDotNetCliPath(@"C:\Program Files\dotnet\dotnet.exe", "64 bit cli");

        Add(Job.RyuJitX86.With(CsProjCoreToolchain.From(dotnetCli32bit)).WithId("32 bit cli"));
        Add(Job.RyuJitX64.With(CsProjCoreToolchain.From(dotnetCli64bit)).WithId("64 bit cli"));
    }
}
```

See also:
  [#310](https://github.com/dotnet/BenchmarkDotNet/issues/310),
  [#534](https://github.com/dotnet/BenchmarkDotNet/issues/534)
   and [docs](http://benchmarkdotnet.org/Configs/Toolchains.htm).

#### Environment variables and Mono args

One of the keys features of BenchmarkDotNet is comparing different environments.
And we add more and more options for you in each release.
Now you can specify Mono arguments:


```cs
public class ConfigWithCustomArguments : ManualConfig
{
    public ConfigWithCustomArguments()
    {
        // --optimize=MODE , -O=mode
        // MODE is a comma separated list of optimizations. They also allow
        // optimizations to be turned off by prefixing the optimization name with a minus sign.

        Add(Job.Mono.With(new[] { new MonoArgument("--optimize=inline") }).WithId("Inlining enabled"));
        Add(Job.Mono.With(new[] { new MonoArgument("--optimize=-inline") }).WithId("Inlining disabled"));
    }
}
```

And environment variables:

```cs
public class ConfigWithCustomEnvVars : ManualConfig
{
    public ConfigWithCustomEnvVars()
    {
        Add(Job.Core.WithId("Inlining enabled"));
        Add(Job.Core.With(
            new[] { new EnvironmentVariable("COMPlus_JitNoInline", "1") })
            .WithId("Inlining disabled"));
    }
}
```

You can find hundreds of runtime knobs [here](https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/clr-configuration-knobs.md).
Now it's so easy to compare performance between different knobs values!

See also: [#262](https://github.com/dotnet/BenchmarkDotNet/issues/262) and [docs](http://benchmarkdotnet.org/Advanced/CustomizingMono.htm).

#### Better environment description

When BenchmarkDotNet prints the summary about benchmarks, it always shows brief information about our environment.
Now we can detect situation when you run a benchmark on a virtual machine (at least, in some cases)
  and print a warning about it
  (thanks [@lukasz-pyrzyk](https://github.com/lukasz-pyrzyk);
   [#167](https://github.com/dotnet/BenchmarkDotNet/issues/167),
   [#527](https://github.com/dotnet/BenchmarkDotNet/pull/527)).
We support detecting `HyperV`, `VirtualBox`, `VMware` (Windows only for now).

Also now we support Windows 10 [1709, Fall Creators Update] and always print the revision number of Windows 10 version
  (like `10.0.15063.674` instead of `10.0.15063`).
  
#### More

Of course, v0.10.10 includes not only new features but also additional sections in the documentation, bug fixes, build script improvements, internal refactoring.
It's not so exciting to write about such stuff in the release notes, but it's also very important changes which improve the overall quality of the library.

### Issues and Pull Requests

In the v0.10.10 scope, **34** issues were resolved and **18** pull requests were merged.
This release includes **95** commits by **12** contributors:
* [@adamsitnik](https://github.com/adamsitnik)
* [@aidmsu](https://github.com/aidmsu)
* [@AndreyAkinshin](https://github.com/AndreyAkinshin)
* [@ig-sinicyn](https://github.com/ig-sinicyn)
* [@ipjohnson](https://github.com/ipjohnson)
* [@jawn](https://github.com/jawn)
* Jiri Cincura
* [@Ky7m](https://github.com/Ky7m)
* [@lukasz-pyrzyk](https://github.com/lukasz-pyrzyk)
* [@pentp](https://github.com/pentp)
* [@rolshevsky](https://github.com/rolshevsky)
* [@Teknikaali](https://github.com/Teknikaali)

Thank you very much!

#### Resolved Issues

* [#160](https://github.com/dotnet/BenchmarkDotNet/issues/160) Make ClrMd Source diagnoser working with new ClrMD api
* [#167](https://github.com/dotnet/BenchmarkDotNet/issues/167) Detect virtual machine environment
* [#262](https://github.com/dotnet/BenchmarkDotNet/issues/262) Runtime knobs
* [#310](https://github.com/dotnet/BenchmarkDotNet/issues/310) Support 32bit benchmarks for .NET Core
* [#350](https://github.com/dotnet/BenchmarkDotNet/issues/350) ParamsSource
* [#437](https://github.com/dotnet/BenchmarkDotNet/issues/437) Add DisassemblyDiagnoser for outputting disassembled JITed code
* [#466](https://github.com/dotnet/BenchmarkDotNet/issues/466) MSBuild parameters are not passed to generated benchmark project
* [#495](https://github.com/dotnet/BenchmarkDotNet/issues/495) Attributes put on base methods are not considered in derived class
* [#500](https://github.com/dotnet/BenchmarkDotNet/issues/500) Borken compilation for net46 projects when .NET Framework 4.7 is installed
* [#505](https://github.com/dotnet/BenchmarkDotNet/issues/505) JsonExporterBase doesn't include MemoryDiagnoser stats in output 
* [#511](https://github.com/dotnet/BenchmarkDotNet/issues/511) [bug] Bug in GetTargetedMatchingMethod() logic
* [#513](https://github.com/dotnet/BenchmarkDotNet/issues/513) IterationSetup not run in Job.InProcess
* [#516](https://github.com/dotnet/BenchmarkDotNet/issues/516) Get a compilation error "CS1009: Unrecognized escape sequence" when using verbatim strings
* [#519](https://github.com/dotnet/BenchmarkDotNet/issues/519) BenchmarkSwitcher.RunAllJoined throws InvalidOperationException
* [#526](https://github.com/dotnet/BenchmarkDotNet/issues/526) Remove project.json support
* [#529](https://github.com/dotnet/BenchmarkDotNet/issues/529) No namespace in export filenames can lead to data loss
* [#530](https://github.com/dotnet/BenchmarkDotNet/issues/530) Build error on Appveyor with recent changes
* [#533](https://github.com/dotnet/BenchmarkDotNet/issues/533) When I clone, build, and run BenchmarkDotNet.Samples I get an error
* [#534](https://github.com/dotnet/BenchmarkDotNet/issues/534) Allow the users to compare 32 vs 64 RyuJit for .NET Core
* [#535](https://github.com/dotnet/BenchmarkDotNet/issues/535) No way to set RuntimeFrameworkVersion in multiple-version config
* [#536](https://github.com/dotnet/BenchmarkDotNet/issues/536) Strange disassembly ordering/truncation
* [#537](https://github.com/dotnet/BenchmarkDotNet/issues/537) Can't benchmark a netstandard2.0 project
* [#538](https://github.com/dotnet/BenchmarkDotNet/issues/538) Duplicate using causing benchmark not to work
* [#539](https://github.com/dotnet/BenchmarkDotNet/issues/539) Target .NET Core 2.0 to take advantage of the new APIs 
* [#540](https://github.com/dotnet/BenchmarkDotNet/issues/540) Artifacts for disassembler projects
* [#542](https://github.com/dotnet/BenchmarkDotNet/issues/542) Problems with Disassembler + Job.Dry
* [#555](https://github.com/dotnet/BenchmarkDotNet/issues/555) Test "CanDisassembleAllMethodCalls" fails on Ubuntu
* [#556](https://github.com/dotnet/BenchmarkDotNet/issues/556) Table in report is broken in VSCode markdown viewer
* [#558](https://github.com/dotnet/BenchmarkDotNet/issues/558) Warn the users when running Benchmarks from xUnit with shadow copy enabled 
* [#559](https://github.com/dotnet/BenchmarkDotNet/issues/559) DissassemblyDiagnoser jit/arch info seems to be wrong
* [#561](https://github.com/dotnet/BenchmarkDotNet/issues/561) Strange behaviour when benchmark project is build in debug mode
* [#562](https://github.com/dotnet/BenchmarkDotNet/issues/562) DisassemblyDiagnoser crashes on overloaded benchmark
* [#564](https://github.com/dotnet/BenchmarkDotNet/issues/564) [Bug] Benchmarking a method doesn't run global setup when filter is applied
* [#571](https://github.com/dotnet/BenchmarkDotNet/issues/571) Allow users to use non compile-time constants as Parameters

#### Merged Pull Requests

* [#507](https://github.com/dotnet/BenchmarkDotNet/pull/507) Fix a typo in Jobs.md 
* [#508](https://github.com/dotnet/BenchmarkDotNet/pull/508) Fixed some typos and grammar
* [#512](https://github.com/dotnet/BenchmarkDotNet/pull/512) Warning about antivirus software after benchmark failure
* [#514](https://github.com/dotnet/BenchmarkDotNet/pull/514) #495 - Unit test for reading attributes from the base class
* [#515](https://github.com/dotnet/BenchmarkDotNet/pull/515) Fix #513 - IterationSetup not run in Job.InProcess
* [#518](https://github.com/dotnet/BenchmarkDotNet/pull/518) Fixed information about MemoryDiagnoser
* [#520](https://github.com/dotnet/BenchmarkDotNet/pull/520) XML Exporter documentation and samples
* [#525](https://github.com/dotnet/BenchmarkDotNet/pull/525) adding validator for setup cleanup attributes
* [#527](https://github.com/dotnet/BenchmarkDotNet/pull/527) Detecting virtual machine hypervisor, #167
* [#531](https://github.com/dotnet/BenchmarkDotNet/pull/531) Remove --no-build argument for dotnet test & pack commands
* [#532](https://github.com/dotnet/BenchmarkDotNet/pull/532) Fix type of local in EmitInvokeMultipleBody
* [#547](https://github.com/dotnet/BenchmarkDotNet/pull/547) Fix markdown headers
* [#548](https://github.com/dotnet/BenchmarkDotNet/pull/548) Fix condition in package reference list and update dotnet cli version from 1.0.4 to 2.0.0 for non-Windows system
* [#549](https://github.com/dotnet/BenchmarkDotNet/pull/549) Project files cleanup
* [#552](https://github.com/dotnet/BenchmarkDotNet/pull/552) Fix exporters to use fully qualified filenames
* [#563](https://github.com/dotnet/BenchmarkDotNet/pull/563) Remove leading space character in a MD table row, #556
* [#565](https://github.com/dotnet/BenchmarkDotNet/pull/565) Single point of full config creation 
* [#569](https://github.com/dotnet/BenchmarkDotNet/pull/569) Update cakebuild scripts

### Links

* [BenchmarkDotNet on GitHub](https://github.com/dotnet/BenchmarkDotNet)
* [Official documentation](http://benchmarkdotnet.org/)
* [ChangeLog](https://github.com/dotnet/BenchmarkDotNet/wiki/ChangeLog)
* [v0.10.10 milestone](https://github.com/dotnet/BenchmarkDotNet/issues?q=milestone:v0.10.10)
* [v0.10.10 commits](https://github.com/dotnet/BenchmarkDotNet/compare/v0.10.9...v0.10.10)

