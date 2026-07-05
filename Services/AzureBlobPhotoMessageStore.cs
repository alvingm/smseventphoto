using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using smseventphoto.Models;

namespace smseventphoto.Services;

public sealed class AzureBlobPhotoMessageStore : IPhotoMessageStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly BlobContainerClient _containerClient;

    public AzureBlobPhotoMessageStore(StorageOptions options)
    {
        _containerClient = new BlobContainerClient(options.AzureBlobConnectionString, options.ContainerName);
    }

    public async Task<IReadOnlyList<PhotoEntry>> ListAsync(CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var entries = new List<PhotoEntry>();
        await foreach (var blob in _containerClient.GetBlobsAsync(prefix: "entries/", cancellationToken: cancellationToken))
        {
            var entryClient = _containerClient.GetBlobClient(blob.Name);
            var content = await entryClient.DownloadContentAsync(cancellationToken);
            var entry = content.Value.Content.ToObjectFromJson<PhotoEntry>(JsonOptions);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries.OrderByDescending(entry => entry.CreatedAt).ToList();
    }

    public async Task<PhotoEntry> SaveAsync(PhotoUpload upload, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var id = Guid.NewGuid().ToString("n");
        var extension = PhotoSubmissionValidator.GetExtensionForContentType(upload.ContentType);
        var photoStorageName = $"photos/{id}{extension}";
        var photoClient = _containerClient.GetBlobClient(photoStorageName);

        await photoClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = upload.ContentType
                }
            },
            cancellationToken);

        var entry = new PhotoEntry
        {
            Id = id,
            OriginalFileName = Path.GetFileName(upload.FileName),
            ContentType = upload.ContentType,
            PhotoStorageName = photoStorageName,
            Message = upload.Message.Trim(),
            Sender = NormalizeSender(upload.Sender),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var entryClient = _containerClient.GetBlobClient(GetEntryBlobName(id));
        await using var entryStream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(entry, JsonOptions));
        await entryClient.UploadAsync(entryStream, overwrite: true, cancellationToken);

        return entry;
    }

    public async Task<StoredPhoto?> OpenPhotoAsync(string id, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var entry = await GetEntryAsync(id, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        var photoClient = _containerClient.GetBlobClient(entry.PhotoStorageName);
        if (!await photoClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await photoClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return new StoredPhoto(response.Value.Content, entry.ContentType);
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    private async Task<PhotoEntry?> GetEntryAsync(string id, CancellationToken cancellationToken)
    {
        var entryClient = _containerClient.GetBlobClient(GetEntryBlobName(id));
        if (!await entryClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var content = await entryClient.DownloadContentAsync(cancellationToken);
        return content.Value.Content.ToObjectFromJson<PhotoEntry>(JsonOptions);
    }

    private static string GetEntryBlobName(string id) => $"entries/{id}.json";

    private static string? NormalizeSender(string? sender)
    {
        return string.IsNullOrWhiteSpace(sender) ? null : sender.Trim();
    }
}
