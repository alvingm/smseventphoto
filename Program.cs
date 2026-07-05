using Microsoft.AspNetCore.Mvc;
using smseventphoto.Models;
using smseventphoto.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<IPhotoMessageStore>(serviceProvider =>
{
    var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();

    if (!string.IsNullOrWhiteSpace(options.AzureBlobConnectionString))
    {
        return new AzureBlobPhotoMessageStore(options);
    }

    var localRoot = Path.IsPathRooted(options.LocalRootPath)
        ? options.LocalRootPath
        : Path.Combine(environment.ContentRootPath, options.LocalRootPath);

    return new LocalFilePhotoMessageStore(localRoot);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapGet("/photos/{id}", async Task<IResult> (string id, IPhotoMessageStore store, CancellationToken cancellationToken) =>
{
    var photo = await store.OpenPhotoAsync(id, cancellationToken);
    return photo is null
        ? Results.NotFound()
        : Results.File(photo.Content, photo.ContentType);
});

app.MapPost("/api/messages", async Task<IResult> (HttpRequest request, IPhotoMessageStore store, CancellationToken cancellationToken) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { error = "Multipart form data is required." });
    }

    var form = await request.ReadFormAsync(cancellationToken);
    var photo = form.Files["photo"] ?? form.Files["media"] ?? form.Files["Media"] ?? form.Files.FirstOrDefault();
    var message = form["message"].ToString();
    var sender = form["from"].ToString();
    var validationError = PhotoSubmissionValidator.Validate(photo, message, sender);
    if (validationError is not null)
    {
        return Results.BadRequest(new { error = validationError });
    }

    var validatedPhoto = photo!;
    using var content = validatedPhoto.OpenReadStream();
    var entry = await store.SaveAsync(
        new PhotoUpload(validatedPhoto.FileName, validatedPhoto.ContentType, message, sender),
        content,
        cancellationToken);

    return Results.Created($"/photos/{entry.Id}", new
    {
        entry.Id,
        entry.Message,
        entry.Sender,
        entry.CreatedAt
    });
});

app.MapRazorPages();
app.Run();
