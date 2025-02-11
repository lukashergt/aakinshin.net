---
title: "BenchmarkDotNet v0.10.12"
date: "2018-01-15"
tags:
- programming
- dotnet
- cs
- BenchmarkDotNet
- benchmarking
aliases:
- /blog/post/bdn-v0_10_12
---

BenchmarkDotNet v0.10.12 has been released! This release includes:

* **Improved DisassemblyDiagnoser:**
  BenchmarkDotNet contains an embedded disassembler so that it can print assembly code for all benchmarks;
  it's not easy, but the disassembler evolves in every release.
* **Improved MemoryDiagnoser:**
  it has a better precision level, and it takes less time to evaluate memory allocations in a benchmark.
* **New TailCallDiagnoser:**
  now you get notifications when JIT applies the tail call optimizations to your methods.
* **Better environment info:**
  when your share performance results, it's very important to share information about your environment.
  The library generates the environment summary for you by default.
  Now it contains information about the amount of physical CPU, physical cores, and logic cores.
  If you run a benchmark on a virtual machine, you will get the name of the hypervisor
  (e.g., Hyper-V, VMware, or VirtualBox).
* **Better summary table:**
  one of the greatest features of BenchmarkDotNet is the summary table.
  It shows all important information about results in a compact and understandable form.
  Now it has better customization options: you can display relative performance of different environments
  (e.g., compare .NET Framework and .NET Core) and group benchmarks by categories.
* **New GC settings:** now we support `NoAffinitize`, `HeapAffinitizeMask`, `HeapCount`.
* Other minor improvements and bug fixes

<!--more-->

### Diagnosers

Diagnosers are helpers which print additional information about your benchmarks.

#### Improved DisassemblyDiagnoser

