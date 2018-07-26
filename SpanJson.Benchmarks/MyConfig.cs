using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Toolchains.CustomCoreClr;

namespace SpanJson.Benchmarks
{
    public class MyConfig : ManualConfig
    {
        // get the latest sdk
        // https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x64.zip
        // extract it and set path, SET PATH=latestsdk;%PATH%
        // SET COMPlus_TieredCompilation = 1/0
        // dotnet run -c Release -- 4
        public MyConfig()
        {
            Add(Job.Default.With(new CustomCoreClrToolchain(
                "myget builds",
                "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json",
                "3.0.0-preview1-26726-01",
                "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json",
                "4.6.0-preview1-26725-04", "netcoreapp3.0")));
            Add(MemoryDiagnoser.Default);
            Set(new DefaultOrderProvider(SummaryOrderPolicy.Default, MethodOrderPolicy.Alphabetical));
        }
    }
}