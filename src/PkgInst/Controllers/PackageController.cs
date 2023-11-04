using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using PkgInst.Models;

namespace PkgInst.Controllers;

public class PackageController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly string _basePath;
    private readonly string _companyName;

    public PackageController(IConfiguration configuration)
    {
        _configuration = configuration;
        _basePath = _configuration["BasePath"];
        _companyName = _configuration["CompanyName"];
    }

    // TODO: move to a helper class
    private IEnumerable<Package> GetPackages()
    {
        foreach (var fullPath in Directory.EnumerateDirectories(_basePath))
        {
            string id = new DirectoryInfo(fullPath).Name;
            var packageInfo = GetPackageInfo(fullPath);
            string name = packageInfo.Item1;
            var fileInfo = new FileInfo(Path.Combine(fullPath, "installer.exe"));
            long size = fileInfo.Length;
            DateTime dateTimeCreated = fileInfo.LastWriteTime;
            string parameters = packageInfo.Item2;
            yield return new Package(id, name, $"/download?id={id}&name={name}", size, dateTimeCreated, parameters);
        }
    }

    // TODO: move to a helper class
    private (string, string) GetPackageInfo(string path)
    {
        string appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        string dateTime = DateTime.Now.ToString("yyyyMMddHHmmssfffffff");

        var processInfo = new ProcessStartInfo
        {
            FileName = "7za",
            Arguments = $"e \"{Path.Combine(path, "installer.exe")}\" -aoa -o\"{Path.Combine(appPath, dateTime)}\" \"{Path.Combine("pkg_1", "executable_package.kpd")}\"",
            WindowStyle = ProcessWindowStyle.Hidden
        };

        var process = Process.Start(processInfo);
        process?.WaitForExit();

        string[] lines = System.IO.File.ReadAllLines(Path.Combine(appPath, dateTime, "executable_package.kpd"));
        string name = lines.FirstOrDefault(x => x.StartsWith("LocalizedName="))!.Split('=')[1];
        string parameters = string.Join('=', lines.FirstOrDefault(x => x.StartsWith("Params="))!.Split('=').Skip(1));

        Directory.Delete(Path.Combine(appPath, dateTime), true);

        return (string.IsNullOrWhiteSpace(name) ? "installer.exe" : name, parameters);
    }

    [Route("/")]
    [Route("/sort")]
    public IActionResult Index(string by = "name", string order = "asc")
    {
        switch (by)
        {
            case "name":
                ViewBag.Packages = order == "asc" ? GetPackages().OrderBy(x => x.Name) : GetPackages().OrderByDescending(x => x.Name);
                break;
            case "size":
                ViewBag.Packages = order == "asc" ? GetPackages().OrderBy(x => x.Size) : GetPackages().OrderByDescending(x => x.Size);
                break;
            case "dateTimeCreated":
                ViewBag.Packages = order == "asc" ? GetPackages().OrderBy(x => x.DateTimeCreated) : GetPackages().OrderByDescending(x => x.DateTimeCreated);
                break;
            default:
                ViewBag.Packages = GetPackages().OrderBy(x => x.Name);
                break;
        }

        ViewData["CompanyName"] = _companyName;

        return View();
    }

    // TODO: make ability to download unpacked original installer
    [Route("/download")]
    public IActionResult Download(string id, string name)
    {
        var bytes = System.IO.File.ReadAllBytes(Path.Combine(_basePath, id, "installer.exe"));
        return File(bytes, "application/vnd.microsoft.portable-executable", name);
    }
}
