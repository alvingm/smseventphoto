namespace smseventphoto.Models;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string? AzureBlobConnectionString { get; init; }

    public string ContainerName { get; init; } = "eventphotos";

    public string LocalRootPath { get; init; } = "App_Data";
}
