using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using PkgInst.Models;
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
            // TODO: make sure that there are always only two items extracted otherwise change "*.*"
            string tempPath = _packageHelper.ExtractPackage(id, Path.Combine("pkg_1", "exec", "*.*"));

            var originalFileStream = new DirectoryInfo(tempPath).GetFiles().First(x => !x.Name.Equals("executable_package.kpd")).OpenRead();

            Task.Run(() => 
            { 
                Thread.Sleep(10000); 
                Directory.Delete(tempPath, true); 
            });

            return File(originalFileStream, "application/octet-stream", name);
        }

        var fileStream = System.IO.File.OpenRead(Path.Combine(_basePath, id, "installer.exe"));
        return File(fileStream, "application/octet-stream", name);
    }
}
