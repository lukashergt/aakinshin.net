---
title: "BenchmarkDotNet v0.10.7"
date: "2017-06-05"
tags:
- programming
- dotnet
- cs
- BenchmarkDotNet
- benchmarking
aliases:
- /blog/post/bdn-v0_10_7
---

BenchmarkDotNet v0.10.7 has been released.
In this post, I will briefly cover the following features:

* LINQPad support
* Filters and categories
* Updated Setup/Cleanup attributes
* Better Value Types support
* Building Sources on Linux

<!--more-->

---

### LINQPad support

We already supported LinqPad some time ago, but the support was broken in recent version of BenchmarkDotNet.
It turned out that there was a conflict between the Roslyn assemblies in LINQPad and those referenced by BenchmarkDotNet.
With the help of [@albahari](https://github.com/albahari) (the author of LinqPad), the issue [was fixed](https://github.com/dotnet/BenchmarkDotNet/issues/445#issuecomment-300683997).
The fix was on the LINQPad side, so you need LINQPad 5.22.05+ to get it worked (currently it's a beta version, it can be downloaded from [the official site](https://www.linqpad.net/Download.aspx)).

{{< img linqpad >}}

See also:
  [BenchmarkDotNet#66](https://github.com/dotnet/BenchmarkDotNet/issues/66),
  [BenchmarkDotNet#445](https://github.com/dotnet/BenchmarkDotNet/issues/445).

---

### Filters and categories

[This issue](https://github.com/dotnet/BenchmarkDotNet/issues/248) by [@jskeet](https://github.com/jskeet) was pretty old,
  but I finally found some time to implement it.
BenchmarkDotNet becomes very popular, some of our users have a lot of benchmarks, and they don't want to run all the benchmarks each time.
In this case, they can *filter* some of them with the help of *filters*.

Usage examples:

```cs
[Config(typeof(Config))]
public class IntroFilters
{
    private class Config : ManualConfig
    {
        // We will benchmark ONLY method with names with names (which contains "A" OR "1") AND (have length < 3)
        public Config()
        {
            Add(new DisjunctionFilter(
                new NameFilter(name => name.Contains("A")),
                new NameFilter(name => name.Contains("1"))
            )); // benchmark with names which contains "A" OR "1"
            Add(new NameFilter(name => name.Length < 3)); // benchmark with names with length < 3
        }
    }

    [Benchmark] public void A1() => Thread.Sleep(10); // Will be benchmarked
    [Benchmark] public void A2() => Thread.Sleep(10); // Will be benchmarked
    [Benchmark] public void A3() => Thread.Sleep(10); // Will be benchmarked
    [Benchmark] public void B1() => Thread.Sleep(10); // Will be benchmarked
    [Benchmark] public void B2() => Thread.Sleep(10);
    [Benchmark] public void B3() => Thread.Sleep(10);
    [Benchmark] public void C1() => Thread.Sleep(10); // Will be benchmarked
    [Benchmark] public void C2() => Thread.Sleep(10);
    [Benchmark] public void C3() => Thread.Sleep(10);
    [Benchmark] public void Aaa() => Thread.Sleep(10);
}
```

An example of `BenchmarkCategory` usage:

```cs
[DryJob]
[CategoriesColumn]
[BenchmarkCategory("Awesome")]
[AnyCategoriesFilter("A", "1")]
public class IntroCategories
{
    [Benchmark]
    [BenchmarkCategory("A", "1")]
    public void A1() => Thread.Sleep(10); // Will be benchmarked

    [Benchmark]
    [BenchmarkCategory("A", "2")]
    public void A2() => Thread.Sleep(10); // Will be benchmarked

    [Benchmark]
    [BenchmarkCategory("B", "1")]
    public void B1() => Thread.Sleep(10); // Will be benchmarked

    [Benchmark]
    [BenchmarkCategory("B", "2")]
    public void B2() => Thread.Sleep(10);
}
```

The filtering can be performed via command line.
Examples:

```txt
--category=A
--allCategories=A,B
--anyCategories=A,B
```

If you are using `BenchmarkSwitcher` and want to run all the benchmarks with a category from all types and join them into one summary table, use the `--join` option (or `BenchmarkSwitcher.RunAllJoined`):

```txt
* --join --category=MyAwesomeCategory
```

The last feature was inspired by a [@NickCraver](https://github.com/NickCraver) [comment](https://github.com/dotnet/BenchmarkDotNet/issues/248#issuecomment-300652000),
  it should be useful in [Dapper](https://github.com/StackExchange/Dapper).

See also:
  [BenchmarkDotNet#248](https://github.com/dotnet/BenchmarkDotNet/issues/248).

---

### Updated Setup/Cleanup attributes
Sometimes we want to write some logic which should be executed *before* or *after* a benchmark, but we don't want to measure it.
For this purpose, BenchmarkDotNet provides a set of attributes: `[GlobalSetup]`, `[GlobalCleanup]`, `[IterationSetup]`, `[IterationCleanup]`.

#### GlobalSetup

A method which is marked by the `[GlobalSetup]` attribute will be executed only once per a benchmarked method
  after initialization of benchmark parameters and before all the benchmark method invocations.

```cs
public class GlobalSetupExample
{
    [Params(10, 100, 1000)]
    public int N;

    private int[] data;

    [GlobalSetup]
    public void GlobalSetup()
    {
        data = new int[N]; // executed once per each N value
    }

    [Benchmark]
    public int Logic()
    {
        int res = 0;
        for (int i = 0; i < N; i++)
            res += data[i];
        return res;
    }
}
```

#### GlobalCleanup

A method which is marked by the `[GlobalCleanup]` attribute will be executed only once per a benchmarked method
  after all the benchmark method invocations.
If you are using some unmanaged resources (e.g., which were created in the `GlobalSetup` method), they can be disposed in the `GlobalCleanup` method.

```cs
public void GlobalCleanup()
{
    // Disposing logic
}
```

#### IterationSetup

A method which is marked by the `[IterationSetup]` attribute will be executed only once *before each an iteration*.
It's not recommended to use this attribute in microbenchmarks because it can spoil the results.
However, if you are writing a macrobenchmark (e.g. a benchmark which takes at least 100ms) and
  you want to prepare some data before each iteration, `[IterationSetup]` can be useful.
BenchmarkDotNet doesn't support setup/cleanup method for a single method invocation (*an operation*), but you can perform only one operation per iteration.
It's recommended to use `RunStrategy.Monitoring` for such cases.
Be careful: if you allocate any objects in the `[IterationSetup]` method, the MemoryDiagnoser results can also be spoiled.

#### IterationCleanup
A method which is marked by the `[IterationCleanup]` attribute will be executed only once *after each an iteration*.
This attribute has the same set of constraint with `[IterationSetup]`: it's not recommended to use `[IterationCleanup]` in microbenchmarks or benchmark which also

#### An example

```cs
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 2, targetCount: 3)]
public class SetupAndCleanupExample
{
  private int setupCounter;
  private int cleanupCounter;

  [IterationSetup]
  public void IterationSetup() => Console.WriteLine("// " + "IterationSetup" + " (" + ++setupCounter + ")");

  [IterationCleanup]
  public void IterationCleanup() => Console.WriteLine("// " + "IterationCleanup" + " (" + ++cleanupCounter + ")");

  [GlobalSetup]
  public void GlobalSetup() => Console.WriteLine("// " + "GlobalSetup");

  [GlobalCleanup]
  public void GlobalCleanup() => Console.WriteLine("// " + "GlobalCleanup");

  [Benchmark]
  public void Benchmark() => Console.WriteLine("// " + "Benchmark");
}
```

The order of method calls:

```txt
// GlobalSetup

// IterationSetup (1)    // IterationSetup Jitting
// IterationCleanup (1)  // IterationCleanup Jitting

// IterationSetup (2)    // MainWarmup1
// Benchmark             // MainWarmup1
// IterationCleanup (2)  // MainWarmup1

// IterationSetup (3)    // MainWarmup2
// Benchmark             // MainWarmup2
// IterationCleanup (3)  // MainWarmup2

// IterationSetup (4)    // MainTarget1
// Benchmark             // MainTarget1
// IterationCleanup (4)  // MainTarget1

// IterationSetup (5)    // MainTarget2
// Benchmark             // MainTarget2
// IterationCleanup (5)  // MainTarget2

// IterationSetup (6)    // MainTarget3
// Benchmark             // MainTarget3
// IterationCleanup (6)  // MainTarget3

// GlobalCleanup
```

#### Some additional comments

In `v0.10.6`, we had only the `[Setup]` and `[Cleanup]` attributes which were renamed to `[GlobalSetup]` and `[GlobalCleanup]`.
In `v0.10.7`, we still have `[Setup]` and `[Cleanup]` (so, your benchmarks will not be broken after the update) with a simple trick which is very popular for backward compatibility:

```cs
[Obsolete("Use GlobalSetupAttribute")]
public class SetupAttribute : GlobalSetupAttribute
{
}
[Obsolete("Use GlobalCleanupAttribute")]
public class CleanupAttribute : GlobalCleanupAttribute
{
}
```

It's recommended to fix your benchmarks because we are going to drop in in a few months.
Here is discussion about renaming: [BenchmarkDotNet#456](https://github.com/dotnet/BenchmarkDotNet/issues/456).

Historically, BenchmarkDotNet was focused only on microbenchmarking.
We didn't implement `[IterationSetup]`/`[IterationCleanup]` before because these attributes can't be applied for benchmarking of methods which take nanoseconds (if you want good precision):
Since a lot of our users use it for macrobenchmarking now (and they don't need super-precision in this case),
  it makes sense to support it now and provide a way to use all the BenchmarkDotNet features for such cases.

See also:
  [BenchmarkDotNet#270](https://github.com/dotnet/BenchmarkDotNet/issues/270),
  [BenchmarkDotNet#274](https://github.com/dotnet/BenchmarkDotNet/issues/274),
  [BenchmarkDotNet#325](https://github.com/dotnet/BenchmarkDotNet/issues/325),
  [BenchmarkDotNet#456](https://github.com/dotnet/BenchmarkDotNet/issues/456).

---

### Better Value Types support

Microbenchmarking is tricky.
And it's super-tricky if you are working on the nanosecond-level with value types.
It turned out that we had some troubles with benchmarks that were returning value types prior to v0.10.7,
  but [@adamsitnik](https://github.com/adamsitnik) solved it, and now we have better support for such cases.
Probably, it's not the last such problem, so we are going to continue to improve precision with each release!

See also:
  [BenchmarkDotNet/afa803d0](https://github.com/dotnet/BenchmarkDotNet/commit/afa803d0e38c0e11864b2e4394d4a85d3801d944)

---

### Building Sources on Linux

BenchmarkDotNet is a cross-platform NuGet package so that you can use all the basic features on Windows, Linux, and MacOS.
We develop BenchmarkDotNet on Windows, but it's already possible to develop it on Linux and MacOS (with some limitations).
If you have latest [.NET Core SDK](https://www.microsoft.com/net/download/core) and [Mono](https://www.mono-project.com/download/stable/),
  you should be able to build the solution (with unloaded F#/VB projects), run samples (for both `net46`/`netcoreapp1.1`), run unit tests (for `netcoreapp1.1` only).

{{< img linux-xunit >}}

Unfortunately, I don't know how to run unit tests on `net46` and how to pack NuGet packages on Linux:

```txt
akinshin@xu:~/RiderProjects/BenchmarkDotNet/tests$ dotnet test BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj --configuration Release --framework net46
Build started, please wait...
/usr/share/dotnet/sdk/1.0.4/Microsoft.Common.CurrentVersion.targets(1111,5): error MSB3644: The reference assemblies for framework ".NETFramework,Version=v4.6" were not found. To resolve this, install the SDK or Targeting Pack for this framework version or retarget your application to a version of the framework for which you have the SDK or Targeting Pack installed. Note that assemblies will be resolved from the Global Assembly Cache (GAC) and will be used in place of reference assemblies. Therefore your assembly may not be correctly targeted for the framework you intend. [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018: The "GenerateRuntimeConfigurationFiles" task failed unexpectedly. [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018: System.IO.DirectoryNotFoundException: Could not find a part of the path '/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/bin/Release/net46/BenchmarkDotNet.Tests.runtimeconfig.json'. [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at Interop.ThrowExceptionForIoErrno(ErrorInfo errorInfo, String path, Boolean isDirectory, Func`2 errorRewriter) [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at Interop.CheckIo[TSafeHandle](TSafeHandle handle, String path, Boolean isDirectory, Func`2 errorRewriter) [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at Microsoft.Win32.SafeHandles.SafeFileHandle.Open(String path, OpenFlags flags, Int32 mode) [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at System.IO.UnixFileStream..ctor(String path, FileMode mode, FileAccess access, FileShare share, Int32 bufferSize, FileOptions options, FileStream parent) [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at System.IO.UnixFileSystem.Open(String fullPath, FileMode mode, FileAccess access, FileShare share, Int32 bufferSize, FileOptions options, FileStream parent) [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at System.IO.FileStream.Init(String path, FileMode mode, FileAccess access, FileShare share, Int32 bufferSize, FileOptions options) [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at Microsoft.NET.Build.Tasks.GenerateRuntimeConfigurationFiles.WriteToJsonFile(String fileName, Object value) [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at Microsoft.NET.Build.Tasks.GenerateRuntimeConfigurationFiles.WriteRuntimeConfig(ProjectContext projectContext) [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at Microsoft.NET.Build.Tasks.GenerateRuntimeConfigurationFiles.ExecuteCore() [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at Microsoft.NET.Build.Tasks.TaskBase.Execute() [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at Microsoft.Build.BackEnd.TaskExecutionHost.Microsoft.Build.BackEnd.ITaskExecutionHost.Execute() [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
/usr/share/dotnet/sdk/1.0.4/Sdks/Microsoft.NET.Sdk/build/Microsoft.NET.Sdk.targets(129,5): error MSB4018:    at Microsoft.Build.BackEnd.TaskBuilder.<ExecuteInstantiatedTask>d__25.MoveNext() [/home/akinshin/RiderProjects/BenchmarkDotNet/tests/BenchmarkDotNet.Tests/BenchmarkDotNet.Tests.csproj]
```

[It seems](https://developercommunity.visualstudio.com/content/problem/61641/targeting-multiple-frameworks-causes-build-to-fail.html) that it's impossible now (.NET Core SDK 1.0.4) by design.
I hope that it will be possible in the future and BenchmarkDotNet will become xplat not only for our users, but also for core BenchmarkDotNet developers.

---
### Conclusion

`v0.10.7` is a small (but important) step forward to `v1.0`.
We didn't release it yet because we are still not sure about how perfect API should look like and what kind of features should be included in a decent benchmarking library.
Any feedback is very valuable, don't hesitate to [create issues](https://github.com/dotnet/BenchmarkDotNet/issues/new) on GitHub if you don't like the current API or you need some additional features.

* BenchmarkDotNet on GitHub: https://github.com/dotnet/BenchmarkDotNet
* Official documentation: http://benchmarkdotnet.org/
* ChangeLog: https://github.com/dotnet/BenchmarkDotNet/wiki/ChangeLog
