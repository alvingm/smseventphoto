using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using smseventphoto.Models;
using smseventphoto.Services;

namespace smseventphoto.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IPhotoMessageStore _store;

    public IndexModel(IPhotoMessageStore store)
    {
        _store = store;
    }

    public IReadOnlyList<PhotoEntry> Photos { get; private set; } = [];

    [BindProperty]
    public PhotoSubmissionInput Input { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Photos = await _store.ListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var validationError = PhotoSubmissionValidator.Validate(Input.Photo, Input.Message, Input.Sender);
        if (validationError is not null)
        {
            ModelState.AddModelError(string.Empty, validationError);
            Photos = await _store.ListAsync(cancellationToken);
            return Page();
        }

        var validatedPhoto = Input.Photo!;
        using var content = validatedPhoto.OpenReadStream();
        await _store.SaveAsync(
            new PhotoUpload(validatedPhoto.FileName, validatedPhoto.ContentType, Input.Message!, Input.Sender),
            content,
            cancellationToken);

        return RedirectToPage();
    }

    public sealed class PhotoSubmissionInput
    {
        public IFormFile? Photo { get; set; }

        public string? Message { get; set; }

        public string? Sender { get; set; }
    }
}
