using BenchmarkDotNet.Attributes;

namespace PkgInst.Benchmark;

public class PackageHelperBenchmark
{
    private static readonly PackageHelper _packageHelper = new PackageHelper(Path.Combine("D:", "ksc", "KLSHARE", "PkgInst"));

    [Benchmark]
    public void GetPackages()
    {
        _packageHelper.GetPackages();
    }

    [Benchmark]
    public void GetPackagesInParallel()
    {
        _packageHelper.GetPackagesInParallel();
    }
}
