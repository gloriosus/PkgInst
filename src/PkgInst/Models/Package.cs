namespace PkgInst.Models;

public class Package
{
    public string Id { get; protected set; } = null!;
    public string Name { get; protected set; } = null!;
    public string Url { get; protected set; } = null!;
    public long Size { get; protected set; }
    public DateTime DateTimeCreated { get; protected set; }
    public string Parameters { get; protected set; } = null!;

    public Package(string id, string name, string url, long size, DateTime dateTimeCreated, string parameters)
    {
        Id = id;
        Name = name;
        Url = url;
        Size = size;
        DateTimeCreated = dateTimeCreated;
        Parameters = parameters;
    }
}
