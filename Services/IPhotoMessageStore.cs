using smseventphoto.Models;

namespace smseventphoto.Services;

public interface IPhotoMessageStore
{
    Task<IReadOnlyList<PhotoEntry>> ListAsync(CancellationToken cancellationToken);

    Task<PhotoEntry> SaveAsync(PhotoUpload upload, Stream content, CancellationToken cancellationToken);

    Task<StoredPhoto?> OpenPhotoAsync(string id, CancellationToken cancellationToken);
}
