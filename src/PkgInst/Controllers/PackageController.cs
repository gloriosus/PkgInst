using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using PkgInst.Models;
using PkgInst.Helpers;

namespace PkgInst.Controllers;

public class PackageController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly string _basePath;
    private readonly string _companyName;
    private readonly string _appPath;
    private readonly PackageHelper _packageHelper;

    public PackageController(IConfiguration configuration, PackageHelper packageHelper)
    {
        _configuration = configuration;
        _basePath = _configuration["BasePath"];
        _companyName = _configuration["CompanyName"];
        _appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? throw new DirectoryNotFoundException();
        _packageHelper = packageHelper;
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

        ViewData["CompanyName"] = _companyName;

        return View();
    }

    [Route("/download")]
    public IActionResult Download(string id, string name, bool original = false)
    {
        if (original)
        {
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
