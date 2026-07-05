namespace smseventphoto.Models;

public sealed record PhotoUpload(string FileName, string ContentType, string Message, string? Sender);