`DisassemblyDiagnoser` prints an assembly listing for your source code.
We already had this feature, but we continue to improve it.
Our goal is not just to provide a raw info about your code, but provide a comfortable way to explore the program internals.
In v0.10.12, Adam Sitnik ([@adamsitnik](https://github.com/adamsitnik)) [implemented](https://github.com/dotnet/BenchmarkDotNet/issues/546)
  advanced support of labels for jump targets.  

* When user hovers over a label, the mouse cursor changes to pointer and label get's highlighted
* When user clicks a label, all usages gets highlighted
* When user presses F3, we jump to next usage of given label

Demo:

{{< img disasm >}}

#### Improved MemoryDiagnoser

`MemoryDiagnoser` show the memory traffic for each benchmark and the GC collection count for each generation.
In this release, we [improved](https://github.com/dotnet/BenchmarkDotNet/issues/606) accuracy and
 reduce the total time which you should spend to get the results.

#### New TailCallDiagnoser

[@GeorgePlotnikov](https://github.com/GeorgePlotnikov) implemented `TailCallDiagnoser` which
  detects [tail call](https://en.wikipedia.org/wiki/Tail_call) optimizations and prints information about it.
This feature should be useful for F# developers.
Currently, it has some restrictions: it works only for x64 programs, and it's Windows-only.

Demo:

```cs
[Diagnostics.Windows.Configs.TailCallDiagnoser]
[LegacyJitX86Job, LegacyJitX64Job, RyuJitX64Job]
public class Jit_TailCalling
{
    [Benchmark]
    public long Calc()
        => FactorialWithoutTailing(7) - FactorialWithTailing(7);

    private static long FactorialWithoutTailing(int depth)
        => depth == 0 ? 1 : depth * FactorialWithoutTailing(depth - 1);

    private static long FactorialWithTailing(int pos, int depth)
        => pos == 0 ? depth : FactorialWithTailing(pos - 1, depth * pos);

    private static long FactorialWithTailing(int depth)
        => FactorialWithTailing(1, depth);
}
```

TailCallDiagnosers prints the following lines:

```txt
// * Diagnostic Output - TailCallDiagnoser *
--------------------

--------------------
Jit_TailCalling.Calc: LegacyJitX64(Jit=LegacyJit, Platform=X64, Runtime=Clr)
--------------------

--------------------
Jit_TailCalling.Calc: LegacyJitX86(Jit=LegacyJit, Platform=X86, Runtime=Clr)
--------------------

--------------------
Jit_TailCalling.Calc: RyuJitX64(Jit=RyuJit, Platform=X64)
--------------------
Caller: <null>.<null> - <null>
Callee: BenchmarkDotNet.Samples.JIT.Jit_TailCalling.FactorialWithTailing - int64  (int32,int32)
Tail prefix: False
Tail call type: RecursiveLoop
-------------------
```

### Better environment info

One of the most important parts of any performance report is the environment information.
People should understand what kind of machine did you use for your benchmarks.

Irina Ananyeva ([@morgan-kn](https://github.com/morgan-kn)) [implemented](https://github.com/dotnet/BenchmarkDotNet/issues/582) a cool feature
  which displays the amount of physical CPU, logical cores, and physical cores (an example: `1 CPU, 8 logical cores and 4 physical cores`).
Now the environment info section looks like this (it works on Windows/Linux/macOS; .NET Framework/.NET Core/Mono):

```md
BenchmarkDotNet=v0.10.12, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.192)
Intel Core i7-6700HQ CPU 2.60GHz (Skylake), 1 CPU, 8 logical cores and 4 physical cores
Frequency=2531249 Hz, Resolution=395.0619 ns, Timer=TSC
.NET Core SDK=2.0.3
  [Host] : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT
  Clr    : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2600.0
  Core   : .NET Core 2.0.3 (Framework 4.6.25815.02), 64bit RyuJIT
  Mono   : Mono 5.4.0 (Visual Studio), 64bit
```

Some people run benchmarks on virtual machines instead of real hardware,
  and it's also an important fact.
With a new feature by Łukasz Pyrzyk ([@lukasz-pyrzyk](https://github.com/lukasz-pyrzyk)),
  a special label (like `VM=VirtualBox`) will be automatically added to the result.

### Better summary table

The summary table tries to help you understand performance data in a quick way.
In the old versions of BenchmarkDotNet, you can mark a method as a baseline and get "scaled" performance values for all other methods.
In v0.10.12 (thanks Marc Gravell ([@mgravell](https://github.com/mgravell)) for the [idea](https://github.com/mgravell))),
  you can introduce several baselines in a class (if you are using the benchmark categories) or
  mark a job as a baseline (it allows evaluating the relative performance of different environments).

Let's look at a few examples.

**Example 1: Methods**

You can mark a method as a baseline with the help of `[Benchmark(Baseline = true)]`.

```cs
public class Sleeps
{
    [Benchmark]
    public void Time50() => Thread.Sleep(50);

    [Benchmark(Baseline = true)]
    public void Time100() => Thread.Sleep(100);

    [Benchmark]
    public void Time150() => Thread.Sleep(150);
}
```

As a result, you will have additional `Scaled` column in the summary table:

```md
|  Method |      Mean |     Error |    StdDev | Scaled |
|-------- |----------:|----------:|----------:|-------:|
|  Time50 |  50.46 ms | 0.0779 ms | 0.0729 ms |   0.50 |
| Time100 | 100.39 ms | 0.0762 ms | 0.0713 ms |   1.00 |
| Time150 | 150.48 ms | 0.0986 ms | 0.0922 ms |   1.50 |
```
 
**Example 2: Methods with categories**

The only way to have several baselines in the same class is to separate them by categories.
  and mark the class with `[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]`.

```cs
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Sleeps
{
    [BenchmarkCategory("Fast"), Benchmark(Baseline = true)]        
    public void Time50() => Thread.Sleep(50);

    [BenchmarkCategory("Fast"), Benchmark]
    public void Time100() => Thread.Sleep(100);
    
    [BenchmarkCategory("Slow"), Benchmark(Baseline = true)]        
    public void Time550() => Thread.Sleep(550);

    [BenchmarkCategory("Slow"), Benchmark]
    public void Time600() => Thread.Sleep(600);
}
```

Results:

```md
|  Method | Categories |      Mean |     Error |    StdDev | Scaled |
|-------- |----------- |----------:|----------:|----------:|-------:|
|  Time50 |       Fast |  50.46 ms | 0.0745 ms | 0.0697 ms |   1.00 |
| Time100 |       Fast | 100.47 ms | 0.0955 ms | 0.0893 ms |   1.99 |
|         |            |           |           |           |        |
| Time550 |       Slow | 550.48 ms | 0.0525 ms | 0.0492 ms |   1.00 |
| Time600 |       Slow | 600.45 ms | 0.0396 ms | 0.0331 ms |   1.09 |
```

**Example 3: Jobs**

If you want to compare several runtime configurations,
  you can mark one of your jobs with `isBaseline = true`.

```cs
[ClrJob(isBaseline: true)]
[MonoJob]
[CoreJob]
public class RuntimeCompetition
{
    [Benchmark]
    public int SplitJoin() => string.Join(",", new string[1000]).Split(',').Length;
}
```

Results:

```md
    Method | Runtime |     Mean |     Error |    StdDev | Scaled | ScaledSD |
---------- |-------- |---------:|----------:|----------:|-------:|---------:|
 SplitJoin |     Clr | 19.42 us | 0.2447 us | 0.1910 us |   1.00 |     0.00 |
 SplitJoin |    Core | 13.00 us | 0.2183 us | 0.1935 us |   0.67 |     0.01 |
 SplitJoin |    Mono | 39.14 us | 0.7763 us | 1.3596 us |   2.02 |     0.07 |
 ```

### New GC Settings

BenchmarkDotNet allows configuring GC Settings for each job.
Now we [support](https://github.com/dotnet/BenchmarkDotNet/issues/622) a few additional settings:
  `NoAffinitize`, `HeapAffinitizeMask`, `HeapCount`.
If you set them, the library generates `app.config` like this:

```xml
<configuration>
   <runtime>
      <GCHeapCount enabled="6"/>
      <GCNoAffinitize enabled="true"/>
      <GCHeapAffinitizeMask enabled="144"/>
   </runtime>
</configuration>
```

See the [MSDN page](https://support.microsoft.com/en-us/help/4014604/may-2017-description-of-the-quality-rollup-for-the-net-framework-4-6-4) for details.

### Milestone details

In the [v0.10.12](https://github.com/dotnet/BenchmarkDotNet/issues?q=milestone:v0.10.12) scope, 
14 issues were resolved and 10 pull requests where merged.
This release includes 42 commits by 9 contributors.

#### Resolved issues (14)

* [#273](https://github.com/dotnet/BenchmarkDotNet/issues/273) Create a tail call diagnoser
* [#543](https://github.com/dotnet/BenchmarkDotNet/issues/543) Run Disassembly Diagnoser without extra run (assignee: [@adamsitnik](https://github.com/adamsitnik))
* [#546](https://github.com/dotnet/BenchmarkDotNet/issues/546) Synthesizing labels for jump targets (assignee: [@adamsitnik](https://github.com/adamsitnik))
* [#574](https://github.com/dotnet/BenchmarkDotNet/issues/574) Display VM hypervisor in summary section (assignee: [@lukasz-pyrzyk](https://github.com/lukasz-pyrzyk))
* [#582](https://github.com/dotnet/BenchmarkDotNet/issues/582) Print amount of logical and physical core (assignee: [@morgan-kn](https://github.com/morgan-kn))
* [#599](https://github.com/dotnet/BenchmarkDotNet/issues/599) Proper HTML escaping of BenchmarkAttribute Description 
* [#606](https://github.com/dotnet/BenchmarkDotNet/issues/606) Improve Memory Diagnoser (assignee: [@adamsitnik](https://github.com/adamsitnik))
* [#608](https://github.com/dotnet/BenchmarkDotNet/issues/608) Properly escaping generated markdown (assignee: [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [#612](https://github.com/dotnet/BenchmarkDotNet/issues/612) Disassembler DisassembleMethod fails with "Object reference not set to an instance of an object.", (assignee: [@adamsitnik](https://github.com/adamsitnik))
* [#617](https://github.com/dotnet/BenchmarkDotNet/issues/617) Allow baseline per category (assignee: [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [#618](https://github.com/dotnet/BenchmarkDotNet/issues/618) Enable ApprovalTests in .NET Core 2.0 tests (assignee: [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [#621](https://github.com/dotnet/BenchmarkDotNet/issues/621) Try to search for missing references if build fails (assignee: [@adamsitnik](https://github.com/adamsitnik))
* [#622](https://github.com/dotnet/BenchmarkDotNet/issues/622) Support of new GC settings (assignee: [@adamsitnik](https://github.com/adamsitnik))
* [#623](https://github.com/dotnet/BenchmarkDotNet/issues/623) RPlotExporter uses wrong path to csv measurements (assignee: [@AndreyAkinshin](https://github.com/AndreyAkinshin))

#### Merged pull requests (10)

* [#573](https://github.com/dotnet/BenchmarkDotNet/pull/573) Сreate a tail call diagnoser (by [@GeorgePlotnikov](https://github.com/GeorgePlotnikov))
* [#576](https://github.com/dotnet/BenchmarkDotNet/pull/576) Display VM name in summary section, fixes #574 (by [@lukasz-pyrzyk](https://github.com/lukasz-pyrzyk))
* [#595](https://github.com/dotnet/BenchmarkDotNet/pull/595) Migrate all project to new project system. (by [@mfilippov](https://github.com/mfilippov))
* [#598](https://github.com/dotnet/BenchmarkDotNet/pull/598) Added info about the new TailCallDiagnoser (by [@GeorgePlotnikov](https://github.com/GeorgePlotnikov))
* [#603](https://github.com/dotnet/BenchmarkDotNet/pull/603) Fix HTML Encoding for Html Exporter (by [@Chrisgozd](https://github.com/Chrisgozd))
* [#605](https://github.com/dotnet/BenchmarkDotNet/pull/605) Grammar (by [@onionhammer](https://github.com/onionhammer))
* [#607](https://github.com/dotnet/BenchmarkDotNet/pull/607) Print amount of logical and physical core #582 (by [@morgan-kn](https://github.com/morgan-kn))
* [#615](https://github.com/dotnet/BenchmarkDotNet/pull/615) Quick fix Disassembler.Program.GetMethod when more than one method found just return null (by [@nietras](https://github.com/nietras))
* [#619](https://github.com/dotnet/BenchmarkDotNet/pull/619) Logical group support, fixes #617 (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [#620](https://github.com/dotnet/BenchmarkDotNet/pull/620) New README.md (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))

#### Commits (42)

* [6f587d](https://github.com/dotnet/BenchmarkDotNet/commit/6f587d99897ed67c94277c4c0d34f838e586ff92) Migrate all project to new project system. (by [@mfilippov](https://github.com/mfilippov))
* [47ba57](https://github.com/dotnet/BenchmarkDotNet/commit/47ba57d9e196a81710eb002eb3af4fb6401b7e78) added info about the new TailCallDiagnoser (by [@GeorgePlotnikov](https://github.com/GeorgePlotnikov))
* [c1a4b2](https://github.com/dotnet/BenchmarkDotNet/commit/c1a4b20b11165e696f198e0e68a0a5c2b991b65e) Сreate a tail call diagnoser (#573) (by [@GeorgePlotnikov](https://github.com/GeorgePlotnikov))
* [ebe3e2](https://github.com/dotnet/BenchmarkDotNet/commit/ebe3e2f90f2a974fdf1ec3524f8aa79674beccc5) Merge pull request #598 from GeorgePlotnikov/patch-1 (by [@adamsitnik](https://github.com/adamsitnik))
* [6249f0](https://github.com/dotnet/BenchmarkDotNet/commit/6249f0a4ee37904fac418cd8715af9d8f667c01d) some polishing of the JIT diagnosers (by [@adamsitnik](https://github.com/adamsitnik))
* [119231](https://github.com/dotnet/BenchmarkDotNet/commit/119231c8ebf94673dcfdbd5bacc1cdfde4a294c4) Fix HTML Encoding for Html Exporter (#603), fixes #599 (by [@Chrisgozd](https://github.com/Chrisgozd))
* [fe3f30](https://github.com/dotnet/BenchmarkDotNet/commit/fe3f3046c26ef0a63e55c7f97651b5ee815e22ee) Disassembly Prettifier, fixes #546 (by [@adamsitnik](https://github.com/adamsitnik))
* [3eb63f](https://github.com/dotnet/BenchmarkDotNet/commit/3eb63ff8c6b4a571423bc2b2d2cf086e1c2f993f) Merge pull request #595 from mfilippov/new-fs-vb-proj (by [@adamsitnik](https://github.com/adamsitnik))
* [16d03f](https://github.com/dotnet/BenchmarkDotNet/commit/16d03f65cd6198fd0003c7608a986b823c638538) make our F# samples work for .NET Core 2.0 (by [@adamsitnik](https://github.com/adamsitnik))
* [d06de7](https://github.com/dotnet/BenchmarkDotNet/commit/d06de7af52d60a5d92b3665e9d20b0be3dfc29e7) bring back our old Visual Basic and F# integration tests (by [@adamsitnik](https://github.com/adamsitnik))
* [63249b](https://github.com/dotnet/BenchmarkDotNet/commit/63249b50ec6dfeb6719ba9edb911404e16bf7f02) "Kaby Lake R" and "Coffee Lake" support in ProcessorBrandStringHelper (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [a8a09e](https://github.com/dotnet/BenchmarkDotNet/commit/a8a09ebbc86c51167cf90f18c1658d52afcf1b70) disassembly prettifier: highlighting references to labels, jumping to next on... (by [@adamsitnik](https://github.com/adamsitnik))
* [e6d747](https://github.com/dotnet/BenchmarkDotNet/commit/e6d747efd2d19380d8da388cceb3471b5e894dbd) Grammar (by [@onionhammer](https://github.com/onionhammer))
* [fef4aa](https://github.com/dotnet/BenchmarkDotNet/commit/fef4aa67b3c3bf6aac8f2281ee8fbd763660cba4) Merge pull request #605 from onionhammer/patch-1 (by [@adamsitnik](https://github.com/adamsitnik))
* [ffacd7](https://github.com/dotnet/BenchmarkDotNet/commit/ffacd74b63f364e88aa8afa597fbbc84d6a564c2) don't require extra run for DisassemblyDiagnoser, fixes #543, #542 (by [@adamsitnik](https://github.com/adamsitnik))
* [bcac26](https://github.com/dotnet/BenchmarkDotNet/commit/bcac26452dbed7ba310ecef8a4ec0814cd22591d) revert last commit change (run global setup regardless of Jitting) (by [@adamsitnik](https://github.com/adamsitnik))
* [3e87d8](https://github.com/dotnet/BenchmarkDotNet/commit/3e87d8699b27751ef05e8303f6ccb1f6d9c74b44) don't perform an extra run to get GC stats for .NET Core, part of #550 (by [@adamsitnik](https://github.com/adamsitnik))
* [f87dbc](https://github.com/dotnet/BenchmarkDotNet/commit/f87dbc5357e7f15d7913e2136ac73c8d1af8cfd1) obtain GC stats in separate iteration run, no overhead, support for iteration... (by [@adamsitnik](https://github.com/adamsitnik))
* [e5fe0f](https://github.com/dotnet/BenchmarkDotNet/commit/e5fe0f87dc0043a10648faf01fc29805624c5c3a) update to C# 7.1 so we can use all the latest features (by [@adamsitnik](https://github.com/adamsitnik))
* [bc50b2](https://github.com/dotnet/BenchmarkDotNet/commit/bc50b2e851aabe15c47656897ef5024279e4e31c) build benchmarks in Parallel, part of #550 (by [@adamsitnik](https://github.com/adamsitnik))
* [e59590](https://github.com/dotnet/BenchmarkDotNet/commit/e595902a377085cb2f44fca6fcab3efae82cda06) Display VM name in summary section, fixes #574 (#576) (by [@lukasz-pyrzyk](https://github.com/lukasz-pyrzyk))
* [8908f8](https://github.com/dotnet/BenchmarkDotNet/commit/8908f8798f0914ce6abe925a0c14e063ace6964d) fix GetMethod (by [@nietras](https://github.com/nietras))
* [4ca82d](https://github.com/dotnet/BenchmarkDotNet/commit/4ca82db5857cda64732743bb5e47199f4300fcf5) Merge pull request #615 from nietras/disassembler-more-than-one-method-fix (by [@adamsitnik](https://github.com/adamsitnik))
* [387ae5](https://github.com/dotnet/BenchmarkDotNet/commit/387ae54f1fedffb78f5955c7935034ecde3cc856) be more defensive when trying to read source code with disassembler, part of ... (by [@adamsitnik](https://github.com/adamsitnik))
* [703815](https://github.com/dotnet/BenchmarkDotNet/commit/7038155d914e5679696b17d18524e8066256d14e) docs: how to contribute to disassembler (by [@adamsitnik](https://github.com/adamsitnik))
* [242671](https://github.com/dotnet/BenchmarkDotNet/commit/242671b88d188827f0cc83a6da1dfef4986f2e03) Enable ApprovalTests in .NET Core 2.0 tests, fixes #618 (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [c4d21b](https://github.com/dotnet/BenchmarkDotNet/commit/c4d21bf7e7a022c6cffcc59ddd35415a83b93243) Print amount of logical and physical core #582 (#607) (by [@morgan-kn](https://github.com/morgan-kn))
* [e33e84](https://github.com/dotnet/BenchmarkDotNet/commit/e33e848e1679fc5ceb88ec27dc9ecad1041b0a34) Add HtmlReady dialect for MarkdownExporter, fixes #608 (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [cf167b](https://github.com/dotnet/BenchmarkDotNet/commit/cf167b9f092abc157677081bbf2955ee50ad6934) Enable html escaping for GitHub markdown dialect, fixes #608 (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [8bb28b](https://github.com/dotnet/BenchmarkDotNet/commit/8bb28b30a0a2913ce8a92af8c60e27884cd7a90c) Logical group support, fixes #617 (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [ae87c6](https://github.com/dotnet/BenchmarkDotNet/commit/ae87c6d54670f21707069c7d4b432ba886212312) Merge pull request #619 from dotnet/logical-groups (by [@adamsitnik](https://github.com/adamsitnik))
* [14e90b](https://github.com/dotnet/BenchmarkDotNet/commit/14e90bfce8c1430b6235dd6c6e7e94d7136b0d67) parallel build post fix: don't write the compilation errors to NullLogger, re... (by [@adamsitnik](https://github.com/adamsitnik))
* [db4ae8](https://github.com/dotnet/BenchmarkDotNet/commit/db4ae81451251aaf5cce62b4bf059de9642e54f1) Try to search for missing references if build fails, fixes #621 (by [@adamsitnik](https://github.com/adamsitnik))
* [0eba0f](https://github.com/dotnet/BenchmarkDotNet/commit/0eba0f548400531c7992f0b12d7d1766e213ba9b) Support of new GC settings, fixes #622 (by [@adamsitnik](https://github.com/adamsitnik))
* [e31b2d](https://github.com/dotnet/BenchmarkDotNet/commit/e31b2d410def2b7f3941ff44059d0ffdce0dc2ab) Revert Samples/Program.cs (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [7f126b](https://github.com/dotnet/BenchmarkDotNet/commit/7f126ba124137155b146340f29117e0872be6d3e) Add logs in RPlotExporter (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [f8a447](https://github.com/dotnet/BenchmarkDotNet/commit/f8a4477120bcc8034fe5611db4de823b798cfe3a) Fix path to csv in RPlotExporter, fixes #623 (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [273f50](https://github.com/dotnet/BenchmarkDotNet/commit/273f5083babb4d7fd19843cbf2a9401a68568e6c) New plots in RPlotExporter (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [f293f0](https://github.com/dotnet/BenchmarkDotNet/commit/f293f0d5cb6ac42457a22a7637af4bd979f2e131) New README.md (#620) (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [5e3366](https://github.com/dotnet/BenchmarkDotNet/commit/5e3366729a9cd0a3064d90732610c3957d7f3efb) Update copyright year in docs (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [ab7458](https://github.com/dotnet/BenchmarkDotNet/commit/ab74588dd79961887879d83bca0db590966bdc40) Update index in docs (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))
* [4616d4](https://github.com/dotnet/BenchmarkDotNet/commit/4616d48e55cc06ab777b1a5b14d95672df2a22f5) Set library version: 0.10.12 (by [@AndreyAkinshin](https://github.com/AndreyAkinshin))

#### Contributors (9)

* Adam Sitnik ([@adamsitnik](https://github.com/adamsitnik))
* Andrey Akinshin ([@AndreyAkinshin](https://github.com/AndreyAkinshin))
* Christopher Gozdziewski ([@Chrisgozd](https://github.com/Chrisgozd))
* Erik O'Leary ([@onionhammer](https://github.com/onionhammer))
* George Plotnikov ([@GeorgePlotnikov](https://github.com/GeorgePlotnikov))
* Irina Ananyeva ([@morgan-kn](https://github.com/morgan-kn))
* Łukasz Pyrzyk ([@lukasz-pyrzyk](https://github.com/lukasz-pyrzyk))
* Mikhail Filippov ([@mfilippov](https://github.com/mfilippov))
* nietras ([@nietras](https://github.com/nietras))

Thank you very much!

### Links

* [BenchmarkDotNet on GitHub](https://github.com/dotnet/BenchmarkDotNet)
* [Official documentation](http://benchmarkdotnet.org/)
* [ChangeLog](https://github.com/dotnet/BenchmarkDotNet/wiki/ChangeLog)
* [NuGet package](https://www.nuget.org/packages/BenchmarkDotNet/0.10.12)
* [v0.10.12 milestone](https://github.com/dotnet/BenchmarkDotNet/issues?q=milestone:v0.10.12)
* [v0.10.12 commits](https://github.com/dotnet/BenchmarkDotNet/compare/v0.10.11...v0.10.12)
