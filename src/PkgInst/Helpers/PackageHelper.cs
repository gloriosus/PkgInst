using PkgInst.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace PkgInst.Helpers;

public class PackageHelper
{
    private readonly IConfiguration _configuration;
    private readonly string _basePath;
    private readonly string _appPath;

    public PackageHelper(IConfiguration configuration)
    {
        _configuration = configuration;
        _basePath = _configuration["BasePath"];
        _appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? throw new DirectoryNotFoundException();
    }

    public IEnumerable<Package> GetPackages()
    {
        var list = new ConcurrentBag<Package>();

        Parallel.ForEach(new DirectoryInfo(_basePath).GetDirectories().Select(x => x.Name), packageId => 
        {
            var packageInfo = GetPackageInfo(packageId);
            var fileInfo = new FileInfo(Path.Combine(_basePath, packageId, "installer.exe"));

            string name = packageInfo.Item1;
            string url = $"/download?id={packageId}&name={name}";
            long size = fileInfo.Length;
            DateTime dateTimeCreated = fileInfo.LastWriteTime;
            string parameters = packageInfo.Item2;

            list.Add(new Package(packageId, name, url, size, dateTimeCreated, parameters));
        });

        return list;
    }

    public (string, string) GetPackageInfo(string packageId)
    {
        string tempPath = ExtractPackage(packageId, Path.Combine("pkg_1", "executable_package.kpd"));

        string[] lines = File.ReadAllLines(Path.Combine(tempPath, "executable_package.kpd"));
        string name = lines.FirstOrDefault(x => x.StartsWith("LocalizedName="))!.Split('=')[1];
        string parameters = string.Join('=', lines.FirstOrDefault(x => x.StartsWith("Params="))!.Split('=').Skip(1));

        Directory.Delete(tempPath, true);

        return (string.IsNullOrWhiteSpace(name) ? "installer.exe" : name, parameters);
    }

    public string ExtractPackage(string packageId, string nameToExtract)
    {
        string packagePath = Path.Combine(_basePath, packageId, "installer.exe");
        string guid = Guid.NewGuid().ToString();
        string tempPath = Path.Combine(_appPath, "temp", guid);

        var processInfo = new ProcessStartInfo
        {
        #if Linux
            FileName = "7zz",
        #else
            FileName = "7za",
        #endif
            Arguments = $"e \"{packagePath}\" -aoa -o\"{tempPath}\" \"{nameToExtract}\"",
            WindowStyle = ProcessWindowStyle.Hidden
        };

        var process = Process.Start(processInfo);
        process?.WaitForExit();

        return tempPath;
    }
}
