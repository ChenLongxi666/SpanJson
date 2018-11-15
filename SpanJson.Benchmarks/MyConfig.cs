using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Toolchains.CustomCoreClr;

namespace SpanJson.Benchmarks
{
    public class MyConfig : ManualConfig
    {
        public MyConfig()
        {
            Add(Job.Default.With(CustomCoreClrToolchain.CreateBuilder().UseCoreClrNuGet("3.0.0-preview1-27101-02").UseCoreFxNuGet("4.6.0-preview1-27101-02")
                .RuntimeFrameworkVersion("netcoreapp3.0").ToToolchain()));
            Add(MemoryDiagnoser.Default);
            Set(new DefaultOrderer(SummaryOrderPolicy.Default, MethodOrderPolicy.Alphabetical));
        }
    }
}