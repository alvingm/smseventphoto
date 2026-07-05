namespace smseventphoto.Models;

public sealed class PhotoEntry
{
    public required string Id { get; init; }

    public required string OriginalFileName { get; init; }

    public required string ContentType { get; init; }

    public required string PhotoStorageName { get; init; }

    public required string Message { get; init; }

    public string? Sender { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
