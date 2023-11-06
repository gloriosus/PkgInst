using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using PkgInst.Helpers;

namespace PkgInst.Controllers;

// TODO: add logging and error handling
public class PackageController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly string _basePath;
    private readonly string _companyName;
    private readonly string _appPath;
    private readonly PackageHelper _packageHelper;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly ILogger<PackageController> _logger;

    public PackageController(IConfiguration configuration, PackageHelper packageHelper, IHttpContextAccessor contextAccessor, ILogger<PackageController> logger)
    {
        _configuration = configuration;
        _basePath = _configuration["BasePath"];
        _companyName = _configuration["CompanyName"];
        _appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? throw new DirectoryNotFoundException();
        _packageHelper = packageHelper;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    [Route("/")]
    [Route("/sort")]
    public IActionResult Index(string by = "name", string order = "asc")
    {
        switch (by)
        {
            case "name":
                ViewBag.Packages = order.Equals("asc") ? _packageHelper.GetPackages().OrderBy(x => x.Name) : 
                                  (order.Equals("desc") ? _packageHelper.GetPackages().OrderByDescending(x => x.Name) : 
                                   throw new ArgumentOutOfRangeException());
                break;
            case "size":
                ViewBag.Packages = order.Equals("asc") ? _packageHelper.GetPackages().OrderBy(x => x.Size) : 
                                  (order.Equals("desc") ? _packageHelper.GetPackages().OrderByDescending(x => x.Size) :
                                  throw new ArgumentOutOfRangeException());
                break;
            case "dateTimeCreated":
                ViewBag.Packages = order.Equals("asc") ? _packageHelper.GetPackages().OrderBy(x => x.DateTimeCreated) : 
                                  (order.Equals("desc") ? _packageHelper.GetPackages().OrderByDescending(x => x.DateTimeCreated) :
                                  throw new ArgumentOutOfRangeException());
                break;
            default:
                ViewBag.Packages = _packageHelper.GetPackages().OrderBy(x => x.Name);
                break;
        }

        ViewBag.SortBy = by;
        ViewBag.SortOrder = order;

        ViewData["CompanyName"] = _companyName;

        return View();
    }

    [Route("/download")]
    public IActionResult Download(string id, string name, bool original = false)
    {
        var ip = _contextAccessor.HttpContext?.Connection?.RemoteIpAddress?.MapToIPv4().ToString();
        _logger.LogInformation("A client with the IP address {IpAddress} downloaded the package {PackageName}", ip, name);

        if (original)
        {
            string tempPath = _packageHelper.ExtractPackage(id, Path.Combine("pkg_1", "exec"));

            var itemPaths = new DirectoryInfo(Path.Combine(tempPath, "pkg_1", "exec"))
                .EnumerateFiles(string.Empty, SearchOption.TopDirectoryOnly)
                .Where(x => !x.Name.Equals("executable_package.kpd"))
                .Select(x => x.FullName)
                .Concat(
                    new DirectoryInfo(Path.Combine(tempPath, "pkg_1", "exec"))
                    .EnumerateDirectories(string.Empty, SearchOption.TopDirectoryOnly)
                    .Select(x => x.FullName)
                );

            var originalFileStream = itemPaths.Count() == 1 ? 
                new FileInfo(itemPaths.First()).OpenRead() : 
                GetNewArchive(Path.Combine(tempPath, $"{name}.zip"), itemPaths);

            Task.Run(() =>
            {
                Thread.Sleep(10000);
                Directory.Delete(tempPath, true);
            });

            return File(originalFileStream, "application/octet-stream", itemPaths.Count() == 1 ? name : $"{name}.zip");
        }

        var fileStream = System.IO.File.OpenRead(Path.Combine(_basePath, id, "installer.exe"));
        return File(fileStream, "application/octet-stream", name);
    }

    private FileStream GetNewArchive(string archivePath, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var processInfo = new ProcessStartInfo
            {
            #if Linux
                FileName = "7zz",
            #else
                FileName = "7za",
            #endif
                Arguments = $"a \"{archivePath}\" \"{path}\"",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = Process.Start(processInfo);
            process?.WaitForExit();
        }

        return System.IO.File.OpenRead(archivePath);
    }
}
