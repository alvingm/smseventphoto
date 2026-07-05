using System.Text.Json;
using smseventphoto.Models;

namespace smseventphoto.Services;

public sealed class LocalFilePhotoMessageStore : IPhotoMessageStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _entriesDirectory;
    private readonly string _photosDirectory;

    public LocalFilePhotoMessageStore(string rootPath)
    {
        var fullRoot = Path.GetFullPath(rootPath);
        _entriesDirectory = Path.Combine(fullRoot, "entries");
        _photosDirectory = Path.Combine(fullRoot, "photos");

        Directory.CreateDirectory(_entriesDirectory);
        Directory.CreateDirectory(_photosDirectory);
    }

    public async Task<IReadOnlyList<PhotoEntry>> ListAsync(CancellationToken cancellationToken)
    {
        var entries = new List<PhotoEntry>();
        foreach (var filePath in Directory.EnumerateFiles(_entriesDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            await using var stream = File.OpenRead(filePath);
            var entry = await JsonSerializer.DeserializeAsync<PhotoEntry>(stream, JsonOptions, cancellationToken);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries.OrderByDescending(entry => entry.CreatedAt).ToList();
    }

    public async Task<PhotoEntry> SaveAsync(PhotoUpload upload, Stream content, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("n");
        var extension = PhotoSubmissionValidator.GetExtensionForContentType(upload.ContentType);
        var photoStorageName = $"photos/{id}{extension}";
        var photoPath = Path.Combine(_photosDirectory, $"{id}{extension}");

        await using (var fileStream = File.Create(photoPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

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

        var entryPath = Path.Combine(_entriesDirectory, $"{id}.json");
        await using var entryStream = File.Create(entryPath);
        await JsonSerializer.SerializeAsync(entryStream, entry, JsonOptions, cancellationToken);

        return entry;
    }

    public async Task<StoredPhoto?> OpenPhotoAsync(string id, CancellationToken cancellationToken)
    {
        var entryPath = Path.Combine(_entriesDirectory, $"{id}.json");
        if (!File.Exists(entryPath))
        {
            return null;
        }

        await using var entryStream = File.OpenRead(entryPath);
        var entry = await JsonSerializer.DeserializeAsync<PhotoEntry>(entryStream, JsonOptions, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        var fileName = Path.GetFileName(entry.PhotoStorageName.Replace("photos/", string.Empty, StringComparison.Ordinal));
        var photoPath = Path.Combine(_photosDirectory, fileName);
        if (!File.Exists(photoPath))
        {
            return null;
        }

        var photoStream = new FileStream(photoPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return new StoredPhoto(photoStream, entry.ContentType);
    }

    private static string? NormalizeSender(string? sender)
    {
        return string.IsNullOrWhiteSpace(sender) ? null : sender.Trim();
    }
}
