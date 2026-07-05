using Microsoft.AspNetCore.Http;

namespace smseventphoto.Services;

public static class PhotoSubmissionValidator
{
    public const long MaxPhotoSizeBytes = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> ContentTypeExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp"
    };

    public static string? Validate(IFormFile? photo, string? message, string? sender)
    {
        if (photo is null || photo.Length == 0)
        {
            return "An image file is required.";
        }

        if (photo.Length > MaxPhotoSizeBytes)
        {
            return "Images must be 10 MB or smaller.";
        }

        if (!ContentTypeExtensions.ContainsKey(photo.ContentType))
        {
            return "Only JPG, PNG, GIF, or WebP images are supported.";
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return "A message is required.";
        }

        if (message.Length > 280)
        {
            return "Messages must be 280 characters or fewer.";
        }

        if (!string.IsNullOrWhiteSpace(sender) && sender.Length > 40)
        {
            return "Sender names must be 40 characters or fewer.";
        }

        return null;
    }

    public static string GetExtensionForContentType(string contentType)
    {
        if (ContentTypeExtensions.TryGetValue(contentType, out var extension))
        {
            return extension;
        }

        throw new InvalidOperationException($"Unsupported content type '{contentType}'.");
    }
}
